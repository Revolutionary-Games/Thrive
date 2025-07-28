using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
    private const string ACHIEVEMENTS_INTEGRITY = "aadasd";

    // NEVER CHANGE THESE
    private const string ACHIEVEMENTS_ENC_KEY_PART = "Thirv11525700";
    private static readonly byte[] EncryptionIv = "2947011395745013"u8.ToArray();

    private static AchievementsManager? instance;

    private static bool preventAchievements;

    private static bool playerHasCheated;
    private static bool playerInFreebuild;

    private readonly object achievementsDataLock = new();

    private readonly AchievementStatStore statsStore = new();

    private readonly Dictionary<int, IAchievement> achievements = new();

#pragma warning disable CA2213
    private Control achievementsGUIContainer = null!;
#pragma warning restore CA2213

    private double timeSinceSave;

    private bool loaded;
    private bool loading;

    private bool dirty;

    private bool saving;

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
        if (playerHasCheated)
            return;

        playerHasCheated = true;
        GD.Print("Player has cheated for the first time in the current save");
        OnPlayerHasCheatedEvent?.Invoke();
        UpdateAchievementsPrevention();
    }

    public override void _Ready()
    {
        base._Ready();

        // Create a container for achievement popups to be in
        achievementsGUIContainer = new Control
        {
            AnchorLeft = 0,
            AnchorRight = 1,
            AnchorTop = 0,
            AnchorBottom = 0,
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            MouseForcePassScrollEvents = false,
        };

        AddChild(achievementsGUIContainer);
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

        // Saving periodically if data is dirty
        if (timeSinceSave > 5 && dirty)
        {
            dirty = false;
            SaveData();
        }
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
            Thread.Sleep(1);
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
            statsStore.IncrementIntStat(AchievementStatStore.STAT_MICROBE_KILLS);

            // TODO: automatically generate the list of relevant achievements for each event?
            ReportStatUpdateToRelevantAchievements([AchievementIds.MICROBIAL_MASSACRE]);
        }
    }

    internal void OnExitEditorWithoutChanges()
    {
        if (preventAchievements)
            return;

        // TODO: add an achievement for this
    }

    internal void OnPlayerPhotosynthesisGlucoseBalance(float balance)
    {
        if (preventAchievements)
            return;

        // TODO: add an achievement for this
    }

    // End of events

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

    private static Aes GetEncryption()
    {
        var key = Encoding.UTF8.GetBytes(ACHIEVEMENTS_ENC_KEY_PART + GetKeySecondPart());
        var aes = Aes.Create();

        aes.Key = key;
        aes.IV = EncryptionIv;

        return aes;
    }

    private void ReportStatUpdateToRelevantAchievements(ReadOnlySpan<int> ids)
    {
        try
        {
            foreach (var id in ids)
            {
                if (achievements[id].ProcessPotentialUnlock(statsStore))
                {
                    GD.Print("Unlocked new achievement: ", achievements[id].InternalName);
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

    /// <summary>
    ///   Shows a GUI popup about an unlocked achievement
    /// </summary>
    private void DisplayAchievement(IAchievement achievement)
    {
        // TODO:
    }

    private void PerformLoad()
    {
        // Load achievements progress data
        // TODO: in Steam mode need to load data from steam
        var newProgress = LoadAchievementsProgress();

        lock (achievementsDataLock)
        {
            if (newProgress != null)
                achievementsDiskProgress = newProgress;

            statsStore.Load(achievementsDiskProgress.IntStats);
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
                        achievementsDiskProgress.UnlockedAchievements.Contains(loadedAchievement.Key));

                    achievements[loadedAchievement.Value.Identifier] = loadedAchievement.Value;
                }
            }
        }
        catch (Exception e)
        {
            GD.PrintErr("Cannot load achievement data: ", e);
            GD.PrintErr("Quitting as achievements will not work at all");
            SceneManager.Instance.QuitDueToError();
            return;
        }

        Invoke.Instance.Perform(() =>
        {
            GD.Print("Achievements data loaded");
            loaded = true;
            loading = false;
        });
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

            // TODO: take a copy of the data?

            // Save in the background
            TaskExecutor.Instance.AddTask(new Task(PerformDataSave));
        }
    }

    private void PerformDataSave()
    {
        // Copy stats data for writing
        lock (achievementsDataLock)
        {
            statsStore.Save(achievementsDiskProgress.IntStats);
        }

        // TODO: make sure that the stats and achievements can't get out of sync here in saving (as we don't hold a
        // lock for the whole duration of the save)

        // Writing out the data
        SaveAchievementsProgress(achievementsDiskProgress);

        lock (achievementsDataLock)
        {
            saving = false;
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

        var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(result)));

        if (hash != ACHIEVEMENTS_INTEGRITY)
        {
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

        // Skip magic
        file.Get32();

        var length = file.Get32();

        var result = file.GetBuffer(length);

        if (file.GetError() != Error.Ok)
        {
            GD.PrintErr("Failed to read achievements progress data");
            return null;
        }

        using Aes aes = GetEncryption();

        using var stream = new MemoryStream(result);
        using var crypto = new CryptoStream(stream, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var textReader = new StreamReader(crypto);
        using var reader = new JsonTextReader(textReader);

        var deserializer = JsonSerializer.Create();
        var progress = deserializer.Deserialize<AchievementsDiskProgress>(reader) ??
            throw new Exception("Failed to deserialize achievements progress");

        return progress;
    }

    private void SaveAchievementsProgress(AchievementsDiskProgress data)
    {
        var path = Constants.ACHIEVEMENTS_PROGRESS_SAVE;

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);

        // Magic is "TAch"
        file.Store32('T' << 24 | 'A' << 16 | 'c' << 8 | 'h');

        // Create data buffer
        using Aes aes = GetEncryption();
        using var stream = new MemoryStream();
        using var crypto = new CryptoStream(stream, aes.CreateEncryptor(), CryptoStreamMode.Write);
        using var textWriter = new StreamWriter(crypto);
        using var writer = new JsonTextWriter(textWriter);

        var serializer = JsonSerializer.Create();

        // Ensure data is not modified while saving it
        lock (achievementsDataLock)
        {
            serializer.Serialize(writer, data);
        }

        writer.Flush();
        textWriter.Flush();
        crypto.Flush();

        // Write data
        file.Store32((uint)stream.Length);
        file.StoreBuffer(stream.ToArray());
    }

    private class AchievementsDiskProgress
    {
        public HashSet<string> UnlockedAchievements = new HashSet<string>();

        public Dictionary<int, int> IntStats = new Dictionary<int, int>();
    }
}
