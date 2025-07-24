using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Godot;

/// <summary>
///   Singleton manager for achievement data
/// </summary>
public partial class AchievementsManager : Node
{
    private static AchievementsManager? instance;

    private static bool preventAchievements;

    private static bool playerHasCheated;
    private static bool playerInFreebuild;

    private readonly object achievementsDataLock = new();

#pragma warning disable CA2213
    private Control achievementsGUIContainer = null!;
#pragma warning restore CA2213

    private double timeSinceSave;

    private bool loaded;
    private bool loading;

    private bool dirty;

    private bool saving;

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

    private static void UpdateAchievementsPrevention()
    {
        preventAchievements = playerInFreebuild || playerHasCheated;
    }

    private void PerformLoad()
    {
        // TODO: load achievements data

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
        // TODO: writing out the data

        lock (achievementsDataLock)
        {
            saving = false;
        }
    }
}
