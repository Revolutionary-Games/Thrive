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

    private bool loaded;
    private bool loading;

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

    public override void _ExitTree()
    {
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
        // TODO: saving periodically if data is dirty
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
