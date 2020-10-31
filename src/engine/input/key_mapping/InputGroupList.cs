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

    private static IEnumerable<InputGroupItem> allGroupItems;
    private static InputDataList defaultControls;

    private ConfirmationDialog conflictDialog;

    private InputEventItem latestDialogCaller;
    private InputEventItem latestDialogConflict;
    private InputEventWithModifiers latestDialogNewEvent;

    private bool wasListeningForInput;
    private InputDataList loadingData;

    static InputGroupList()
    {
        InputEventItemScene = GD.Load<PackedScene>("res://src/engine/input/key_mapping/InputEventItem.tscn");
        InputGroupItemScene = GD.Load<PackedScene>("res://src/engine/input/key_mapping/InputGroupItem.tscn");
        InputActionItemScene = GD.Load<PackedScene>("res://src/engine/input/key_mapping/InputActionItem.tscn");

        defaultControls = GetCurrentlyAppliedControls();
    }

    public InputGroupList()
    {
        Instance?.Dispose();
        Instance = this;
    }

    public delegate void ControlsChangedDelegate(InputDataList data);

    /// <summary>
    ///   Fires whenever some inputs were redefined.
    /// </summary>
    public event ControlsChangedDelegate OnControlsChanged;

    public static InputGroupList Instance { get; private set; }

    /// <summary>
    ///   The currently loading data.
    ///   Used by <see cref="InputGroupItemConverter"></see>
    /// </summary>
    public InputDataList LoadingData
    {
        get => loadingData;
        private set
        {
            loadingData = value;

            // Load json
            try
            {
                using var file = new File();
                file.Open(Constants.INPUT_OPTIONS, File.ModeFlags.Read);
                var fileContent = file.GetAsText();
                allGroupItems = JsonConvert.DeserializeObject<IList<InputGroupItem>>(fileContent);
                file.Close();
            }
            catch (Exception e)
            {
                GD.PrintErr($"Could not load the input settings: {e}");
            }
        }
    }

    /// <summary>
    ///   If I was listening for inputs.
    ///   Used by the pause menu to not close whenever escape is pressed if the user was redefining keys
    /// </summary>
    public bool WasListeningForInput
    {
        get
        {
            var res = wasListeningForInput;
            wasListeningForInput = false;
            return res;
        }
        internal set => wasListeningForInput = value;
    }

    public IEnumerable<InputGroupItem> AllGroupItems => allGroupItems ??= GetChildren().OfType<InputGroupItem>();

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
                p => InputMap.GetActionList(p).OfType<InputEventWithModifiers>()
                    .ToList()));
    }

    /// <summary>
    ///   Returns not only the applied controls, but the controls the user is currently editing before pressing save.
    /// </summary>
    /// <returns>The not applied controls.</returns>
    public InputDataList GetCurrentlyPendingControls()
    {
        var groups = AllGroupItems.ToList();
        if (!groups.Any())
            return GetDefaultControls();
        return new InputDataList(groups.SelectMany(p => p.Actions)
            .ToDictionary(p => p.InputName, p => p.Inputs.Select(x => x.AssociatedEvent).ToList()));
    }

    public override void _Ready()
    {
        conflictDialog = GetNode<ConfirmationDialog>(ConflictDialogPath);
    }

    /// <summary>
    ///   Sets up and displays the "There is a conflict" dialog.
    /// </summary>
    /// <param name="caller">The event which wants to be redefined</param>
    /// <param name="conflict">The event which produced the conflict</param>
    /// <param name="newEvent">The new event wanted to be set to the caller</param>
    public void ShowInputConflictDialog(InputEventItem caller, InputEventItem conflict, InputEventWithModifiers newEvent)
    {
        latestDialogCaller = caller;
        latestDialogConflict = conflict;
        latestDialogNewEvent = newEvent;

        conflictDialog.DialogText = $"There is a conflict with {conflict.AssociatedAction.DisplayName}.\n" +
                                    $"Do you want to remove the input from {conflict.AssociatedAction.DisplayName}?";
        conflictDialog.PopupCenteredMinsize();
    }

    /// <summary>
    ///   Get the input conflict if there are any
    /// </summary>
    /// <param name="item">The event with the new value</param>
    /// <returns>The collision if any</returns>
    public InputEventItem Conflicts(InputEventItem item)
    {
        // Get all environments the item is associated with.
        var environments = item.AssociatedAction.AssociatedGroup.EnvironmentId;

        // Take all InputGroups.
        // Take the ones with any interception of the environments.
        // Take the input actions.
        // Get the first action where the event collides or null if there aren't any.
        return AllGroupItems.Where(p => p.EnvironmentId.Any(x => environments.Contains(x)))
            .SelectMany(p => p.Actions)
            .Where(p => !Equals(item.AssociatedAction, p))
            .SelectMany(p => p.Inputs)
            .FirstOrDefault(p => Equals(p, item));
    }

    /// <summary>
    ///   Loads the input_options.json file and sets up the tree.
    /// </summary>
    /// <param name="data">The input data the input tab should be loaded with</param>
    public void InitFromData(InputDataList data)
    {
        LoadingData = data;
        foreach (Node child in GetChildren())
        {
            RemoveChild(child);

            child.Free();
        }

        foreach (var inputGroup in AllGroupItems)
        {
            AddChild(inputGroup);
        }
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

    internal void ControlsChanged()
    {
        OnControlsChanged?.Invoke(GetCurrentlyPendingControls());
    }

    protected override void Dispose(bool disposing)
    {
        foreach (var inputGroupItem in AllGroupItems)
        {
            inputGroupItem.Dispose();
        }

        base.Dispose(disposing);
    }
}
