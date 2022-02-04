﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
///   Holds the various <see cref="InputGroupItem">input groups</see> in one VBoxContainer.
///   Used by OptionsMenu>Inputs>InputGroupContainer
/// </summary>
public class InputGroupList : VBoxContainer
{
    [Export]
    public NodePath ConflictDialogPath = null!;

    [Export]
    public NodePath ResetInputsDialog = null!;

    private IEnumerable<InputGroupItem>? activeInputGroupList;

    private InputEventItem? latestDialogCaller;
    private InputEventItem? latestDialogConflict;
    private InputEventWithModifiers? latestDialogNewEvent;

    private CustomConfirmationDialog conflictDialog = null!;
    private CustomConfirmationDialog resetInputsDialog = null!;

    public delegate void ControlsChangedDelegate(InputDataList data);

    /// <summary>
    ///   Fired whenever some inputs were redefined.
    /// </summary>
    public event ControlsChangedDelegate? OnControlsChanged;

    public PackedScene InputEventItemScene { get; private set; } = null!;
    public PackedScene InputGroupItemScene { get; private set; } = null!;
    public PackedScene InputActionItemScene { get; private set; } = null!;

    /// <summary>
    ///   Is any Input currently waiting for input
    /// </summary>
    public bool ListeningForInput => ActiveInputGroupList
        .Any(group => group.Actions
            .Any(action => action.Inputs.Any(singularInput => singularInput.WaitingForInput)));

    public IEnumerable<InputGroupItem> ActiveInputGroupList => activeInputGroupList ?? Array.Empty<InputGroupItem>();

    public override void _Ready()
    {
        InputEventItemScene = GD.Load<PackedScene>("res://src/engine/input/key_mapping/InputEventItem.tscn");
        InputGroupItemScene = GD.Load<PackedScene>("res://src/engine/input/key_mapping/InputGroupItem.tscn");
        InputActionItemScene = GD.Load<PackedScene>("res://src/engine/input/key_mapping/InputActionItem.tscn");

        conflictDialog = GetNode<CustomConfirmationDialog>(ConflictDialogPath);
        resetInputsDialog = GetNode<CustomConfirmationDialog>(ResetInputsDialog);
    }

    /// <summary>
    ///   Returns not only the applied controls, but the controls the user is currently editing before pressing save.
    /// </summary>
    /// <returns>
    ///   The not applied controls, contains the full set of controls, also ones that haven't been changed.
    /// </returns>
    public InputDataList GetCurrentlyPendingControls()
    {
        if (!ActiveInputGroupList.Any())
            return Settings.GetDefaultControls();

        return new InputDataList(ActiveInputGroupList.SelectMany(p => p.Actions)
            .ToDictionary(p => p.InputName, p => p.Inputs.Select(x => x.AssociatedEvent).ToList()));
    }

    /// <summary>
    ///   Get the input conflicts if there are any
    /// </summary>
    /// <param name="item">The event with the new value to set</param>
    /// <returns>The collisions if any of item against already set controls</returns>
    public InputEventItem? Conflicts(InputEventItem item)
    {
        // This needs to be done this way as it seems older compiler version can't understand the code otherwise
        // and issues warnings
        // ReSharper disable InlineOutVariableDeclaration RedundantAssignment
        InputActionItem? inputActionItem = null;
        if (item.AssociatedAction?.TryGetTarget(out inputActionItem) != true || inputActionItem == null)
            return default;

        InputGroupItem? inputGroupItem = null;
        if (inputActionItem.AssociatedGroup?.TryGetTarget(out inputGroupItem) != true || inputGroupItem == null)
            return default;

        // ReSharper restore InlineOutVariableDeclaration RedundantAssignment

        // Get all environments the item is associated with.
        var environments = inputGroupItem.EnvironmentId;

        // Take all InputGroups.
        // Take the ones with any interception of the environments.
        // Take the input actions.
        // Get the first action where the event collides or null if there aren't any.
        return ActiveInputGroupList.Where(group => group.EnvironmentId.Any(x => environments.Contains(x)))
            .SelectMany(group => group.Actions)
            .Where(action => !Equals(inputActionItem, action))
            .SelectMany(action => action.Inputs)
            .FirstOrDefault(input => Equals(input, item));
    }

    /// <summary>
    ///   Sets up and displays the "There is a conflict" dialog.
    /// </summary>
    /// <param name="caller">The event which wants to be redefined</param>
    /// <param name="conflict">The event which produced the conflict</param>
    /// <param name="newEvent">The new event wanted to be set to the caller</param>
    public void ShowInputConflictDialog(InputEventItem caller, InputEventItem conflict,
        InputEventWithModifiers newEvent)
    {
        // See the comments in Conflicts as to why this is done like this
        // ReSharper disable InlineOutVariableDeclaration RedundantAssignment
        InputActionItem? inputActionItem = null;
        if (conflict.AssociatedAction?.TryGetTarget(out inputActionItem) != true || inputActionItem == null)
            return;

        // ReSharper restore InlineOutVariableDeclaration RedundantAssignment

        latestDialogCaller = caller;
        latestDialogConflict = conflict;
        latestDialogNewEvent = newEvent;

        conflictDialog.DialogText = string.Format(CultureInfo.CurrentCulture,
            TranslationServer.Translate("KEY_BINDING_CHANGE_CONFLICT"),
            inputActionItem.DisplayName, inputActionItem.DisplayName);

        conflictDialog.PopupCenteredShrink();
    }

    public void OnResetInputs()
    {
        resetInputsDialog.PopupCenteredShrink();
    }

    public void OnConflictConfirmed()
    {
        if (latestDialogConflict == null || latestDialogCaller == null || latestDialogNewEvent == null)
        {
            GD.PrintErr("Key binding conflict was resolved but no active dialogs exist");
            return;
        }

        latestDialogConflict.Delete();

        // Pass the input event again to have the key be set where it was previously skipped
        latestDialogCaller._Input(latestDialogNewEvent);
    }

    public bool IsConflictDialogOpen()
    {
        return conflictDialog.Visible;
    }

    /// <summary>
    ///   Processes the input data and saves the created GUI Controls in AllGroupItems
    /// </summary>
    /// <param name="data">The input data the input tab should be loaded with</param>
    public void LoadFromData(InputDataList data)
    {
        if (activeInputGroupList != null)
        {
            foreach (var inputGroupItem in activeInputGroupList)
                inputGroupItem.DetachAndFree();
        }

        activeInputGroupList = BuildGUI(SimulationParameters.Instance.InputGroups, data);
    }

    public void InitGroupList()
    {
        this.QueueFreeChildren();

        LoadFromData(Settings.Instance.CurrentControls);

        foreach (var inputGroup in ActiveInputGroupList)
        {
            AddChild(inputGroup);
        }
    }

    internal void ControlsChanged()
    {
        OnControlsChanged?.Invoke(GetCurrentlyPendingControls());
    }

    private IEnumerable<InputGroupItem> BuildGUI(IEnumerable<NamedInputGroup> groupData, InputDataList data)
    {
        return groupData.Select(p => InputGroupItem.BuildGUI(this, p, data)).ToList();
    }
}
