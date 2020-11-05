using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Holds the various <see cref="InputGroupItem">input groups</see> in one VBoxContainer.
///   Used by OptionsMenu>Inputs>InputGroupContainer
/// </summary>
public class InputGroupList : VBoxContainer
{
    [Export]
    public NodePath ConflictDialogPath;

    internal static PackedScene InputEventItemScene;
    internal static PackedScene InputGroupItemScene;
    internal static PackedScene InputActionItemScene;

    private static InputDataList defaultControls = GetCurrentlyAppliedControls();
    private static bool wasListeningForInput;

    private IEnumerable<InputGroupItem> activeInputGroupList;

    private InputEventItem latestDialogCaller;
    private InputEventItem latestDialogConflict;
    private InputEventWithModifiers latestDialogNewEvent;

    private ConfirmationDialog conflictDialog;

    public delegate void ControlsChangedDelegate(InputDataList data);

    /// <summary>
    ///   Fires whenever some inputs were redefined.
    /// </summary>
    public event ControlsChangedDelegate OnControlsChanged;

    /// <summary>
    ///   If I was listening for inputs.
    ///   Used by the pause menu to not close whenever escape is pressed if the user was redefining keys
    /// </summary>
    public static bool WasListeningForInput
    {
        get
        {
            var res = wasListeningForInput;
            wasListeningForInput = false;
            return res;
        }
        internal set => wasListeningForInput = value;
    }

    /// <summary>
    ///   Is any Input currently waiting for input
    /// </summary>
    public bool ListeningForInput => ActiveInputGroupList
        .Any(x => x.Actions
            .Any(y => y.Inputs
                .Any(z => z.WaitingForInput)));

    public IEnumerable<InputGroupItem> ActiveInputGroupList => activeInputGroupList;

    /// <summary>
    ///   Returns the default controls which never change, unless there is a new release.
    /// </summary>
    /// <returns>The default controls</returns>
    public static InputDataList GetDefaultControls()
    {
        return (InputDataList)defaultControls.Clone();
    }

    /// <summary>
    ///   Returns the currently applied controls. Gathers the data from the godot InputMap.
    ///   Required to get the default controls.
    /// </summary>
    /// <returns>The current inputs</returns>
    public static InputDataList GetCurrentlyAppliedControls()
    {
        return new InputDataList(InputMap.GetActions().OfType<string>()
            .ToDictionary(p => p,
                p => InputMap.GetActionList(p).OfType<InputEventWithModifiers>().Select(
                    x => new SpecifiedInputKey(x)).ToList()));
    }

    /// <summary>
    ///   Returns not only the applied controls, but the controls the user is currently editing before pressing save.
    /// </summary>
    /// <returns>The not applied controls.</returns>
    public InputDataList GetCurrentlyPendingControls()
    {
        var groups = ActiveInputGroupList.ToList();
        if (!groups.Any())
            return GetDefaultControls();

        return new InputDataList(groups.SelectMany(p => p.Actions)
            .ToDictionary(p => p.InputName, p => p.Inputs.Select(x => x.AssociatedEvent).ToList()));
    }

    /// <summary>
    ///   Get the input conflict if there are any
    /// </summary>
    /// <param name="item">The event with the new value</param>
    /// <returns>The collision if any</returns>
    public InputEventItem Conflicts(InputEventItem item)
    {
        if (!item.AssociatedAction.TryGetTarget(out var inputActionItem))
            return default;

        if (!inputActionItem.AssociatedGroup.TryGetTarget(out var inputGroupItem))
            return default;

        // Get all environments the item is associated with.
        var environments = inputGroupItem.EnvironmentId;

        // Take all InputGroups.
        // Take the ones with any interception of the environments.
        // Take the input actions.
        // Get the first action where the event collides or null if there aren't any.
        return ActiveInputGroupList.Where(p => p.EnvironmentId.Any(x => environments.Contains(x)))
            .SelectMany(p => p.Actions)
            .Where(p => !Equals(inputActionItem, p))
            .SelectMany(p => p.Inputs)
            .FirstOrDefault(p => Equals(p, item));
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
        if (!conflict.AssociatedAction.TryGetTarget(out var inputActionItem))
            return;

        latestDialogCaller = caller;
        latestDialogConflict = conflict;
        latestDialogNewEvent = newEvent;

        conflictDialog.DialogText = $"There is a conflict with {inputActionItem.DisplayName}.\n" +
            $"Do you want to remove the input from {inputActionItem.DisplayName}?";
        conflictDialog.PopupCenteredMinsize();
    }

    public void OnConflictConfirmed()
    {
        latestDialogConflict.Delete();
        latestDialogCaller._Input(latestDialogNewEvent);
    }

    public bool IsConflictDialogOpen()
    {
        return conflictDialog.Visible;
    }

    /// <summary>
    ///   Loads the input_options and saves it to with the data in the AllGroupItems
    /// </summary>
    public void LoadFromData(InputDataList data)
    {
        // Load json
        try
        {
            using var file = new File();
            file.Open(Constants.INPUT_OPTIONS, File.ModeFlags.Read);
            var fileContent = file.GetAsText();
            var loadedJson = JsonConvert.DeserializeObject<List<NamedInputGroup>>(fileContent);
            foreach (var inputGroupItem in activeInputGroupList ?? Array.Empty<InputGroupItem>())
                inputGroupItem.Dispose();

            activeInputGroupList = BuildGUI(loadedJson, data);
            file.Close();
        }
        catch (Exception e)
        {
            GD.PrintErr($"Could not load the input settings: {e}");
        }
    }

    public override void _Ready()
    {
        InputEventItemScene = GD.Load<PackedScene>("res://src/engine/input/key_mapping/InputEventItem.tscn");
        InputGroupItemScene = GD.Load<PackedScene>("res://src/engine/input/key_mapping/InputGroupItem.tscn");
        InputActionItemScene = GD.Load<PackedScene>("res://src/engine/input/key_mapping/InputActionItem.tscn");

        conflictDialog = GetNode<ConfirmationDialog>(ConflictDialogPath);
    }

    /// <summary>
    ///   Loads the input_options.json file and sets up the tree.
    /// </summary>
    /// <param name="data">The input data the input tab should be loaded with</param>
    public void InitFromData(InputDataList data)
    {
        LoadFromData(data);
    }

    internal void ControlsChanged()
    {
        OnControlsChanged?.Invoke(GetCurrentlyPendingControls());
    }

    private IEnumerable<InputGroupItem> BuildGUI(IEnumerable<NamedInputGroup> loadedJson, InputDataList data)
    {
        return loadedJson.Select(p => InputGroupItem.BuildGUI(this, p, data)).ToList();
    }
}
