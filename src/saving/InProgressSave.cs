using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Godot;
using Saving;
using Path = System.IO.Path;

/// <summary>
///   Holds data needed for an in-progress save action. And manages stepping through all the actions that need to happen
/// </summary>
public class InProgressSave : IDisposable
{
    private readonly Func<InProgressSave, Save> createSaveData;
    private readonly Action<InProgressSave, Save> performSave;

    private readonly Task<string>? saveNameTask;

    private readonly Stopwatch stopwatch;

    /// <summary>
    ///   Raw save name, that is processed by saveNameTask
    /// </summary>
    private readonly string? saveName;

    private State state = State.Initial;
    private Save? save;

    private bool success;
    private string message = "Error: message not set";

    /// <summary>
    ///   Failure exception or message describing the problem. Which one it is depends on
    ///   <see cref="exceptionOrMessageIsException"/>
    /// </summary>
    private string exceptionOrFailureMessage = string.Empty;

    private bool exceptionOrMessageIsException = true;

    private bool disposed;

    private bool wasColourblindScreenFilterVisible;

    private bool pauseCompletely;

    public InProgressSave(SaveInformation.SaveType type, Func<Node> currentGameRoot,
        Func<InProgressSave, Save> createSaveData, Action<InProgressSave, Save> performSave, string? saveName)
    {
        // This was used for game pausing / unpausing. Now this is unneeded but let's keep this here for a while still
        // to see if there's some other use for this...
        _ = currentGameRoot;

        this.createSaveData = createSaveData;
        this.performSave = performSave;
        this.saveName = saveName;
        Type = type;

        stopwatch = Stopwatch.StartNew();

        // Start calculating the save name here to save some time
        if (type != SaveInformation.SaveType.Invalid)
        {
            saveNameTask = new Task<string>(CalculateNameForSave);
            TaskExecutor.Instance.AddTask(saveNameTask);
        }
    }

    private enum State
    {
        Initial,
        Screenshot,
        SaveData,
        Finished,
    }

    /// <summary>
    ///   True when a save is currently being made. Used to prevent another save or load starting
    /// </summary>
    public static bool IsSaving { get; private set; }

    public SaveInformation.SaveType Type { get; }

    public void Start(bool pause = false)
    {
        PauseManager.Instance.AddPause(nameof(InProgressSave));

        IsSaving = true;
        pauseCompletely = pause;
        Invoke.Instance.Perform(Step);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    internal void ReportStatus(bool success, string message, string exceptionOrFailure = "", bool isException = true)
    {
        this.success = success;
        this.message = message;
        exceptionOrFailureMessage = exceptionOrFailure;
        exceptionOrMessageIsException = isException;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                saveNameTask?.Dispose();
            }

            disposed = true;
        }
    }

    private static string GetNextNameForSaveType(string regex, string newNameStart, int maxSaveCount)
    {
        var highestNumber = FindExistingSavesOfType(out var totalCount, out var oldestSave,
            regex);

        // If all slots aren't used yet
        if (totalCount < maxSaveCount || oldestSave == null)
        {
            ++highestNumber;
            return $"{newNameStart}_{highestNumber:n0}." + Constants.SAVE_EXTENSION;
        }

        // Replace oldest
        return oldestSave;
    }

    private static int FindExistingSavesOfType(out int totalCount, out string? oldestSave, string matchRegex)
    {
        int highestNumber = 0;
        totalCount = 0;
        oldestSave = null;
        ulong oldestModifiedTime = ulong.MaxValue;

        foreach (var name in SaveHelper.CreateListOfSaves(SaveHelper.SaveOrder.FileSystem))
        {
            var match = Regex.Match(name, matchRegex);

            if (match.Success)
            {
                if (!int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture,
                        out int found))
                {
                    continue;
                }

                ++totalCount;

                if (found > highestNumber)
                    highestNumber = found;

                var modified = FileAccess.GetModifiedTime(Path.Combine(Constants.SAVE_FOLDER, name));

                if (modified < oldestModifiedTime)
                {
                    oldestModifiedTime = modified;
                    oldestSave = name;
                }
            }
        }

        return highestNumber;
    }

    private void Step()
    {
        if (pauseCompletely)
            PauseManager.Instance.AddPause(nameof(InProgressSave));

        switch (state)
        {
            case State.Initial:
                if (Type == SaveInformation.SaveType.Invalid)
                {
                    // If we are just meant to show an error message, we can jump ahead steps
                    performSave.Invoke(this, new Save());

                    if (string.IsNullOrEmpty(message))
                        ReportStatus(false, "Error: failure not set for invalid type save");

                    state = State.Finished;
                    break;
                }

                // On this frame a pause menu might still be open, wait until next frame for it to close before
                // taking a screenshot
                wasColourblindScreenFilterVisible = ColourblindScreenFilter.Instance.Visible;
                if (wasColourblindScreenFilterVisible)
                {
                    ColourblindScreenFilter.Instance.Hide();
                }

                state = State.Screenshot;
                break;
            case State.Screenshot:
            {
                save = createSaveData.Invoke(this);

                if (wasColourblindScreenFilterVisible)
                {
                    ColourblindScreenFilter.Instance.Show();
                }

                SaveStatusOverlay.Instance.ShowMessage(Localization.Translate("SAVING_DOT_DOT_DOT"),
                    Mathf.Inf);

                state = State.SaveData;
                JSONDebug.FlushJSONTracesOut();
                break;
            }

            case State.SaveData:
            {
                if (saveNameTask == null)
                {
                    throw new InvalidOperationException(
                        "In progress ave is in invalid state for missing save name generation task");
                }

                save!.Name = saveNameTask.Result;

                GD.Print("Creating a save with name: ", save.Name);
                performSave.Invoke(this, save);
                state = State.Finished;
                break;
            }

            case State.Finished:
            {
                stopwatch.Stop();
                GD.Print("save finished, success: ", success, " message: ", message, " elapsed: ", stopwatch.Elapsed);

                JSONDebug.FlushJSONTracesOut();

                PauseManager.Instance.Resume(nameof(InProgressSave));

                if (success)
                {
                    SaveStatusOverlay.Instance.ShowMessage(message);
                }
                else
                {
                    SaveStatusOverlay.Instance.ShowMessage(Localization.Translate("SAVE_FAILED"));
                    SaveStatusOverlay.Instance.ShowError(Localization.Translate("ERROR_SAVING"),
                        message, exceptionOrFailureMessage, false, false, null, exceptionOrMessageIsException);
                }

                IsSaving = false;

                SaveHelper.MarkLastSaveToCurrentTime();

                return;
            }

            default:
                throw new InvalidOperationException();
        }

        Invoke.Instance.Queue(Step);
    }

    private string CalculateNameForSave()
    {
        switch (Type)
        {
            case SaveInformation.SaveType.Manual:
            {
                if (!string.IsNullOrWhiteSpace(saveName))
                {
                    if (!saveName!.EndsWith(Constants.SAVE_EXTENSION_WITH_DOT, StringComparison.Ordinal))
                        return saveName + Constants.SAVE_EXTENSION_WITH_DOT;

                    return saveName;
                }

                // Find the next unused save number
                int number = 0;

                foreach (var name in SaveHelper.CreateListOfSaves(SaveHelper.SaveOrder.FileSystem))
                {
                    var match = Regex.Match(name, "^save_(\\d+)\\." + Constants.SAVE_EXTENSION);

                    if (match.Success)
                    {
                        if (!int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture,
                                out int found))
                        {
                            continue;
                        }

                        if (found > number)
                            number = found;
                    }
                }

                ++number;
                return $"save_{number:n0}." + Constants.SAVE_EXTENSION;
            }

            case SaveInformation.SaveType.AutoSave:
            {
                return GetNextNameForSaveType("^auto_save_(\\d+)\\." + Constants.SAVE_EXTENSION, "auto_save",
                    Settings.Instance.MaxAutoSaves);
            }

            case SaveInformation.SaveType.QuickSave:
            {
                return GetNextNameForSaveType("^quick_save_(\\d+)\\." + Constants.SAVE_EXTENSION, "quick_save",
                    Settings.Instance.MaxQuickSaves);
            }

            default:
                throw new InvalidOperationException();
        }
    }
}
