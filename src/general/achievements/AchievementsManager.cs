﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Newtonsoft.Json;
using FileAccess = Godot.FileAccess;

/// <summary>
///   Singleton manager for achievement data
/// </summary>
public partial class AchievementsManager : Node
{
    /// <summary>
    ///   We really do not want someone to modify the achievement config file, so we verify its hash. This is safe to
    ///   update when intentionally modifying achievement properties.
    /// </summary>
    private const string ACHIEVEMENTS_INTEGRITY = "BiFbOHf4F4zJ91WEKBJDfFj40WJ6RcuIWELLu/gBJnE=";

    private const int MAX_ACHIEVEMENTS_LOAD_WAIT = 60;
    private const int ACHIEVEMENTS_SAVE_INTERVAL = 10;

    private const double ACHIEVEMENT_DISPLAY_TIME = 10;

    // NEVER CHANGE THESE
    private const string ACHIEVEMENTS_ENC_KEY_PART = "Thirv1152570";

    private static readonly byte[] HashKeyFull =
        Encoding.UTF8.GetBytes(ACHIEVEMENTS_ENC_KEY_PART + GetKeySecondPart());

    private static AchievementsManager? instance;

    private static bool preventAchievements;

    // This defaults to on and gets reset when a valid new game is loaded with the right data
    private static bool playerHasCheated = true;
    private static bool playerInFreebuild;

    private static bool showCheatsUsedInfo;

    private readonly NodePath positionName = new("position");

    private readonly object achievementsDataLock = new();

    private readonly Dictionary<int, IAchievement> achievements = new();

    private readonly Queue<IAchievement> achievementsToPopupQueue = new();

    private IAchievementStatStore statsStore = new AchievementStatStore();

#pragma warning disable CA2213
    private Control achievementsGUIContainer = null!;

    private AchievementPopup? createdAchievementPopup;

    private Control? createdAchievementsNotEligibleNotice;

    private PackedScene achievementPopupScene = null!;

    private PackedScene notEligibleScene = null!;

    private AudioStream achievementSound = null!;
#pragma warning restore CA2213

    private double timeSinceSave;

    private bool loaded;
    private bool loading;

    private bool dirty;

    private bool saving;
    private bool invalidData = true;

    private double shownPopupTime = 1000;

    private AchievementsDiskProgress achievementsDiskProgress = new();

    protected AchievementsManager()
    {
        instance = this;
    }

    public delegate void OnPlayerHasCheated();

    public static event OnPlayerHasCheated? OnPlayerHasCheatedEvent;

    public static AchievementsManager Instance => instance ?? throw new InstanceNotLoadedYetException();

    public static bool HasUsedCheats => playerHasCheated;

    public static void ReportNewGameStarted(bool alreadyCheated)
    {
        if (playerInFreebuild)
        {
            GD.Print("Resetting achievements freebuild flag as new game is starting");
            playerInFreebuild = false;
        }

        if (alreadyCheated)
        {
            GD.Print("Starting a new game where cheats have been used already");
            playerHasCheated = true;
        }
        else
        {
            playerHasCheated = false;
        }

        UpdateAchievementsPrevention();
    }

    public static void ReportEnteredFreebuild()
    {
        if (playerInFreebuild)
            return;

        playerInFreebuild = true;
        GD.Print("Reported freebuild status to achievements");

        UpdateAchievementsPrevention();
    }

    public static void ReportCheatsUsed()
    {
        // ReSharper disable HeuristicUnreachableCode
#if DEBUG
#pragma warning disable CS0162 // Unreachable code detected
        if (Constants.IGNORE_CHEATS_FOR_ACHIEVEMENTS_IN_DEBUG)
        {
            GD.Print("Allowing cheat in debug mode");
            return;
        }
#pragma warning restore CS0162 // Unreachable code detected
#endif

        // ReSharper restore HeuristicUnreachableCode

        if (playerHasCheated)
            return;

        playerHasCheated = true;
        GD.Print("Player has cheated for the first time in the current save");
        OnPlayerHasCheatedEvent?.Invoke();
        UpdateAchievementsPrevention();

        showCheatsUsedInfo = true;
    }

    public override void _Ready()
    {
        base._Ready();

        // Determine backend to use
        if (SteamHandler.Instance.IsLoaded || SteamHandler.Instance.WasLoadAttempted)
        {
            GD.Print("Using Steam as achievements storage");
            statsStore = new SteamStatStore(SteamHandler.Instance.GetSteamClientForAchievements());
        }

        if (!SceneManager.Instance.QuittingRequested())
        {
            StartLoadAchievementsData();
        }
        else
        {
            GD.Print("Skipping achievements load as game startup is failing");
        }

        // Create a container for achievement popups to be in
        var layer = new CanvasLayer
        {
            Name = "AchievementsLayer",

            // Really make sure the achievements show on top of anything like Steam achievement popups will
            Layer = 1025,
            Visible = true,
        };

        achievementsGUIContainer = new Control
        {
            AnchorLeft = 0,
            AnchorRight = 1,
            AnchorTop = 0,
            AnchorBottom = 1,
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            MouseForcePassScrollEvents = false,
        };

        layer.AddChild(achievementsGUIContainer);
        AddChild(layer);

        ProcessMode = ProcessModeEnum.Always;

        achievementPopupScene = GD.Load<PackedScene>("res://src/general/achievements/AchievementPopup.tscn");
        notEligibleScene = GD.Load<PackedScene>("res://src/general/achievements/AchievementsNotEligibleNotice.tscn");

        achievementSound = GD.Load<AudioStream>("res://assets/sounds/soundeffects/gui/achievementSound.ogg");
    }

    public override void _ExitTree()
    {
        SaveData();

        while (true)
        {
            lock (achievementsDataLock)
            {
                if (!saving)
                {
                    GD.Print("Final achievements data save complete");
                    break;
                }
            }
        }

        base._ExitTree();

        if (instance == this)
        {
            instance = null;
        }
        else
        {
            GD.PrintErr("Unknown achievements manager instance exiting tree");
        }
    }

    public override void _Process(double delta)
    {
        timeSinceSave += delta;
        shownPopupTime += delta;

        // Saving periodically if data is dirty
        // TODO: it is better to trigger a save like this immediately after doing something important?
        // But still rate-limited at least.
        if (timeSinceSave > ACHIEVEMENTS_SAVE_INTERVAL && dirty)
        {
            timeSinceSave = 0;
            dirty = false;
            SaveData();
            return;
        }

        if (achievementsToPopupQueue.Count > 0)
        {
            if (shownPopupTime > ACHIEVEMENT_DISPLAY_TIME)
            {
                // Show the next achievement popup
                shownPopupTime = 0;
                var popup = achievementsToPopupQueue.Dequeue();

                if (createdAchievementPopup == null)
                {
                    createdAchievementPopup = achievementPopupScene.Instantiate<AchievementPopup>();
                    achievementsGUIContainer.AddChild(createdAchievementPopup);
                }

                createdAchievementPopup.Visible = true;
                createdAchievementPopup.UpdateDataFrom(popup, statsStore);

                createdAchievementPopup.PlayAnimation(ACHIEVEMENT_DISPLAY_TIME - 0.001f);

                achievementsGUIContainer.Visible = true;
            }
        }
        else
        {
            if (shownPopupTime > ACHIEVEMENT_DISPLAY_TIME)
            {
                achievementsGUIContainer.Visible = false;
            }
        }

        if (showCheatsUsedInfo)
        {
            showCheatsUsedInfo = false;

            if (createdAchievementsNotEligibleNotice == null)
            {
                createdAchievementsNotEligibleNotice = notEligibleScene.Instantiate<Control>();
                achievementsGUIContainer.AddChild(createdAchievementsNotEligibleNotice);
            }

            AnimateEligibilityNotice();

            // Make the GUI visible
            shownPopupTime = 0;
            achievementsGUIContainer.Visible = true;
        }
    }

    /// <summary>
    ///   Returns achievement data of all valid achievements
    /// </summary>
    /// <returns>Enumerable of all achievements</returns>
    public IEnumerable<IAchievement> GetAchievements()
    {
        return achievements.Values;
    }

    /// <summary>
    ///   Gets stat store for *reading* nothing should modify the stats retrieved from here
    /// </summary>
    /// <returns>Stats instance for reading data from</returns>
    public IAchievementStatStore GetStats()
    {
        return statsStore;
    }

    /// <summary>
    ///   Resets all achievements and stats to locked status.
    ///   Very bad to call if the player has not very explicitly wanted this to happen!
    /// </summary>
    public void Reset()
    {
        GD.Print("Resetting all achievement and stats states");

        lock (achievementsDataLock)
        {
            if (loading)
                GD.PrintErr("Resetting achievements while load is in progress will probably mess something up");

            foreach (var achievement in achievements)
            {
                achievement.Value.Reset();
            }

            statsStore.Reset();

            achievementsDiskProgress = new AchievementsDiskProgress();

            dirty = true;
            invalidData = false;
            loaded = true;
        }

        GD.Print("Finished resetting achievements state");
    }

    public void StartLoadAchievementsData()
    {
        if (loaded)
        {
            GD.PrintErr("Achievements data already loaded");
            return;
        }

        lock (achievementsDataLock)
        {
            if (loading)
                return;

            loading = true;
            TaskExecutor.Instance.AddTask(new Task(PerformLoad));
        }
    }

    public void WaitForAchievementsData()
    {
        // This lock is probably not critical, but probably doesn't hurt here
        lock (achievementsDataLock)
        {
            if (loaded)
                return;
        }

        var start = Stopwatch.StartNew();

        while (!loaded)
        {
            // Fail immediately if there's a problem
            if (SceneManager.Instance.QuittingRequested())
                return;

            Thread.Sleep(1);

            // Allow really slow computers to play Thrive but don't fully lock things up if there's a problem
            if (start.Elapsed > TimeSpan.FromSeconds(MAX_ACHIEVEMENTS_LOAD_WAIT))
            {
                GD.PrintErr("Achievements data load timed out, quitting startup");
                SceneManager.Instance.QuitDueToError();
                return;
            }
        }

        var elapsed = start.Elapsed;
        if (elapsed > TimeSpan.FromMilliseconds(150))
        {
            GD.Print($"Waiting for achievements data took: {elapsed.TotalMilliseconds:F2} ms");
        }
    }

    // Callbacks forwarded from AchievementEvents

    internal void OnPlayerMicrobeKill()
    {
        if (preventAchievements)
            return;

        lock (achievementsDataLock)
        {
            statsStore.IncrementIntStat(IAchievementStatStore.STAT_MICROBE_KILLS);

            // TODO: automatically generate the list of relevant achievements for each event?
            ReportStatUpdateToRelevantAchievements([AchievementIds.MICROBIAL_MASSACRE]);
        }
    }

    internal void OnExitEditorWithoutChanges()
    {
        if (preventAchievements)
            return;

        lock (achievementsDataLock)
        {
            statsStore.IncrementIntStat(IAchievementStatStore.STAT_NO_CHANGES_IN_EDITOR);

            ReportStatUpdateToRelevantAchievements([AchievementIds.CANNOT_IMPROVE_PERFECTION]);
        }
    }

    internal void OnPlayerPhotosynthesisGlucoseBalance(float balance)
    {
        if (preventAchievements)
            return;

        if (balance > 0.01f)
        {
            lock (achievementsDataLock)
            {
                if (statsStore.GetIntStat(IAchievementStatStore.STAT_POSITIVE_GLUCOSE_PHOTOSYNTHESIS) < 1)
                {
                    statsStore.SetIntStat(IAchievementStatStore.STAT_POSITIVE_GLUCOSE_PHOTOSYNTHESIS, 1);
                }

                ReportStatUpdateToRelevantAchievements([AchievementIds.TASTE_THE_SUN]);
            }
        }
    }

    internal void OnPlayerUsesRadiation()
    {
        if (preventAchievements)
            return;

        lock (achievementsDataLock)
        {
            if (statsStore.GetIntStat(IAchievementStatStore.STAT_CELL_EATS_RADIATION) < 1)
            {
                statsStore.SetIntStat(IAchievementStatStore.STAT_CELL_EATS_RADIATION, 1);
            }

            ReportStatUpdateToRelevantAchievements([AchievementIds.TASTY_RADIATION]);
        }
    }

    internal void OnPlayerUsesChemosynthesis()
    {
        if (preventAchievements)
            return;

        lock (achievementsDataLock)
        {
            if (statsStore.GetIntStat(IAchievementStatStore.STAT_CELL_USES_CHEMOSYNTHESIS) < 1)
            {
                statsStore.SetIntStat(IAchievementStatStore.STAT_CELL_USES_CHEMOSYNTHESIS, 1);
            }

            ReportStatUpdateToRelevantAchievements([AchievementIds.VENTS_ARE_HOME]);
        }
    }

    internal void OnPlayerSurvivedWithNucleus()
    {
        if (preventAchievements)
            return;

        lock (achievementsDataLock)
        {
            statsStore.IncrementIntStat(IAchievementStatStore.STAT_SURVIVED_WITH_NUCLEUS);

            ReportStatUpdateToRelevantAchievements([AchievementIds.GOING_NUCLEAR]);
        }
    }

    internal void OnEndosymbiosisCompleted()
    {
        if (preventAchievements)
            return;

        lock (achievementsDataLock)
        {
            statsStore.IncrementIntStat(IAchievementStatStore.STAT_ENDOSYMBIOSIS_COMPLETED);

            ReportStatUpdateToRelevantAchievements([AchievementIds.MICRO_BORG]);
        }
    }

    internal void OnPlayerInCellColony()
    {
        if (preventAchievements)
            return;

        lock (achievementsDataLock)
        {
            statsStore.IncrementIntStat(IAchievementStatStore.STAT_CELL_COLONY_FORMED);

            ReportStatUpdateToRelevantAchievements([AchievementIds.BETTER_TOGETHER]);
        }
    }

    internal void OnReturnToMulticellularStageFromEditor()
    {
        if (preventAchievements)
            return;

        lock (achievementsDataLock)
        {
            if (statsStore.GetIntStat(IAchievementStatStore.STAT_REACHED_MULTICELLULAR) < 1)
            {
                statsStore.SetIntStat(IAchievementStatStore.STAT_REACHED_MULTICELLULAR, 1);
            }

            ReportStatUpdateToRelevantAchievements([AchievementIds.BEYOND_THE_CELL]);
        }
    }

    internal void OnReturnToMicrobeStageFromEditor()
    {
        if (preventAchievements)
            return;

        lock (achievementsDataLock)
        {
            statsStore.IncrementIntStat(IAchievementStatStore.STAT_EDITOR_USAGE);

            ReportStatUpdateToRelevantAchievements([AchievementIds.THE_EDITOR]);
        }
    }

    internal void OnHighestPlayerGeneration(int playerSpeciesGeneration)
    {
        if (preventAchievements)
            return;

        lock (achievementsDataLock)
        {
            if (statsStore.GetIntStat(IAchievementStatStore.STAT_MAX_SPECIES_GENERATION) < playerSpeciesGeneration)
            {
                statsStore.SetIntStat(IAchievementStatStore.STAT_MAX_SPECIES_GENERATION, playerSpeciesGeneration);
            }

            ReportStatUpdateToRelevantAchievements([AchievementIds.THRIVING]);
        }
    }

    internal void OnPlayerDigestedObject()
    {
        if (preventAchievements)
            return;

        lock (achievementsDataLock)
        {
            statsStore.IncrementIntStat(IAchievementStatStore.STAT_ENGULFMENT_COUNT);

            ReportStatUpdateToRelevantAchievements([AchievementIds.YUM]);
        }
    }

    // End of events

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            positionName.Dispose();
        }

        base.Dispose(disposing);
    }

    private static void UpdateAchievementsPrevention()
    {
        preventAchievements = playerInFreebuild || playerHasCheated;
    }

    private static string GetKeySecondPart()
    {
        ulong value = Constants.ACHIEVEMENT_DATA_VALUE;
        value += 32454563;
        value = ulong.RotateLeft(value, 12);
        value ^= 45576465734523465;
        value = ulong.RotateRight(value, 7);
        value += 42;

        return Convert.ToString(value);
    }

    private static uint GetMagic()
    {
        // Magic is "TAch"
        return 'T' << 24 | 'A' << 16 | 'c' << 8 | 'h';
    }

    private void ReportStatUpdateToRelevantAchievements(ReadOnlySpan<int> ids)
    {
        try
        {
            foreach (var id in ids)
            {
                if (achievements[id].ProcessPotentialUnlock(statsStore))
                {
                    // As this may not be called on the main thread, invoke here
                    Invoke.Instance.Perform(() => { OnAchievementUnlocked(achievements[id]); });
                }
                else if (achievements[id].IsAtUnlockMilestone(statsStore))
                {
                    // Show progress towards an achievement
                    GD.Print("We are at a milestone towards an achievement, showing that info");
                    DisplayAchievement(achievements[id]);
                }
            }
        }
        catch (Exception e)
        {
            GD.PrintErr("Error processing stat update: ", e);
        }

        dirty = true;
    }

    private void OnAchievementUnlocked(IAchievement achievement)
    {
        GD.Print("Unlocked new achievement: ", achievement.InternalName);

        bool playSound = true;

        lock (achievementsDataLock)
        {
            if (statsStore is AchievementStatStore)
            {
                achievementsDiskProgress.UnlockedAchievements.Add(achievement.InternalName);
                DisplayAchievement(achievement);
            }
            else
            {
                // Steam plays its own sound, so we don't want to play our own as well
                playSound = false;

                GD.Print("Reporting to Steam about new achievement");

                if (!SteamHandler.Instance.GetSteamClientForAchievements()
                        .SetSteamAchievement(achievement.InternalName))
                {
                    GD.PrintErr("Failed to report to Steam about a new achievement");
                }

                // Need to save stats immediately on unlocking an achievement
                timeSinceSave = 1000;
                dirty = true;
            }
        }

        if (playSound)
            GUICommon.Instance.PlayCustomSound(achievementSound);
    }

    /// <summary>
    ///   Shows a GUI popup about an unlocked achievement or one with significant progress
    /// </summary>
    private void DisplayAchievement(IAchievement achievement)
    {
        if (statsStore is AchievementStatStore)
        {
            GD.Print("Showing a popup about an achievement: ", achievement.InternalName);
            achievementsToPopupQueue.Enqueue(achievement);
        }
        else
        {
            if (!achievement.GetSteamProgress(statsStore, out var current, out var max))
            {
                GD.PrintErr(
                    "Achievement that wants to show progress, couldn't retrieve current Steam progress for display");
                return;
            }

            if (SteamHandler.Instance.GetSteamClientForAchievements()
                .IndicateAchievementProgress(achievement.InternalName, current, max))
            {
                GD.Print("Requesting Steam show progress towards an achievement");
            }
            else
            {
                GD.PrintErr("Failed to show achievement progress with Steam popup");
            }
        }
    }

    private void PerformLoad()
    {
        var alreadyUnlockedCallback = (string achievement) =>
            achievementsDiskProgress.UnlockedAchievements.Contains(achievement);

        // Load achievements progress data (only in non-Steam mode)
        if (statsStore is AchievementStatStore basicStore)
        {
            AchievementsDiskProgress? newProgress;

            try
            {
                newProgress = LoadAchievementsProgress();
            }
            catch (Exception e)
            {
                GD.PrintErr("Error while loading achievements data: ", e);

                // TODO: if players hit this too often we might just need to have a popup warning about this and
                // asking if the player would like to reset their achievements data or quit the game
                GD.PrintErr("QUITTING THE GAME AS ACHIEVEMENT DATA IS NOT GOOD!");
                Invoke.Instance.Perform(() => SceneManager.Instance.QuitDueToError());

                // Unblock the main thread if it is waiting for it
                loaded = true;
                return;
            }

            lock (achievementsDataLock)
            {
                if (newProgress != null)
                    achievementsDiskProgress = newProgress;

                basicStore.Load(achievementsDiskProgress.IntStats);
            }
        }
        else
        {
            GD.Print("Reading current achievement status from Steam");
            alreadyUnlockedCallback = achievement =>
            {
                if (!SteamHandler.Instance.GetSteamClientForAchievements()
                        .GetSteamAchievement(achievement, out var achieved))
                {
                    GD.PrintErr("Failed to read current achievement state for: ", achievement);
                    return false;
                }

                return achieved;
            };
        }

        // Load the achievement configuration JSON
        try
        {
            var data = LoadAchievementsConfig();

            var serializer = JsonSerializer.Create();
            using var reader = new JsonTextReader(new StringReader(data));
            var deserialized = serializer.Deserialize<Dictionary<string, FileLoadedAchievement>>(reader) ??
                throw new Exception("Failed to deserialize achievements");

            lock (achievementsDataLock)
            {
                foreach (var loadedAchievement in deserialized)
                {
                    loadedAchievement.Value.OnLoaded(loadedAchievement.Key,
                        alreadyUnlockedCallback(loadedAchievement.Key));

                    achievements[loadedAchievement.Value.Identifier] = loadedAchievement.Value;
                }
            }
        }
        catch (Exception e)
        {
            GD.PrintErr("Cannot load achievement data: ", e);
            GD.PrintErr("Quitting as achievements will not work at all");
            SceneManager.Instance.QuitDueToError();
            loaded = true;
            return;
        }

        // Cannot invoke here as the main thread may not be running
        lock (achievementsDataLock)
        {
            // But this print needs to be from the main thread
            Invoke.Instance.Perform(() => GD.Print("Achievements data loaded"));

            invalidData = false;
            loaded = true;
            loading = false;
            timeSinceSave = 0;
        }
    }

    private void SaveData()
    {
        lock (achievementsDataLock)
        {
            if (saving)
            {
                // Just in case, queue a new save to happen again
                dirty = true;
                GD.Print("Queueing another achievements save as one was in-progress already");
                return;
            }

            saving = true;

            if (statsStore is AchievementStatStore basicStore)
            {
                // TODO: take a copy of the data (instead of locking while serializing)?

                // Save in the background
                TaskExecutor.Instance.AddTask(new Task(() => PerformDataSave(basicStore)));
            }
            else
            {
                if (SteamHandler.Instance.GetSteamClientForAchievements().SaveSteamStats())
                {
                    GD.Print("Saving achievements to Steam");
                }
                else
                {
                    GD.PrintErr("Failed to save achievements to Steam");
                }

                saving = false;
                timeSinceSave = 0;
            }
        }
    }

    private void PerformDataSave(AchievementStatStore stats)
    {
        // Copy stats data for writing
        lock (achievementsDataLock)
        {
            stats.Save(achievementsDiskProgress.IntStats);
        }

        // TODO: make sure that the stats and achievements can't get out of sync here in saving (as we don't hold a
        // lock for the whole duration of the save)

        // Writing out the data
        SaveAchievementsProgress(achievementsDiskProgress);

        lock (achievementsDataLock)
        {
            saving = false;
            timeSinceSave = 0;
        }
    }

    private string LoadAchievementsConfig()
    {
        var path = Constants.ACHIEVEMENTS_CONFIGURATION;

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        var result = file.GetAsText();

        // This might be completely unnecessary
        file.Close();

        if (string.IsNullOrEmpty(result))
            throw new IOException($"Failed to read achievements file: {path}");

        // Harmonize to unix line endings
        if (result.Contains("\r\n"))
            result = result.Replace("\r\n", "\n");

        var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(result)));

        if (hash != ACHIEVEMENTS_INTEGRITY)
        {
#if DEBUG
            GD.Print($"New achievements hash: {hash}");
#endif

            throw new Exception("Achievements file integrity check failed");
        }

        return result;
    }

    private AchievementsDiskProgress? LoadAchievementsProgress()
    {
        var path = Constants.ACHIEVEMENTS_PROGRESS_SAVE;

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);

        if (file == null)
        {
            GD.Print("No existing achievements progress data");
            return null;
        }

        // Verify the magic number
        // Also prevents endianness issues
        if (file.Get32() != GetMagic())
        {
            throw new Exception("Invalid achievements progress magic");
        }

        // Load verification hash
        var hashLength = file.Get16();
        if (hashLength < 8)
        {
            throw new Exception("Bad hash length");
        }

        var hash = file.GetBuffer(hashLength);

        var length = file.Get32();

        var result = file.GetBuffer(length);

        if (file.GetError() != Error.Ok || result == null || result.Length != length)
        {
            throw new Exception("Failed to read achievements progress data");
        }

        // Fail if hash is not right for the data
        var freshHash = HMACSHA1.HashData(HashKeyFull, result);

        if (!freshHash.SequenceEqual(hash) || freshHash.Length < 8)
        {
            GD.PrintErr("ACHIEVEMENTS FILE HAS BEEN CORRUPTED. HASH MISMATCH. NOT LOADING DATA.");
            throw new Exception("Invalid hash for achievements progress data");
        }

        using var stream = new MemoryStream(result);
        using var textReader = new StreamReader(stream);
        using var reader = new JsonTextReader(textReader);

        var deserializer = JsonSerializer.Create();
        var progress = deserializer.Deserialize<AchievementsDiskProgress>(reader) ??
            throw new Exception("Failed to deserialize achievements progress");

        return progress;
    }

    private void SaveAchievementsProgress(AchievementsDiskProgress data)
    {
        if (invalidData)
        {
            GD.PrintErr("Will not save achievements due to corrupt loaded data!");
            return;
        }

        var path = Constants.ACHIEVEMENTS_PROGRESS_SAVE;

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);

        file.Store32(GetMagic());

        // Create a data buffer
        using var stream = new MemoryStream();

        // In a separate block to make sure everything is flushed
        {
            using var textWriter = new StreamWriter(stream, Encoding.UTF8, -1, true);
            using var writer = new JsonTextWriter(textWriter);

            var serializer = JsonSerializer.Create();

            // Ensure data is not modified while saving it
            lock (achievementsDataLock)
            {
                serializer.Serialize(writer, data);
            }
        }

        // Write verification hash
        var diskBytes = stream.ToArray();

        var hash = HMACSHA1.HashData(HashKeyFull, diskBytes);

        if (hash.Length is < 8 or > ushort.MaxValue)
            throw new Exception("Failed to calculate hash");

        file.Store16((ushort)hash.Length);
        file.StoreBuffer(hash);

        // Write the data buffer
        file.Store32((uint)stream.Length);
        file.StoreBuffer(diskBytes);
    }

    private void AnimateEligibilityNotice()
    {
        createdAchievementsNotEligibleNotice!.Visible = true;

        var screenSize = achievementsGUIContainer.GetViewportRect().Size;

        var size = createdAchievementsNotEligibleNotice.Size;

        var offscreenPosition = new Vector2(screenSize.X - 5 - size.X, screenSize.Y + 5);
        var targetPosition = new Vector2(screenSize.X - 5 - size.X, screenSize.Y - size.Y - 3);

        createdAchievementsNotEligibleNotice.Position = offscreenPosition;

        var tween = createdAchievementsNotEligibleNotice.CreateTween();

        tween.SetTrans(Tween.TransitionType.Expo);
        tween.SetEase(Tween.EaseType.InOut);
        tween.SetPauseMode(Tween.TweenPauseMode.Process);

        tween.TweenProperty(createdAchievementsNotEligibleNotice, positionName, targetPosition, 0.8f);
        tween.TweenInterval(8);

        tween.TweenProperty(createdAchievementsNotEligibleNotice, positionName, offscreenPosition, 0.8f);
    }

    private class AchievementsDiskProgress
    {
        public HashSet<string> UnlockedAchievements = new();

        public Dictionary<int, int> IntStats = new();
    }
}
