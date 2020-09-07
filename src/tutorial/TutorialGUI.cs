using System;
using Godot;

/// <summary>
///   GUI control that contains the tutorial. Should be placed over any game state GUI so that things drawn
///   by this are on top. Controlled by Tutorial object
/// </summary>
public class TutorialGUI : Control
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

    /// <summary>
    ///   True when the tutorial selected boxes have been left untouched (on)
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     There's a small bug where if the tutorials are turned back on and then displayed, that leaves the checkboxes
    ///     unchecked, when things become visible all the enable tutorials check boxes would need to read this value
    ///     to fix this
    ///   </para>
    /// </remarks>
    private bool tutorialEnabledSelected = true;

    private WindowDialog microbeWelcomeMessage;
    private Control microbeMovementKeyPrompts;
    private Control microbeMovementKeyForward;
    private Control microbeMovementKeyLeft;
    private Control microbeMovementKeyRight;
    private Control microbeMovementKeyBackwards;
    private WindowDialog microbeMovementPopup;
    private WindowDialog glucoseTutorial;
    private WindowDialog stayingAlive;
    private WindowDialog reproductionTutorial;

    public ITutorialInput EventReceiver { get; set; }

    /// <summary>
    ///   Used to ignore reporting closing back to whoever is setting the visible properties
    /// </summary>
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
                microbeWelcomeMessage.PopupCentered();
            }
            else
            {
                microbeWelcomeMessage.Visible = false;
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
                microbeMovementPopup.Visible = false;
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
                glucoseTutorial.Visible = false;
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
                stayingAlive.Visible = false;
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
                reproductionTutorial.Visible = false;
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
        microbeWelcomeMessage = GetNode<WindowDialog>(MicrobeWelcomeMessagePath);
        microbeMovementKeyPrompts = GetNode<Control>(MicrobeMovementKeyPromptsPath);
        microbeMovementPopup = GetNode<WindowDialog>(MicrobeMovementPopupPath);
        microbeMovementKeyForward = GetNode<Control>(MicrobeMovementKeyForwardPath);
        microbeMovementKeyLeft = GetNode<Control>(MicrobeMovementKeyLeftPath);
        microbeMovementKeyRight = GetNode<Control>(MicrobeMovementKeyRightPath);
        microbeMovementKeyBackwards = GetNode<Control>(MicrobeMovementKeyBackwardsPath);
        glucoseTutorial = GetNode<WindowDialog>(GlucoseTutorialPath);
        stayingAlive = GetNode<WindowDialog>(StayingAlivePath);
        reproductionTutorial = GetNode<WindowDialog>(ReproductionTutorialPath);
    }

    /// <summary>
    ///   This passes time to the TutorialState as this Node doesn't stop processing on pause
    /// </summary>
    public override void _Process(float delta)
    {
        // Just to make sure this is reset properly
        IsClosingAutomatically = false;

        // Let our attached tutorial controller do stuff
        EventReceiver?.Process(this, delta);
    }

    /// <summary>
    ///   A button that closes all tutorials was pressed by the user
    /// </summary>
    private void OnClickedCloseAll()
    {
        if (IsClosingAutomatically)
            return;

        GUICommon.Instance.PlayButtonPressSound();
        EventReceiver?.OnTutorialClosed();

        if (!tutorialEnabledSelected)
        {
            EventReceiver?.OnTutorialDisabled();
        }
    }

    private void OnSpecificCloseClicked(string closedThing)
    {
        if (IsClosingAutomatically)
            return;

        GUICommon.Instance.PlayButtonPressSound();
        EventReceiver?.OnCurrentTutorialClosed(closedThing);
    }

    private void OnTutorialEnabledValueChanged(bool value)
    {
        tutorialEnabledSelected = value;
    }
}
