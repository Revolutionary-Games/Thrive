using System;
using Godot;

/// <summary>
///   GUI control that contains the microbe stage tutorial.
///   Should be placed over any game state GUI so that things drawn by this are on top. Visibility of things is
///   Controlled by TutorialState object
/// </summary>
public class MicrobeTutorialGUI : Control, ITutorialGUI
{
    [Export]
    public NodePath MicrobeWelcomeMessagePath;

    [Export]
    public NodePath MicrobeMovementKeyPromptsPath;

    [Export]
    public NodePath MicrobeMovementPopupPath;

    [Export]
    public NodePath MicrobeMovementKeyForwardPath;

    [Export]
    public NodePath MicrobeMovementKeyLeftPath;

    [Export]
    public NodePath MicrobeMovementKeyRightPath;

    [Export]
    public NodePath MicrobeMovementKeyBackwardsPath;

    [Export]
    public NodePath GlucoseTutorialPath;

    [Export]
    public NodePath StayingAlivePath;

    [Export]
    public NodePath ReproductionTutorialPath;

    [Export]
    public NodePath UnbindTutorialPath;

    private CustomDialog microbeWelcomeMessage;
    private Control microbeMovementKeyPrompts;
    private Control microbeMovementKeyForward;
    private Control microbeMovementKeyLeft;
    private Control microbeMovementKeyRight;
    private Control microbeMovementKeyBackwards;
    private CustomDialog microbeMovementPopup;
    private CustomDialog glucoseTutorial;
    private CustomDialog stayingAlive;
    private CustomDialog reproductionTutorial;
    private CustomDialog unbindTutorial;

    public ITutorialInput EventReceiver { get; set; }

    public MainGameState AssociatedGameState { get; } = MainGameState.MicrobeStage;

    public bool TutorialEnabledSelected { get; private set; } = true;

    public Node GUINode => this;

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
                microbeWelcomeMessage.ClosePopup();
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
                microbeMovementPopup.ClosePopup();
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
                glucoseTutorial.ClosePopup();
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
                stayingAlive.ClosePopup();
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
                reproductionTutorial.ClosePopup();
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
                unbindTutorial.ClosePopup();
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
        unbindTutorial = GetNode<CustomDialog>(UnbindTutorialPath);
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
}
