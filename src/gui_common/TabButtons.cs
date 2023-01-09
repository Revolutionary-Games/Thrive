using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Manages showing tab buttons and making them keyboard and controller navigable
/// </summary>
public class TabButtons : HBoxContainer
{
    /// <summary>
    ///   When true the tab left and right change buttons loop to the other side when the end is reached
    /// </summary>
    [Export]
    public bool TabsLoop;

    [Export]
    public NodePath LeftContainerPath = null!;

    [Export]
    public NodePath LeftButtonIndicatorPath = null!;

    [Export]
    public NodePath RightContainerPath = null!;

    [Export]
    public NodePath RightButtonIndicatorPath = null!;

    [Export]
    public NodePath TabButtonsContainerPath = null!;

    private readonly List<Control> tabButtons = new();

    private TabLevel levelOnScreen = TabLevel.Primary;

    private Container? leftContainer;
    private KeyPrompt? leftButtonIndicator;

    private Container rightContainer = null!;
    private KeyPrompt rightButtonIndicator = null!;

    private Container tabButtonsContainer = null!;

    /// <summary>
    ///   The level of tabs this Control controls. Only one tab control of each level may be enabled at once, otherwise
    ///   unexpected stuff will happen
    /// </summary>
    [Export]
    public TabLevel LevelOnScreen
    {
        get => levelOnScreen;
        set
        {
            levelOnScreen = value;
            UpdateChangeButtonVisibility();
            UpdateChangeButtonActionNames();
        }
    }

    public override void _Ready()
    {
        leftContainer = GetNode<Container>(LeftContainerPath);
        leftButtonIndicator = GetNode<KeyPrompt>(LeftButtonIndicatorPath);

        rightContainer = GetNode<Container>(RightContainerPath);
        rightButtonIndicator = GetNode<KeyPrompt>(RightButtonIndicatorPath);

        tabButtonsContainer = GetNode<Container>(TabButtonsContainerPath);

        UpdateChangeButtonActionNames();

        AdjustSceneAddedChildren();

        CheckControllerInput(null, EventArgs.Empty);
    }

    public override void _EnterTree()
    {
        base._EnterTree();

        KeyPromptHelper.IconsChanged += CheckControllerInput;

        InputManager.RegisterReceiver(this);

        // Skip updating stuff before ready is called
        if (leftContainer == null)
            return;

        AdjustSceneAddedChildren();

        CheckControllerInput(null, EventArgs.Empty);
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        KeyPromptHelper.IconsChanged -= CheckControllerInput;

        InputManager.UnregisterReceiver(this);
    }

    public void AddNewTab(Button button)
    {
        button.FocusMode = FocusModeEnum.None;
        tabButtonsContainer.AddChild(button);
        tabButtons.Add(button);
    }

    public void ClearTabButtons()
    {
        foreach (var tabButton in tabButtons)
        {
            tabButton.QueueFree();
        }

        tabButtons.Clear();
    }

    // Due to the way our input system works, we need to listen to all the types of inputs at once and then after
    // receiving the input determine if those inputs were relevant for us

    [RunOnKeyDown("ui_tab_right", Priority = 1)]
    public bool OnNextPrimaryTab()
    {
        if (LevelOnScreen != TabLevel.Primary)
            return false;

        if (!IsVisibleInTree())
            return false;

        TryToMoveToNextTab();

        // For now always consume without checking if tab change actually worked
        return true;
    }

    [RunOnKeyDown("ui_tab_secondary_right", Priority = 1)]
    public bool OnNextSecondaryTab()
    {
        if (LevelOnScreen != TabLevel.Secondary)
            return false;

        if (!IsVisibleInTree())
            return false;

        TryToMoveToNextTab();

        return true;
    }

    [RunOnKeyDown("ui_tab_left", Priority = 1)]
    public bool OnPreviousPrimaryTab()
    {
        if (LevelOnScreen != TabLevel.Primary)
            return false;

        if (!IsVisibleInTree())
            return false;

        TryToMoveToPreviousTab();

        return true;
    }

    [RunOnKeyDown("ui_tab_secondary_left", Priority = 1)]
    public bool OnPreviousSecondaryTab()
    {
        if (LevelOnScreen != TabLevel.Secondary)
            return false;

        if (!IsVisibleInTree())
            return false;

        TryToMoveToPreviousTab();

        return true;
    }

    private void TryToMoveToNextTab()
    {
        bool foundPressed = false;
        Button? firstTab = null;

        foreach (var potentialButton in tabButtons)
        {
            if (potentialButton is Button button)
            {
                firstTab ??= button;

                if (foundPressed)
                {
                    // Found an unpressed button
                    // We assume here that clicking the new button will correctly clear the old one as otherwise
                    // mouse control would be broken as well
                    button.Pressed = true;
                    return;
                }

                if (button.Pressed)
                {
                    foundPressed = true;
                }
            }
        }

        if (firstTab == null)
        {
            GD.PrintErr("TabButtons not setup correctly");
            return;
        }

        // If we are specified as looping, move back to the start
        if (TabsLoop)
        {
            firstTab.Pressed = true;
        }
    }

    private void TryToMoveToPreviousTab()
    {
        Button? previousButton = null;

        foreach (var potentialButton in tabButtons)
        {
            if (potentialButton is Button button)
            {
                if (button.Pressed)
                {
                    // When we find a pressed button, we want to move to press the previously seen button
                    if (previousButton != null)
                    {
                        previousButton.Pressed = true;
                        break;
                    }

                    // If we don't have a previous button, then we loop until the end and rely on the previous button
                    // variable
                }

                previousButton = button;
            }
        }

        if (previousButton == null)
        {
            GD.PrintErr("TabButtons not setup correctly for previous tab action");
            return;
        }

        // If we are specified as looping, move to the last tab
        if (TabsLoop)
        {
            previousButton.Pressed = true;
        }
    }

    /// <summary>
    ///   This control supports either dynamically adding the tabs through code or in the scene tree so this method is
    ///   needed to put the child buttons in the right place
    /// </summary>
    private void AdjustSceneAddedChildren()
    {
        foreach (Control child in GetChildren())
        {
            // Move all children that aren't our own scene set children to the buttons container
            if (child.Equals(leftContainer) || child.Equals(rightContainer) || child.Equals(tabButtonsContainer))
                continue;

            // Found a button to move
            tabButtons.Add(child);
            child.ReParent(tabButtonsContainer);
        }
    }

    private void CheckControllerInput(object? sender, EventArgs e)
    {
        UpdateChangeButtonVisibility();
    }

    private void UpdateChangeButtonVisibility()
    {
        if (leftContainer == null)
            return;

        // TODO: for people who want to purely use a keyboard we need an option to always show the tab navigation
        // buttons
        if (KeyPromptHelper.InputMethod == ActiveInputMethod.Controller && LevelOnScreen != TabLevel.Uncontrollable)
        {
            leftContainer.Visible = true;
            rightContainer.Visible = true;
        }
        else
        {
            leftContainer.Visible = false;
            rightContainer.Visible = false;
        }
    }

    private void UpdateChangeButtonActionNames()
    {
        if (leftButtonIndicator == null)
            return;

        string wantedLeft;
        string wantedRight;

        switch (LevelOnScreen)
        {
            case TabLevel.Primary:
                wantedLeft = "ui_tab_left";
                wantedRight = "ui_tab_right";
                break;
            case TabLevel.Secondary:
                wantedLeft = "ui_tab_secondary_left";
                wantedRight = "ui_tab_secondary_right";
                break;
            case TabLevel.Uncontrollable:
                // When uncontrollable the control buttons are hidden so we don't need to update the actions
                return;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (leftButtonIndicator.ActionName != wantedLeft)
        {
            leftButtonIndicator.ActionName = wantedLeft;
            leftButtonIndicator.Refresh();
        }

        if (rightButtonIndicator.ActionName != wantedRight)
        {
            rightButtonIndicator.ActionName = wantedRight;
            rightButtonIndicator.Refresh();
        }
    }
}
