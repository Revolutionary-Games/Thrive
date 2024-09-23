using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Godot;
using Xoshiro.PRNG64;

/// <summary>
///   Base class for Godot using benchmarks. Has common functions to make individual benchmark classes shorter
/// </summary>
[GodotAbstract]
public partial class BenchmarkBase : Node
{
    protected readonly List<double> fpsValues = new();

    protected XoShiRo256starstar random;
    protected int internalPhaseCounter;

    protected double timer;
    protected double timeSinceSpawn;

    private readonly BenchmarkHelpers.BenchmarkChangedSettingsStore storedSettings = new();

#pragma warning disable CA2213
    [Export]
    private CustomWindow guiContainer = null!;

    [Export]
    private Label fpsLabel = null!;

    [Export]
    private Label phaseLabel = null!;

    [Export]
    private Label benchmarkResultText = null!;

    [Export]
    private Control benchmarkFinishedText = null!;

    [Export]
    private Button copyResultsButton = null!;
#pragma warning restore CA2213

    private DateTime startTime;
    private double totalDuration;

    private bool exiting;

    protected BenchmarkBase()
    {
        // This calls a virtual property as it is better to initialize random with a proper seed and disallow the seed
        // return property to not try to read other properties
        // ReSharper disable once VirtualMemberCallInConstructor
        random = new XoShiRo256starstar(RandomSeed);
    }

    /// <summary>
    ///   Random seed for this test specifically to initialize the random in this class
    /// </summary>
    protected virtual long RandomSeed => throw new GodotAbstractPropertyNotOverriddenException();

    /// <summary>
    ///   Version of this benchmark. Must be incremented if the benchmark changes significantly.
    /// </summary>
    protected virtual int Version => throw new GodotAbstractPropertyNotOverriddenException();

    /// <summary>
    ///   How many user-visible benchmark steps there are.
    /// </summary>
    protected virtual int TotalSteps => throw new GodotAbstractPropertyNotOverriddenException();

    protected bool HasReachedBenchmarkEnd { get; private set; }

    protected virtual bool StressTestClouds => false;

    public override void _Ready()
    {
        base._Ready();

        guiContainer.Visible = true;

        benchmarkFinishedText.Visible = false;
        copyResultsButton.Visible = false;

        StartBenchmark();
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        BenchmarkHelpers.RestoreNormalSettings(storedSettings);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        fpsLabel.Text = new LocalizedString("FPS", Engine.GetFramesPerSecond()).ToString();

        timer += delta;
        timeSinceSpawn += delta;

        switch (internalPhaseCounter)
        {
            case 0:
            {
                // TODO: if the benchmark ever supports restarting, then cleanup of existing objects needs to be
                // performed here
                BenchmarkHelpers.PerformBenchmarkSetup(storedSettings, StressTestClouds);
                OnBenchmarkStarted();

                IncrementPhase();
                break;
            }
        }
    }

    protected virtual void OnBenchmarkStateReset()
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    protected virtual void OnBenchmarkStarted()
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    protected virtual string GenerateResultsText(int scoreDecimals = 2)
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    protected void IncrementPhase()
    {
        ++internalPhaseCounter;
        timer = 0;
        fpsValues.Clear();

        UpdatePhaseLabel();
    }

    protected void OnBenchmarkEnded()
    {
        totalDuration = (float)(DateTime.Now - startTime).TotalSeconds;
        HasReachedBenchmarkEnd = true;

        IncrementPhase();
    }

    protected virtual int GetDisplayStep(int internalStepNumber)
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    protected float ScoreFromMeasuredFPS()
    {
        if (fpsValues.Count < 1)
            throw new InvalidOperationException("No values recorded");

        // For now just take the average
        // TODO: would be nice to have also min and 1% lows
        return (float)fpsValues.Average();
    }

    protected void WaitForStableFPS()
    {
        // TODO: a more fancy method
        if (timer > 3.5f)
            IncrementPhase();
    }

    protected bool MeasureFPS()
    {
        // Sample up to 20 times per second
        if (timer < 0.05f)
            return false;

        timer = 0;

        fpsValues.Add(Engine.GetFramesPerSecond());

        // TODO: could check for standard deviation or something to determine when we have enough samples
        if (fpsValues.Count > 50)
            return true;

        return false;
    }

    protected void AddTestDurationToResult(StringBuilder result)
    {
        if (totalDuration != 0)
        {
            result.Append("Total test duration: ");
            result.Append(Math.Round(totalDuration, 1).ToString(CultureInfo.InvariantCulture));
            result.Append("s\n");
        }
    }

    protected void AddResultHardwareInfo(StringBuilder result)
    {
        if (result.Length > 0 || Constants.BENCHMARKS_SHOW_HARDWARE_INFO_IMMEDIATELY)
            result.Append(BenchmarkHelpers.GetGeneralHardwareInfo());
    }

    private void StartBenchmark()
    {
        random = new XoShiRo256starstar(RandomSeed);

        internalPhaseCounter = 0;

        startTime = DateTime.Now;
        totalDuration = 0;
        HasReachedBenchmarkEnd = false;

        OnBenchmarkStateReset();

        UpdatePhaseLabel();
    }

    private void UpdatePhaseLabel()
    {
        benchmarkResultText.Text = GenerateResultsText();

        benchmarkFinishedText.Visible = false;
        copyResultsButton.Visible = false;

        if (internalPhaseCounter <= 0)
        {
            phaseLabel.Text = StringUtils.SlashSeparatedNumbersFormat(0, TotalSteps);
        }
        else if (HasReachedBenchmarkEnd)
        {
            phaseLabel.Text = StringUtils.SlashSeparatedNumbersFormat(TotalSteps, TotalSteps);
            benchmarkFinishedText.Visible = true;
            copyResultsButton.Visible = true;
        }
        else
        {
            phaseLabel.Text = StringUtils.SlashSeparatedNumbersFormat(GetDisplayStep(internalPhaseCounter), TotalSteps);
        }
    }

    private void CopyResultsToClipboard()
    {
        var builder = new StringBuilder();

        builder.Append($"Benchmark results for {GetType().Name} v{Version}\n");
        builder.Append(GenerateResultsText(3));

        DisplayServer.ClipboardSet(builder.ToString());
    }

    private void ExitBenchmark()
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (exiting)
            return;

        exiting = true;
        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeOut, 0.1f, OnSwitchToMenuScene, false);
    }

    private void OnSwitchToMenuScene()
    {
        SceneManager.Instance.ReturnToMenu();
    }
}
