using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Godot;

/// <summary>
///   Shows keys associated with one Godot input action, for example g_move_forward.
///   Each InputActionItem has <see cref="InputEventItem">InputEventItems</see> associated with it.
/// </summary>
/// <remarks>
///   <para>
///     Used by OptionsMenu>Inputs>InputGroupContainer>InputGroupItem>InputActionItem.
///   </para>
///   <para>
///     Does not contain changing inputs logic, but contains InputEventItems, which do.
///   </para>
///   <para>
///     Handles the + button for adding new bindings.
///   </para>
/// </remarks>
public class InputActionItem : VBoxContainer
{
    [Export]
    public NodePath AddInputEventPath = null!;

    [Export]
    public NodePath InputActionHeaderPath = null!;

    [Export]
    public NodePath InputEventsContainerPath = null!;

    private Label inputActionHeader = null!;
    private HBoxContainer inputEventsContainer = null!;
    private Button addInputEvent = null!;

    /// <summary>
    ///   The group in which this action is defined.
    /// </summary>
    public WeakReference<InputGroupItem>? AssociatedGroup { get; set; }

    /// <summary>
    ///   The godot specific action name
    /// </summary>
    /// <example>
    ///   g_move_left, g_zoom_in
    /// </example>
    public string InputName { get; set; } = string.Empty;

    /// <summary>
    ///   The string presented to the user
    /// </summary>
    /// <example>
    ///   Move left, Zoom in
    /// </example>
    public string? DisplayName { get; set; }

    /// <summary>
    ///   All the associated inputs executing this action. Must be initialized before using this class
    /// </summary>
    public ObservableCollection<InputEventItem> Inputs { get; private set; } = null!;

    public InputGroupItem? Group
    {
        get
        {
            if (AssociatedGroup == null)
                return null;

            AssociatedGroup.TryGetTarget(out var associatedGroup);
            return associatedGroup;
        }
    }

    public InputGroupList? GroupList
    {
        get
        {
            var group = Group;

            if (group == null)
                return null;

            group.AssociatedList.TryGetTarget(out var associatedList);

            return associatedList;
        }
    }

    /// <summary>
    ///   Sets up the inputs and adds them as children.
    /// </summary>
    public override void _Ready()
    {
        if (string.IsNullOrEmpty(InputName))
            throw new InvalidOperationException($"{nameof(InputName)} can't be empty");

        if (DisplayName == null)
            throw new InvalidOperationException($"{nameof(DisplayName)} can't be null");

        if (Inputs == null)
            throw new InvalidOperationException($"{nameof(Inputs)} can't be null");

        inputActionHeader = GetNode<Label>(InputActionHeaderPath);
        inputEventsContainer = GetNode<HBoxContainer>(InputEventsContainerPath);
        addInputEvent = GetNode<Button>(AddInputEventPath);

        addInputEvent.RegisterToolTipForControl("addInputButton", "options");

        inputActionHeader.Text = DisplayName;

        foreach (var input in Inputs)
        {
            input.AssociatedAction = new WeakReference<InputActionItem>(this);

            inputEventsContainer.AddChild(input);
        }

        inputEventsContainer.MoveChild(addInputEvent, Inputs.Count);

        SetFocusNeighbors();

        Inputs.CollectionChanged += OnInputsChanged;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (obj.GetType() != GetType())
            return false;

        return string.Equals(((InputActionItem)obj).InputName, InputName, StringComparison.InvariantCulture);
    }

    public override int GetHashCode()
    {
        return InputName.GetHashCode();
    }

    internal static InputActionItem BuildGUI(InputGroupItem associatedGroup, NamedInputAction data,
        IEnumerable<SpecifiedInputKey> inputs)
    {
        associatedGroup.AssociatedList.TryGetTarget(out var target);

        if (target == null)
            throw new ArgumentException("associatedGroup has no associated list");

        var inputActionItem = (InputActionItem)target.InputActionItemScene.Instance();

        inputActionItem.InputName = data.InputName;
        inputActionItem.DisplayName = data.Name;
        inputActionItem.AssociatedGroup = new WeakReference<InputGroupItem>(associatedGroup);
        inputActionItem.Inputs =
            new ObservableCollection<InputEventItem>(inputs.Select(d => InputEventItem.BuildGUI(inputActionItem, d)));

        return inputActionItem;
    }

    private void SetFocusNeighbors()
    {
        var count = Inputs.Count;

        for (var index = 0; index < count; index++)
        {
            var input = Inputs[index];
            if (index > 0)
                input.SetLeftNeighbor(Inputs[index - 1]);
            else
                input.SetLeftNeighbor(addInputEvent);

            if (index < count - 1)
                input.SetRightNeighbor(Inputs[index + 1]);
            else
                input.SetRightNeighbor(addInputEvent);
        }

        if (count > 0)
        {
            addInputEvent.FocusNeighbourLeft = Inputs[count - 1].GetRightAnchorPath();
            addInputEvent.FocusNeighbourRight = Inputs[0].GetLeftAnchorPath();
        }
        else
        {
            addInputEvent.FocusNeighbourLeft = AddInputEventPath;
            addInputEvent.FocusNeighbourRight = AddInputEventPath;
        }
    }

    /// <summary>
    ///   The small + button has been pressed
    /// </summary>
    private void OnAddEventButtonPressed()
    {
        var newInput = (InputEventItem)GroupList!.InputEventItemScene.Instance();
        newInput.AssociatedAction = new WeakReference<InputActionItem>(this);
        newInput.JustAdded = true;
        Inputs.Add(newInput);
    }

    private void OnInputsChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                foreach (InputEventItem newItem in e.NewItems)
                {
                    inputEventsContainer.AddChild(newItem);
                }

                inputEventsContainer.MoveChild(addInputEvent, Inputs.Count);

                break;
            case NotifyCollectionChangedAction.Remove:
                foreach (InputEventItem oldItem in e.OldItems)
                {
                    inputEventsContainer.RemoveChild(oldItem);
                }

                break;
            default:
                throw new NotSupportedException($"{e.Action} is not supported on {nameof(Inputs)}");
        }

        SetFocusNeighbors();
    }
}
