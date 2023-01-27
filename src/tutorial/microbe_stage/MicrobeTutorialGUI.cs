using System;
using Godot;
using Tutorial;

/// <summary>
///   GUI control that contains the microbe stage tutorial.
///   Should be placed over any game state GUI so that things drawn by this are on top. Visibility of things is
///   Controlled by TutorialState object
/// </summary>
public class MicrobeTutorialGUI : Control, ITutorialGUI
{
    [Export]
    public NodePath? MicrobeWelcomeMessagePath;

    [Export]
    public NodePath MicrobeMovementKeyPromptsPath = null!;

    [Export]
    public NodePath MicrobeMovementPopupPath = null!;

    [Export]
    public NodePath MicrobeMovementKeyForwardPath = null!;

    [Export]
    public NodePath MicrobeMovementKeyLeftPath = null!;

    [Export]
    public NodePath MicrobeMovementKeyRightPath = null!;

    [Export]
    public NodePath MicrobeMovementKeyBackwardsPath = null!;

    [Export]
    public NodePath GlucoseTutorialPath = null!;

    [Export]
    public NodePath StayingAlivePath = null!;

    [Export]
    public NodePath ReproductionTutorialPath = null!;

    [Export]
    public NodePath EditorButtonTutorialPath = null!;

    [Export]
    public NodePath UnbindTutorialPath = null!;

    [Export]
    public NodePath LeaveColonyTutorialPath = null!;

    [Export]
    public NodePath EarlyMulticellularWelcomePath = null!;

    [Export]
    public NodePath DayNightTutorialPath = null!;

    [Export]
    public NodePath CheckTheHelpMenuPath = null!;

    [Export]
    public NodePath EngulfmentExplanationPath = null!;

    [Export]
    public NodePath EngulfedExplanationPath = null!;

    [Export]
    public NodePath EngulfmentFullCapacityPath = null!;

    [Export]
    public NodePath EditorButtonHighlightPath = null!;

#pragma warning disable CA2213
    private CustomDialog microbeWelcomeMessage = null!;
    private Control microbeMovementKeyPrompts = null!;
    private Control microbeMovementKeyForward = null!;
    private Control microbeMovementKeyLeft = null!;
    private Control microbeMovementKeyRight = null!;
    private Control microbeMovementKeyBackwards = null!;
    private CustomDialog microbeMovementPopup = null!;
    private CustomDialog glucoseTutorial = null!;
    private CustomDialog stayingAlive = null!;
    private CustomDialog reproductionTutorial = null!;
    private CustomDialog editorButtonTutorial = null!;
    private CustomDialog unbindTutorial = null!;
    private CustomDialog checkTheHelpMenu = null!;
    private CustomDialog engulfmentExplanation = null!;
    private CustomDialog engulfedExplanation = null!;
    private CustomDialog engulfmentFullCapacity = null!;
    private CustomDialog leaveColonyTutorial = null!;
    private CustomDialog earlyMulticellularWelcome = null!;
    private CustomDialog dayNightTutorial = null!;
#pragma warning restore CA2213

    [Signal]
    public delegate void OnHelpMenuOpenRequested();

    public ITutorialInput? EventReceiver { get; set; }

    public MainGameState AssociatedGameState => MainGameState.MicrobeStage;

    public bool TutorialEnabledSelected { get; private set; } = true;

    public Node GUINode => this;

    public ControlHighlight? PressEditorButtonHighlight { get; private set; }

    public bool IsClosingAutomatically { get; set; }

    public bool MicrobeWelcomeVisible
    {
        get => microbeWelcomeMessage.Visible;
        set
        {
            if (value == microbeWelcomeMessage.Visible)
                return;

            if (value)
            {
                microbeWelcomeMessage.PopupCenteredShrink();
            }
            else
            {
                microbeWelcomeMessage.Hide();
            }
        }
    }

    public bool MicrobeMovementPromptsVisible
    {
        get => microbeMovementKeyPrompts.Visible;
        set => microbeMovementKeyPrompts.Visible = value;
    }

    public bool MicrobeMovementPopupVisible
    {
        get => microbeMovementPopup.Visible;
        set
        {
            if (value == microbeMovementPopup.Visible)
                return;

            if (value)
            {
                microbeMovementPopup.Show();
            }
            else
            {
                microbeMovementPopup.Hide();
            }
        }
    }

    public bool GlucoseTutorialVisible
    {
        get => glucoseTutorial.Visible;
        set
        {
            if (value == glucoseTutorial.Visible)
                return;

            if (value)
            {
                glucoseTutorial.Show();
            }
            else
            {
                glucoseTutorial.Hide();
            }
        }
    }

    public bool StayingAliveVisible
    {
        get => stayingAlive.Visible;
        set
        {
            if (value == stayingAlive.Visible)
                return;

            if (value)
            {
                stayingAlive.Show();
            }
            else
            {
                stayingAlive.Hide();
            }
        }
    }

    public bool ReproductionTutorialVisible
    {
        get => reproductionTutorial.Visible;
        set
        {
            if (value == reproductionTutorial.Visible)
                return;

            if (value)
            {
                reproductionTutorial.Show();
            }
            else
            {
                reproductionTutorial.Hide();
            }
        }
    }

    public bool EditorButtonTutorialVisible
    {
        get => editorButtonTutorial.Visible;
        set
        {
            if (value == editorButtonTutorial.Visible)
                return;

            if (value)
            {
                editorButtonTutorial.Show();
            }
            else
            {
                editorButtonTutorial.Hide();
            }
        }
    }

    public bool UnbindTutorialVisible
    {
        get => unbindTutorial.Visible;
        set
        {
            if (value == unbindTutorial.Visible)
                return;

            if (value)
            {
                unbindTutorial.Show();
            }
            else
            {
                unbindTutorial.Hide();
            }
        }
    }

    public bool LeaveColonyTutorialVisible
    {
        get => leaveColonyTutorial.Visible;
        set
        {
            if (value == leaveColonyTutorial.Visible)
                return;

            if (value)
            {
                leaveColonyTutorial.Show();
            }
            else
            {
                leaveColonyTutorial.Hide();
            }
        }
    }

    public bool EarlyMulticellularWelcomeVisible
    {
        get => earlyMulticellularWelcome.Visible;
        set
        {
            if (value == earlyMulticellularWelcome.Visible)
                return;

            if (value)
            {
                earlyMulticellularWelcome.PopupCenteredShrink();
            }
            else
            {
                earlyMulticellularWelcome.Hide();
            }
        }
    }

    public float MicrobeMovementRotation
    {
        get => microbeMovementKeyPrompts.RectRotation;
        set
        {
            if (Math.Abs(value - microbeMovementKeyPrompts.RectRotation) < 0.01f)
                return;

            microbeMovementKeyPrompts.RectRotation = value;
        }
    }

    public bool MicrobeMovementPromptForwardVisible
    {
        get => microbeMovementKeyForward.Visible;
        set => microbeMovementKeyForward.Visible = value;
    }

    public bool MicrobeMovementPromptLeftVisible
    {
        get => microbeMovementKeyLeft.Visible;
        set => microbeMovementKeyLeft.Visible = value;
    }

    public bool MicrobeMovementPromptRightVisible
    {
        get => microbeMovementKeyRight.Visible;
        set => microbeMovementKeyRight.Visible = value;
    }

    public bool MicrobeMovementPromptBackwardsVisible
    {
        get => microbeMovementKeyBackwards.Visible;
        set => microbeMovementKeyBackwards.Visible = value;
    }

    public bool CheckTheHelpMenuVisible
    {
        get => checkTheHelpMenu.Visible;
        set
        {
            if (value == checkTheHelpMenu.Visible)
                return;

            if (value)
            {
                checkTheHelpMenu.Show();
            }
            else
            {
                checkTheHelpMenu.Hide();
            }
        }
    }

    public bool EngulfmentExplanationVisible
    {
        get => engulfmentExplanation.Visible;
        set
        {
            if (value == engulfmentExplanation.Visible)
                return;

            engulfmentExplanation.Visible = value;
        }
    }

    public bool EngulfedExplanationVisible
    {
        get => engulfedExplanation.Visible;
        set
        {
            if (value == engulfedExplanation.Visible)
                return;

            engulfedExplanation.Visible = value;
        }
    }

    public bool EngulfmentFullCapacityVisible
    {
        get => engulfmentFullCapacity.Visible;
        set
        {
            if (value == engulfmentFullCapacity.Visible)
                return;

            engulfmentFullCapacity.Visible = value;
        }
    }

    public bool DayNightTutorialVisible
    {
        get => dayNightTutorial.Visible;
        set
        {
            if (value == dayNightTutorial.Visible)
                return;

            dayNightTutorial.Visible = value;
        }
    }

    public override void _Ready()
    {
        microbeWelcomeMessage = GetNode<CustomDialog>(MicrobeWelcomeMessagePath);
        microbeMovementKeyPrompts = GetNode<Control>(MicrobeMovementKeyPromptsPath);
        microbeMovementPopup = GetNode<CustomDialog>(MicrobeMovementPopupPath);
        microbeMovementKeyForward = GetNode<Control>(MicrobeMovementKeyForwardPath);
        microbeMovementKeyLeft = GetNode<Control>(MicrobeMovementKeyLeftPath);
        microbeMovementKeyRight = GetNode<Control>(MicrobeMovementKeyRightPath);
        microbeMovementKeyBackwards = GetNode<Control>(MicrobeMovementKeyBackwardsPath);
        glucoseTutorial = GetNode<CustomDialog>(GlucoseTutorialPath);
        stayingAlive = GetNode<CustomDialog>(StayingAlivePath);
        reproductionTutorial = GetNode<CustomDialog>(ReproductionTutorialPath);
        editorButtonTutorial = GetNode<CustomDialog>(EditorButtonTutorialPath);
        unbindTutorial = GetNode<CustomDialog>(UnbindTutorialPath);
        checkTheHelpMenu = GetNode<CustomDialog>(CheckTheHelpMenuPath);
        engulfmentExplanation = GetNode<CustomDialog>(EngulfmentExplanationPath);
        engulfedExplanation = GetNode<CustomDialog>(EngulfedExplanationPath);
        engulfmentFullCapacity = GetNode<CustomDialog>(EngulfmentFullCapacityPath);
        leaveColonyTutorial = GetNode<CustomDialog>(LeaveColonyTutorialPath);
        earlyMulticellularWelcome = GetNode<CustomDialog>(EarlyMulticellularWelcomePath);
        dayNightTutorial = GetNode<CustomDialog>(DayNightTutorialPath);

        PressEditorButtonHighlight = GetNode<ControlHighlight>(EditorButtonHighlightPath);

        PauseMode = PauseModeEnum.Process;
    }

    public override void _Process(float delta)
    {
        TutorialHelper.ProcessTutorialGUI(this, delta);
    }

    public void OnClickedCloseAll()
    {
        TutorialHelper.HandleCloseAllForGUI(this);
    }

    public void OnSpecificCloseClicked(string closedThing)
    {
        TutorialHelper.HandleCloseSpecificForGUI(this, closedThing);
    }

    public void OnTutorialEnabledValueChanged(bool value)
    {
        TutorialEnabledSelected = value;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (MicrobeWelcomeMessagePath != null)
            {
                MicrobeWelcomeMessagePath.Dispose();
                MicrobeMovementKeyPromptsPath.Dispose();
                MicrobeMovementPopupPath.Dispose();
                MicrobeMovementKeyForwardPath.Dispose();
                MicrobeMovementKeyLeftPath.Dispose();
                MicrobeMovementKeyRightPath.Dispose();
                MicrobeMovementKeyBackwardsPath.Dispose();
                GlucoseTutorialPath.Dispose();
                StayingAlivePath.Dispose();
                ReproductionTutorialPath.Dispose();
                EditorButtonTutorialPath.Dispose();
                UnbindTutorialPath.Dispose();
                LeaveColonyTutorialPath.Dispose();
                EarlyMulticellularWelcomePath.Dispose();
                DayNightTutorialPath.Dispose();
                CheckTheHelpMenuPath.Dispose();
                EngulfmentExplanationPath.Dispose();
                EngulfedExplanationPath.Dispose();
                EngulfmentFullCapacityPath.Dispose();
                EditorButtonHighlightPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void CheckHelpMenuPressed()
    {
        TutorialHelper.HandleCloseSpecificForGUI(this, CheckTheHelpMenu.TUTORIAL_NAME);

        // Note that this opening while the tutorial box is still visible is a bit problematic due to:
        // https://github.com/Revolutionary-Games/Thrive/issues/2326
        EmitSignal(nameof(OnHelpMenuOpenRequested));
    }
}
