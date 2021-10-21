using Godot;

public class ExtinctionBox : Control
{
    [Export]
    public NodePath ExtinctionMenuPath;

    [Export]
    public NodePath LoadMenuPath;

    private Control extinctionMenu;
    private Control loadMenu;

    public override void _Ready()
    {
        extinctionMenu = GetNode<Control>(ExtinctionMenuPath);
        loadMenu = GetNode<Control>(LoadMenuPath);
    }

    private void OpenLoadGamePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        extinctionMenu.Hide();
        loadMenu.Show();
    }

    private void CloseLoadPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        loadMenu.Hide();
        extinctionMenu.Show();
    }

    private void ReturnToMenuPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        // Unpause the game
        GetTree().Paused = false;

        TransitionManager.Instance.AddScreenFade(ScreenFade.FadeType.FadeOut, 0.1f, false);
        TransitionManager.Instance.StartTransitions(this, nameof(OnSwitchToMenu));
    }

    /// <summary>
    ///   Finishes the transition back to the main menu
    /// </summary>
    private void OnSwitchToMenu()
    {
        SceneManager.Instance.ReturnToMenu();
    }
}
