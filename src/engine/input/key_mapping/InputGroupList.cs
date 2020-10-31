using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
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

    private ConfirmationDialog conflictDialog;

    private InputEventItem latestDialogCaller;
    private InputEventItem latestDialogConflict;
    private InputEventWithModifiers latestDialogNewEvent;

    private bool wasListeningForInput;
    private IEnumerable<InputGroupItem> allGroupItems;

    public InputGroupList()
    {
        Instance?.Dispose();
        Instance = this;
    }

    public delegate void ControlsChangedDelegate(InputDataList data);

    public event ControlsChangedDelegate OnControlsChanged;

    public static InputGroupList Instance { get; private set; }

    public InputDataList LoadingData { get; private set; }

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

    public static InputDataList GetDefaultControls()
    {
        return CloneControls(defaultControls);
    }

    public static InputDataList GetCurrentlyAppliedControls()
    {
        return new InputDataList(InputMap.GetActions().OfType<string>()
            .ToDictionary(p => p,
                p => InputMap.GetActionList(p).OfType<InputEventWithModifiers>()
                    .ToList()));
    }

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
        InputEventItemScene = GD.Load<PackedScene>("res://src/engine/input/key_mapping/InputEventItem.tscn");
        InputGroupItemScene = GD.Load<PackedScene>("res://src/engine/input/key_mapping/InputGroupItem.tscn");
        InputActionItemScene = GD.Load<PackedScene>("res://src/engine/input/key_mapping/InputActionItem.tscn");

        conflictDialog = GetNode<ConfirmationDialog>(ConflictDialogPath);
    }

    protected override void Dispose(bool disposing)
    {
        foreach (var inputGroupItem in AllGroupItems)
        {
            inputGroupItem.Dispose();
        }

        base.Dispose(disposing);
    }

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

    public void InitFromData(InputDataList data)
    {
        LoadingData = data;
        foreach (Node child in GetChildren())
        {
            RemoveChild(child);

            child.Free();
        }


        // Load json
        try
        {
            using var file = new File();
            file.Open(Constants.INPUT_OPTIONS, File.ModeFlags.Read);
            var fileContent = file.GetAsText();
            var inputGroups = JsonConvert.DeserializeObject<IList<InputGroupItem>>(fileContent);
            file.Close();

            foreach (var inputGroup in inputGroups)
            {
                AddChild(inputGroup);
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"Could not load the input settings: {e}");
            InitFromData(Settings.Instance.CurrentControls);
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

    internal static InputDataList CloneControls(InputDataList data)
    {
        var result = new Dictionary<string, List<InputEventWithModifiers>>();
        foreach (var keyValuePair in data.Data)
        {
            result[keyValuePair.Key] = new List<InputEventWithModifiers>();
            foreach (var inputEventWithModifiers in keyValuePair.Value)
            {
                InputEventWithModifiers newEvent = inputEventWithModifiers switch
                {
                    InputEventKey key => new InputEventKey { Scancode = key.Scancode, },
                    InputEventMouseButton mouse => new InputEventMouseButton { ButtonIndex = mouse.ButtonIndex, },
                    _ => throw new NotSupportedException($"InputType {inputEventWithModifiers.GetType()} not supported"),
                };

                newEvent.Alt = inputEventWithModifiers.Alt;
                newEvent.Control = inputEventWithModifiers.Control;
                newEvent.Shift = inputEventWithModifiers.Shift;

                result[keyValuePair.Key].Add(newEvent);
            }
        }

        return new InputDataList(result);
    }
}
