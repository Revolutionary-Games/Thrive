using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Manages showing tab buttons and making them keyboard and controller navigable
/// </summary>
public partial class TabButtons : HBoxContainer
{
    /// <summary>
    ///   When true the tab left and right change buttons loop to the other side when the end is reached
    /// </summary>
    [Export]
    public bool TabsLoop;

    /// <summary>
    ///   When true, the tab buttons will not wrap if overflowing.
    /// </summary>
    [Export]
    public bool NoWrap;

    [Export]
    public PressType TabChangeTriggerMethod = PressType.PressedSignal;

    /// <summary>
    ///   If enabled the left/right tab switch indicators take up the same space they would even when they are
    ///   invisible
    /// </summary>
    [Export]
    public bool MoveIndicatorsTakeUpSpaceWhileInvisible;

    [Export]
    public NodePath? LeftContainerPath;

    [Export]
    public NodePath LeftPaddingPath = null!;

    [Export]
    public NodePath LeftButtonIndicatorPath = null!;

    [Export]
    public NodePath RightContainerPath = null!;

    [Export]
    public NodePath RightPaddingPath = null!;

    [Export]
    public NodePath RightButtonIndicatorPath = null!;

    [Export]
    public NodePath TabButtonsContainerPath = null!;

    [Export]
    public NodePath TabButtonsContainerNoWrapPath = null!;

    private readonly List<Control> tabButtons = new();

    private TabLevel levelOnScreen = TabLevel.Primary;

#pragma warning disable CA2213
    private Container? leftContainer;
    private Control leftPadding = null!;
    private KeyPrompt? leftButtonIndicator;

    private Container rightContainer = null!;
    private Control rightPadding = null!;
    private KeyPrompt rightButtonIndicator = null!;

    private Container tabButtonsContainer = null!;
    private Container tabButtonsContainerNoWrap = null!;
#pragma warning restore CA2213

    public enum PressType
    {
        SetPressedState,
        PressedSignal,
    }

    public bool NodeReferencesResolved { get; private set; }

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
        ResolveNodeReferences();

        // This is hidden in the editor to make other scenes nicer that use the tab buttons
        tabButtonsContainer.Visible = !NoWrap;
        tabButtonsContainerNoWrap.Visible = NoWrap;

        UpdateChangeButtonActionNames();

        AdjustSceneAddedChildren();

        CheckControllerInput(null, EventArgs.Empty);
    }

    public void ResolveNodeReferences()
    {
        if (NodeReferencesResolved)
            return;

        leftContainer = GetNode<Container>(LeftContainerPath);
        leftPadding = GetNode<Control>(LeftPaddingPath);
        leftButtonIndicator = GetNode<KeyPrompt>(LeftButtonIndicatorPath);

        rightContainer = GetNode<Container>(RightContainerPath);
        rightPadding = GetNode<Control>(RightPaddingPath);
        rightButtonIndicator = GetNode<KeyPrompt>(RightButtonIndicatorPath);

        tabButtonsContainer = GetNode<Container>(TabButtonsContainerPath);
        tabButtonsContainerNoWrap = GetNode<Container>(TabButtonsContainerNoWrapPath);

        NodeReferencesResolved = true;
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

        if (NoWrap)
        {
            tabButtonsContainerNoWrap.AddChild(button);
        }
        else
        {
            tabButtonsContainer.AddChild(button);
        }

        tabButtons.Add(button);
    }

    public void ClearTabButtons()
    {
        foreach (var tabButton in tabButtons)
        {
            tabButton.DetachAndQueueFree();
        }

        tabButtons.Clear();
    }

    /// <summary>
    ///   Maps a path from a static scene path to one that has been adjusted for the tab buttons modifying its children
    /// </summary>
    /// <param name="pathToTabs">The path to this instance of tabs</param>
    /// <param name="specificTabButtonPath">The specific button that needs adjusting</param>
    /// <returns>The adjusted path</returns>
    public NodePath GetAdjustedButtonPath(NodePath pathToTabs, NodePath specificTabButtonPath)
    {
        // When loading a save this can get called before _Ready is called so we ensure the node references are
        // up to date here
        ResolveNodeReferences();

        var inputString = specificTabButtonPath.ToString();
        var tabPathString = pathToTabs.ToString();

        if (inputString.StartsWith(tabPathString))
            inputString = inputString.Substring(tabPathString.Length + 1);

        return new NodePath(
            $"{tabPathString}/{(NoWrap ? tabButtonsContainerNoWrap.Name : tabButtonsContainer.Name)}/{inputString}");
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (LeftContainerPath != null)
            {
                LeftContainerPath.Dispose();
                LeftPaddingPath.Dispose();
                LeftButtonIndicatorPath.Dispose();
                RightContainerPath.Dispose();
                RightPaddingPath.Dispose();
                RightButtonIndicatorPath.Dispose();
                TabButtonsContainerPath.Dispose();
                TabButtonsContainerNoWrapPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void TryToMoveToNextTab()
    {
        bool foundPressed = false;
        Button? firstTab = null;

        foreach (var potentialButton in tabButtons)
        {
            if (potentialButton is Button button)
            {
                if (!button.Visible || button.Disabled)
                    continue;

                firstTab ??= button;

                if (foundPressed)
                {
                    // Found an unpressed button
                    // We assume here that clicking the new button will correctly clear the old one as otherwise
                    // mouse control would be broken as well
                    PressButton(button);
                    return;
                }

                if (button.ButtonPressed)
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
            PressButton(firstTab);
        }
    }

    private void TryToMoveToPreviousTab()
    {
        Button? previousButton = null;

        foreach (var potentialButton in tabButtons)
        {
            if (potentialButton is Button button)
            {
                if (button.ButtonPressed)
                {
                    // When we find a pressed button, we want to move to press the previously seen button
                    if (previousButton != null)
                    {
                        PressButton(previousButton);
                        break;
                    }

                    // If we don't have a previous button, then we loop until the end and rely on the previous button
                    // variable
                }

                if (button.Visible && !button.Disabled)
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
            PressButton(previousButton);
        }
    }

    private void PressButton(Button button)
    {
        // None of the methods seem to work in all cases so we just allow the GUI creator to define which is the right
        // way
        switch (TabChangeTriggerMethod)
        {
            case PressType.SetPressedState:
                button.ButtonPressed = true;
                break;
            case PressType.PressedSignal:
                button.EmitSignal("pressed");

                // These don't seem to work in any current case but might just be necessary in the future
                // button.EmitSignal("button_down");
                // button.EmitSignal("button_up");

                // If the button doesn't move to pressed state, set it here. This makes some differently made tab
                // controlled buttons work (auto-evo exploring tool, for example)
                if (button.ButtonPressed != true)
                    button.ButtonPressed = true;

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <summary>
    ///   This control supports either dynamically adding the tabs through code or in the scene tree so this method is
    ///   needed to put the child buttons in the right place
    /// </summary>
    private void AdjustSceneAddedChildren()
    {
        foreach (var child in GetChildren().OfType<Control>())
        {
            // Move all children that aren't our own scene set children to the buttons container
            if (child.Equals(leftContainer) || child.Equals(rightContainer) || child.Equals(tabButtonsContainer) ||
                child.Equals(leftPadding) || child.Equals(rightPadding) || child.Equals(tabButtonsContainerNoWrap))
            {
                continue;
            }

            // Found a button to move
            tabButtons.Add(child);
            child.ReParent(NoWrap ? tabButtonsContainerNoWrap : tabButtonsContainer);

            // Make all of the added things visible, this is because line splitting doesn't work by default in the
            // editor so some scenes will want to hide the tab buttons in the editor
            child.Visible = true;

            // Auto disable the focus mode as that's not wanted for tab buttons, this makes things simpler in the
            // scenes that specify tab buttons
            if (child is Button button)
                button.FocusMode = FocusModeEnum.None;
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

            leftPadding.Visible = false;
            rightPadding.Visible = false;
        }
        else
        {
            leftContainer.Visible = false;
            rightContainer.Visible = false;

            leftPadding.Visible = MoveIndicatorsTakeUpSpaceWhileInvisible;
            rightPadding.Visible = MoveIndicatorsTakeUpSpaceWhileInvisible;
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
