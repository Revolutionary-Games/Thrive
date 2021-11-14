using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Godot;
using Saving;

/// <summary>
///   Holds data needed for an in-progress save action. And manages stepping through all the actions that need to happen
/// </summary>
public class InProgressSave : IDisposable
{
    private readonly Func<Node> currentGameRoot;
    private readonly Func<InProgressSave, Save> createSaveData;
    private readonly Action<InProgressSave, Save> performSave;

    /// <summary>
    ///   Raw save name, that is processed by saveNameTask
    /// </summary>
    private readonly string saveName;

    private readonly bool returnToPauseState;

    private State state = State.Initial;
    private Save save;

    private Task<string> saveNameTask;

    private Stopwatch stopwatch;

    private bool success;
    private string message;

    /// <summary>
    ///   Failure exception or message describing the problem. Which one it is depends on
    ///   <see cref="exceptionOrMessageIsException"/>
    /// </summary>
    private string exceptionOrFailureMessage;

    private bool exceptionOrMessageIsException = true;

    private bool disposed;

    private bool wasColourblindScreenFilterVisible;

    public InProgressSave(SaveInformation.SaveType type, Func<Node> currentGameRoot,
        Func<InProgressSave, Save> createSaveData, Action<InProgressSave, Save> performSave, string saveName)
    {
        this.currentGameRoot = currentGameRoot;
        this.createSaveData = createSaveData;
        this.performSave = performSave;
        this.saveName = saveName;
        Type = type;
        returnToPauseState = currentGameRoot.Invoke().GetTree().Paused;

        stopwatch = Stopwatch.StartNew();

        // Start calculating the save name here to save some time
        saveNameTask = new Task<string>(CalculateNameForSave);
        TaskExecutor.Instance.AddTask(saveNameTask);
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

    public void Start()
    {
        currentGameRoot.Invoke().GetTree().Paused = true;

        IsSaving = true;
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
                saveNameTask.Dispose();
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

    private static int FindExistingSavesOfType(out int totalCount, out string oldestSave, string matchRegex)
    {
        int highestNumber = 0;
        totalCount = 0;
        oldestSave = null;
        ulong oldestModifiedTime = ulong.MaxValue;

        using var file = new File();
        foreach (var name in SaveHelper.CreateListOfSaves(SaveHelper.SaveOrder.FileSystem))
        {
            var match = Regex.Match(name, matchRegex);

            if (match.Success)
            {
                ++totalCount;

                int found = Convert.ToInt32(match.Groups[1].Value, CultureInfo.InvariantCulture);

                if (found > highestNumber)
                    highestNumber = found;

                var modified = file.GetModifiedTime(PathUtils.Join(Constants.SAVE_FOLDER, name));

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
        switch (state)
        {
            case State.Initial:
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

                SaveStatusOverlay.Instance.ShowMessage(TranslationServer.Translate("SAVING"),
                    Mathf.Inf);

                state = State.SaveData;
                JSONDebug.FlushJSONTracesOut();
                break;
            }

            case State.SaveData:
            {
                save.Name = saveNameTask.Result;

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

                if (success)
                {
                    SaveStatusOverlay.Instance.ShowMessage(message);

                    currentGameRoot.Invoke().GetTree().Paused = returnToPauseState;
                }
                else
                {
                    SaveStatusOverlay.Instance.ShowMessage(TranslationServer.Translate("SAVE_FAILED"));
                    SaveStatusOverlay.Instance.ShowError(TranslationServer.Translate("ERROR_SAVING"),
                        message, exceptionOrFailureMessage, false, null, exceptionOrMessageIsException);
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
                    if (!saveName.EndsWith(Constants.SAVE_EXTENSION_WITH_DOT, StringComparison.Ordinal))
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
                        int found = Convert.ToInt32(match.Groups[1].Value, CultureInfo.InvariantCulture);

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
