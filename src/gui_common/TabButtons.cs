using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Manages showing tab buttons and making them keyboard and controller navigable
/// </summary>
public class TabButtons : HBoxContainer
{
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
