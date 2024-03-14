using System;
using Godot;

/// <summary>
///   Shows or hides this Node automatically based on the current <see cref="KeyPromptHelper.InputMethod"/>
/// </summary>
/// <remarks>
///   <para>
///     TODO: this class didn't end up being used (TutorialDialog was updated to support this functionality) so this is
///     untested currently
///   </para>
/// </remarks>
public partial class ShowWhenInputTypeMatches : Control
{
    /// <summary>
    ///   The input method to check and make this visible based on
    /// </summary>
    [Export]
    public ActiveInputMethod InputMethod = ActiveInputMethod.Controller;

    /// <summary>
    ///   If set to true the check on <see cref="InputMethod"/> is reversed
    /// </summary>
    [Export]
    public bool ReverseCheck;

    public override void _Ready()
    {
    }

    public override void _EnterTree()
    {
        base._EnterTree();

        CheckVisibility();

        KeyPromptHelper.IconsChanged += OnInputTypeChanged;
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        KeyPromptHelper.IconsChanged -= OnInputTypeChanged;
    }

    private void OnInputTypeChanged(object sender, EventArgs e)
    {
        CheckVisibility();
    }

    private void CheckVisibility()
    {
        var activeMethod = KeyPromptHelper.InputMethod;

        bool shouldBeVisible;

        if (ReverseCheck)
        {
            shouldBeVisible = InputMethod != activeMethod;
        }
        else
        {
            shouldBeVisible = InputMethod == activeMethod;
        }

        Visible = shouldBeVisible;
    }
}
