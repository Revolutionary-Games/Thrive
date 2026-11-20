using System;
using System.Diagnostics;
using System.Reflection;
using Godot;

/// <summary>
///   Standalone scene that checks the default world created by opening the Auto-Evo explorer from the main menu.
///   Run this scene directly so the menu's scene transitions and the exit code belong to the test process.
/// </summary>
public partial class AutoEvoExplorerDefaultWorldTest : Node
{
    private readonly Stopwatch elapsed = new();
    private Step step;
    private bool? previousFeedValue;

#pragma warning disable CA2213
    private Button explorerButton = null!;
#pragma warning restore CA2213

    private enum Step
    {
        OpenMenu,
        SelectExplorer,
        CheckWorld,
    }

    public override void _Ready()
    {
        elapsed.Start();
    }

    public override void _Process(double delta)
    {
        try
        {
            var tree = GetTree();
            if (elapsed.Elapsed.TotalSeconds >= 10)
                throw new TimeoutException($"Timed out at explorer test step {step}");

            switch (step)
            {
                case Step.OpenMenu:
                {
                    // The scene manager attaches its shutdown node last after the autoloads are ready
                    if (tree.Root.GetChild(-1) is not PostShutdownActions)
                        return;

                    var feed = Settings.Instance.ThriveNewsFeedEnabled;
                    previousFeedValue = feed.Value;
                    feed.Value = false;

                    // Keep this observer attached while the main menu and explorer replace the current scene
                    tree.CurrentScene = null;
                    SceneManager.Instance.ReturnToMenu();
                    var menus = tree.CurrentScene!.GetNode("MenuContainers/Menus/VBoxContainer");
                    menus.GetNode<Button>("MainMenu/Tools").EmitSignal(BaseButton.SignalName.Pressed);
                    explorerButton = menus.GetNode<Button>("Tools/AutoEvoExploring");
                    step = Step.SelectExplorer;
                    break;
                }

                case Step.SelectExplorer:
                {
                    if (!explorerButton.IsVisibleInTree() || explorerButton.Disabled)
                        return;

                    explorerButton.EmitSignal(BaseButton.SignalName.Pressed);
                    step = Step.CheckWorld;
                    break;
                }

                case Step.CheckWorld:
                {
                    if (tree.CurrentScene is not AutoEvoExploringTool explorer ||
                        TransitionManager.Instance.HasQueuedTransitions)
                    {
                        return;
                    }

                    CheckDefaultWorld(explorer);
                    Finish(true);
                    return;
                }
            }

            elapsed.Restart();
        }
        catch (Exception e)
        {
            GD.PrintErr(e);
            Finish(false);
        }
    }

    /// <summary>
    ///   Requires the world created by the explorer to have LAWK enabled, detecting an incorrect default passed
    ///   to the new game factory even when the menu and scene transitions succeed.
    /// </summary>
    private static void CheckDefaultWorld(AutoEvoExploringTool explorer)
    {
        var world = typeof(AutoEvoExploringTool).GetField("world", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(explorer)!;
        var game = (GameProperties)world.GetType().GetField(nameof(GameProperties))!.GetValue(world)!;
        if (!game.GameWorld.WorldSettings.LAWK)
            throw new InvalidOperationException("Expected the default explorer world's LAWK to be true, got false");
    }

    private void Finish(bool passed)
    {
        SetProcess(false);
        try
        {
            if (previousFeedValue.HasValue)
                Settings.Instance.ThriveNewsFeedEnabled.Value = previousFeedValue.Value;
        }
        catch (Exception e)
        {
            GD.PrintErr(e);
            passed = false;
        }

        if (passed)
            GD.Print("PASS: default explorer world LAWK");

        GetTree().Quit(passed ? 0 : 1);
    }
}
