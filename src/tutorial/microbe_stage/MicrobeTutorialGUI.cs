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
    public NodePath OrganelleDivisionTutorialPath = null!;

    [Export]
    public NodePath BecomeMulticellularTutorialPath = null!;

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
    private TutorialDialog microbeWelcomeMessage = null!;
    private Control microbeMovementKeyPrompts = null!;
    private Control microbeMovementKeyForward = null!;
    private Control microbeMovementKeyLeft = null!;
    private Control microbeMovementKeyRight = null!;
    private Control microbeMovementKeyBackwards = null!;
    private CustomWindow microbeMovementPopup = null!;
    private CustomWindow glucoseTutorial = null!;
    private CustomWindow stayingAlive = null!;
    private CustomWindow reproductionTutorial = null!;
    private CustomWindow editorButtonTutorial = null!;
    private CustomWindow unbindTutorial = null!;
    private CustomWindow checkTheHelpMenu = null!;
    private CustomWindow engulfmentExplanation = null!;
    private CustomWindow engulfedExplanation = null!;
    private CustomWindow engulfmentFullCapacity = null!;
    private CustomWindow leaveColonyTutorial = null!;
    private CustomWindow earlyMulticellularWelcome = null!;
    private CustomWindow dayNightTutorial = null!;
    private CustomWindow becomeMulticellularTutorial = null!;
    private CustomWindow organelleDivisionTutorial = null!;
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
        set
        {
            if (value == microbeMovementKeyPrompts.Visible)
                return;

            microbeMovementKeyPrompts.Visible = value;

            // Apply visible to children to make the key prompts visible. This saves a lot of processing time overall
            foreach (Control child in microbeMovementKeyPrompts.GetChildren())
            {
                child.Visible = value;
            }
        }
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

    public bool OrganelleDivisionTutorialVisible
    {
        get => organelleDivisionTutorial.Visible;
        set
        {
            if (value == organelleDivisionTutorial.Visible)
                return;

            organelleDivisionTutorial.Visible = value;
        }
    }

    public bool BecomeMulticellularTutorialVisible
    {
        get => becomeMulticellularTutorial.Visible;
        set
        {
            if (value == becomeMulticellularTutorial.Visible)
                return;

            becomeMulticellularTutorial.Visible = value;
        }
    }

    public override void _Ready()
    {
        microbeWelcomeMessage = GetNode<TutorialDialog>(MicrobeWelcomeMessagePath);
        microbeMovementKeyPrompts = GetNode<Control>(MicrobeMovementKeyPromptsPath);
        microbeMovementPopup = GetNode<CustomWindow>(MicrobeMovementPopupPath);
        microbeMovementKeyForward = GetNode<Control>(MicrobeMovementKeyForwardPath);
        microbeMovementKeyLeft = GetNode<Control>(MicrobeMovementKeyLeftPath);
        microbeMovementKeyRight = GetNode<Control>(MicrobeMovementKeyRightPath);
        microbeMovementKeyBackwards = GetNode<Control>(MicrobeMovementKeyBackwardsPath);
        glucoseTutorial = GetNode<CustomWindow>(GlucoseTutorialPath);
        stayingAlive = GetNode<CustomWindow>(StayingAlivePath);
        reproductionTutorial = GetNode<CustomWindow>(ReproductionTutorialPath);
        editorButtonTutorial = GetNode<CustomWindow>(EditorButtonTutorialPath);
        unbindTutorial = GetNode<CustomWindow>(UnbindTutorialPath);
        checkTheHelpMenu = GetNode<CustomWindow>(CheckTheHelpMenuPath);
        engulfmentExplanation = GetNode<CustomWindow>(EngulfmentExplanationPath);
        engulfedExplanation = GetNode<CustomWindow>(EngulfedExplanationPath);
        engulfmentFullCapacity = GetNode<CustomWindow>(EngulfmentFullCapacityPath);
        leaveColonyTutorial = GetNode<CustomWindow>(LeaveColonyTutorialPath);
        earlyMulticellularWelcome = GetNode<CustomWindow>(EarlyMulticellularWelcomePath);
        dayNightTutorial = GetNode<CustomWindow>(DayNightTutorialPath);
        becomeMulticellularTutorial = GetNode<CustomWindow>(BecomeMulticellularTutorialPath);
        organelleDivisionTutorial = GetNode<CustomWindow>(OrganelleDivisionTutorialPath);

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

    public void SetWelcomeTextForLifeOrigin(WorldGenerationSettings.LifeOrigin gameLifeOrigin)
    {
        microbeWelcomeMessage.Description = gameLifeOrigin switch
        {
            WorldGenerationSettings.LifeOrigin.Vent => "MICROBE_STAGE_INITIAL",
            WorldGenerationSettings.LifeOrigin.Pond => "MICROBE_STAGE_INITIAL_POND",
            WorldGenerationSettings.LifeOrigin.Panspermia => "MICROBE_STAGE_INITIAL_PANSPERMIA",
            _ => throw new ArgumentOutOfRangeException(nameof(gameLifeOrigin), gameLifeOrigin,
                "Unhandled life origin for tutorial message"),
        };
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
                OrganelleDivisionTutorialPath.Dispose();
                BecomeMulticellularTutorialPath.Dispose();
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

    private void DummyKeepInitialTextTranslations()
    {
        TranslationServer.Translate("MICROBE_STAGE_INITIAL_POND");
        TranslationServer.Translate("MICROBE_STAGE_INITIAL_PANSPERMIA");
    }
}
