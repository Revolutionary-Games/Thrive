using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

/// <summary>
///   Checks worlds created through the Auto-Evo explorer GUI.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class AutoEvoExploringToolTests
{
    /// <summary>
    ///   Opening the explorer from the main menu should create a world restricted to life as we know it.
    ///   Read the created world's settings so a different default in the GUI can be detected.
    /// </summary>
    [TestCase]
    public async Task MainMenuExplorerCreatesLAWKWorld()
    {
        var manager = SceneManager.Instance;
        var tree = manager.GetTree();
        var testScene = tree.CurrentScene;

        // Keep the test runner attached while the menu switches to the explorer
        tree.CurrentScene = null;
        try
        {
            manager.ReturnToMenu();
            var menus = tree.CurrentScene!.GetNode("MenuContainers/Menus/VBoxContainer");
            menus.GetNode<Button>("MainMenu/Tools").EmitSignal(BaseButton.SignalName.Pressed);
            var explorerButton = menus.GetNode<Button>("Tools/AutoEvoExploring");
            var elapsed = Stopwatch.StartNew();
            while (!explorerButton.IsVisibleInTree() && elapsed.Elapsed.TotalSeconds < 10)
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

            AssertThat(explorerButton.IsVisibleInTree()).IsTrue();
            explorerButton.EmitSignal(BaseButton.SignalName.Pressed);

            elapsed.Restart();
            while ((tree.CurrentScene is not AutoEvoExploringTool || TransitionManager.Instance.HasQueuedTransitions) &&
                   elapsed.Elapsed.TotalSeconds < 10)
            {
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            }

            AssertThat(tree.CurrentScene).IsInstanceOf<AutoEvoExploringTool>();
            var world = typeof(AutoEvoExploringTool).GetField("world", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(tree.CurrentScene)!;
            var game = (GameProperties)world.GetType().GetField(nameof(GameProperties))!.GetValue(world)!;
            AssertThat(game.GameWorld.WorldSettings.LAWK).AppendFailureMessage("Default explorer world LAWK").IsTrue();
        }
        finally
        {
            manager.DetachCurrentScene();
            tree.CurrentScene = testScene;
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }
}
