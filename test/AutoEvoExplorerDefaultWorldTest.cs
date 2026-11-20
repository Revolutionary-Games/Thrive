using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

/// <summary>
///   Standalone scene that checks the default world created by opening the Auto-Evo explorer from the main menu.
///   Run this scene directly so the menu's scene transitions and the exit code belong to the test process.
/// </summary>
public partial class AutoEvoExplorerDefaultWorldTest : Node
{
    public override void _Ready()
    {
        Callable.From(CheckDefaultWorld).CallDeferred();
    }

    private static async Task WaitFor(SceneTree tree, Func<bool> condition, string action)
    {
        var elapsed = Stopwatch.StartNew();
        while (!condition())
        {
            if (elapsed.Elapsed.TotalSeconds >= 10)
                throw new TimeoutException($"Timed out {action}");

            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }

    /// <summary>
    ///   Follows the main menu's Tools button into the explorer and requires the created world's LAWK to be enabled.
    ///   Reading the actual game detects changes to the settings passed to the new game factory.
    /// </summary>
    private async void CheckDefaultWorld()
    {
        var tree = GetTree();
        var feed = Settings.Instance.ThriveNewsFeedEnabled;
        var previousFeedValue = feed.Value;
        int exitCode = 1;
        try
        {
            feed.Value = false;
            await WaitFor(tree, () => tree.Root.GetChildren().Any(node => node is PostShutdownActions),
                "initialising the scene manager");

            // Keep this observer attached while the main menu and explorer replace the current scene
            tree.CurrentScene = null;
            SceneManager.Instance.ReturnToMenu();
            var menus = tree.CurrentScene!.GetNode("MenuContainers/Menus/VBoxContainer");
            menus.GetNode<Button>("MainMenu/Tools").EmitSignal(BaseButton.SignalName.Pressed);
            var explorerButton = menus.GetNode<Button>("Tools/AutoEvoExploring");
            await WaitFor(tree, explorerButton.IsVisibleInTree, "opening the Tools menu");
            explorerButton.EmitSignal(BaseButton.SignalName.Pressed);

            await WaitFor(tree, () => tree.CurrentScene is AutoEvoExploringTool &&
                !TransitionManager.Instance.HasQueuedTransitions, "entering the Auto-Evo explorer");

            var world = typeof(AutoEvoExploringTool).GetField("world", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(tree.CurrentScene)!;
            var game = (GameProperties)world.GetType().GetField(nameof(GameProperties))!.GetValue(world)!;
            if (!game.GameWorld.WorldSettings.LAWK)
                throw new InvalidOperationException("Expected the default explorer world's LAWK to be true, got false");

            GD.Print("PASS: default explorer world LAWK");
            exitCode = 0;
        }
        catch (Exception e)
        {
            GD.PrintErr(e);
        }
        finally
        {
            feed.Value = previousFeedValue;
            tree.Quit(exitCode);
        }
    }
}
