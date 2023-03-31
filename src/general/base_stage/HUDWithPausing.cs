using Godot;
using Newtonsoft.Json;

/// <summary>
///   Separate HUD base that adds pausing, this is separate as both strategy and creature HUDs want pausing but maybe
///   there's something that won't want pausing in the future
/// </summary>
public abstract class HUDWithPausing : HUDBase
{
    [Export]
    public NodePath? PausePromptPath;

    [Export]
    public NodePath PauseInfoPath = null!;

#pragma warning disable CA2213
    private Control pausePrompt = null!;
    private CustomRichTextLabel pauseInfo = null!;
#pragma warning restore CA2213

    /// <summary>
    ///   For toggling paused with the pause button.
    /// </summary>
    [JsonIgnore]
    public bool Paused { get; private set; }

    /// <summary>
    ///   If this returns non-null value the help text / prompt for unpausing is shown when paused
    /// </summary>
    protected abstract string? UnPauseHelpText { get; }

    public override void _Ready()
    {
        base._Ready();

        pausePrompt = GetNode<Control>(PausePromptPath);
        pauseInfo = GetNode<CustomRichTextLabel>(PauseInfoPath);

        UpdatePausePrompt();
    }

    public virtual void PauseButtonPressed(bool buttonState)
    {
        if (menu.Visible)
        {
            return;
        }

        GUICommon.Instance.PlayButtonPressSound();

        Paused = !Paused;

        if (Paused)
        {
            pausePrompt.Show();

            // Pause the game
            PauseManager.Instance.AddPause(nameof(HUDWithPausing));
        }
        else
        {
            pausePrompt.Hide();

            // Unpause the game
            PauseManager.Instance.Resume(nameof(HUDWithPausing));
        }
    }

    /// <summary>
    ///   Makes sure the game is unpaused (at least by us)
    /// </summary>
    public void EnsureGameIsUnpausedForEditor()
    {
        if (!Paused)
            return;

        PauseButtonPressed(!Paused);

        if (PauseManager.Instance.Paused)
        {
            GD.PrintErr(
                "Unpausing the game after editor button (or other required unpaused state) press didn't work");
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (PausePromptPath != null)
            {
                PausePromptPath.Dispose();
                PauseInfoPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void UpdatePausePrompt()
    {
        var text = UnPauseHelpText;

        if (text != null)
        {
            pauseInfo.ExtendedBbcode = text;
        }
        else
        {
            pauseInfo.Visible = false;
        }
    }
}
