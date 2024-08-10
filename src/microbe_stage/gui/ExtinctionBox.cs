using Godot;

/// <summary>
///   Game over screen for the player when they are extinct
/// </summary>
public partial class ExtinctionBox : CustomWindow
{
    [Export]
    public NodePath? ExtinctionMenuPath;

    [Export]
    public NodePath LoadMenuPath = null!;

    [Export]
    public NodePath ExtinctionMessagePath;

#pragma warning disable CA2213
    private Control extinctionMenu = null!;
    private Control loadMenu = null!;
    private Label extinctionMessage = null!;

    [Export]
    private CustomRichTextLabel continueText = null!;

    [Export]
    private Button? continueButton;
#pragma warning restore CA2213

    private Species? speciesToContinueAs;

    [Signal]
    public delegate void ContinueSelectedEventHandler(uint species);

    public Species? ShowContinueAs
    {
        get => speciesToContinueAs;
        set
        {
            if (speciesToContinueAs == value)
                return;

            speciesToContinueAs = value;
            UpdateContinueOption();
        }
    }

    public override void _Ready()
    {
        extinctionMenu = GetNode<Control>(ExtinctionMenuPath);
        loadMenu = GetNode<Control>(LoadMenuPath);
        extinctionMessage = GetNode<Label>(ExtinctionMessagePath);

        UpdateContinueOption();
    }

    public override void _EnterTree()
    {
        base._EnterTree();

        InputManager.RegisterReceiver(this);
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        InputManager.UnregisterReceiver(this);
    }

    [RunOnKeyDown("ui_cancel", Priority = Constants.SUBMENU_CANCEL_PRIORITY)]
    public bool LoadMenuEscapePressed()
    {
        if (!loadMenu.Visible)
            return false;

        loadMenu.Hide();
        extinctionMenu.Show();
        return true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (ExtinctionMenuPath != null)
            {
                ExtinctionMenuPath.Dispose();
                LoadMenuPath.Dispose();
            }
        }

        base.Dispose(disposing);
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

        // This now just prints an error if we are paused here, this used to unpause but when refactoring the pausing
        // what should have paused the game before this wasn't found so this was probably just a safety check
        if (PauseManager.Instance.Paused)
            GD.PrintErr("Game is unexpectedly paused when closing the extinction screen");

        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeOut, 0.1f, OnSwitchToMenu, false);
    }

    /// <summary>
    ///   Finishes the transition back to the main menu
    /// </summary>
    private void OnSwitchToMenu()
    {
        SceneManager.Instance.ReturnToMenu();
    }

    private void OnContinue()
    {
        if (ShowContinueAs == null)
        {
            GD.PrintErr("Species to continue as not set");
            return;
        }

        GUICommon.Instance.PlayButtonPressSound();

        GD.Print("Attempting to continue game as species: ", ShowContinueAs.ID);

        if (continueButton != null)
        {
            continueButton.Disabled = true;
        }
        else
        {
            GD.PrintErr("Cannot disable continue button as it doesn't exist");
        }

        // Play an animation to hide potential patch switch
        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeOut, 0.4f, () =>
        {
            EmitSignal(SignalName.ContinueSelected, ShowContinueAs.ID);

            // And fade in after triggering the continue option
            TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeIn, 0.4f);
        });
    }

    private void UpdateContinueOption()
    {
        if (continueButton == null)
            return;

        if (ShowContinueAs != null)
        {
            GetNode<Label>(ExtinctionMessagePath).Text = Localization.Translate("EXTINCTION_BOX_TEXT_CONTINUE");

            continueText.ExtendedBbcode = Localization.Translate("CONTINUE_AS_SPECIES")
                .FormatSafe(ShowContinueAs.FormattedNameBbCode,
                    StringUtils.ThreeDigitFormat(ShowContinueAs.Population));
            continueText.Visible = true;

            continueButton.Visible = true;
        }
        else
        {
            continueButton.Visible = false;
            continueText.Visible = false;
        }
    }
}
