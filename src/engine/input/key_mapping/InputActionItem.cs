using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Defines one specific input.
///   Each InputActionItem has <see cref="InputEventItem">InputEventItems</see> associated with it.
/// </summary>
[JsonConverter(typeof(InputActionItemConverter))]
public class InputActionItem : VBoxContainer
{
    [Export]
    public NodePath AddInputEventPath;

    [Export]
    public NodePath InputActionHeaderPath;

    [Export]
    public NodePath InputEventsContainerPath;

    /// <summary>
    ///   The group in which this action is defined.
    /// </summary>
    internal InputGroupItem AssociatedGroup;

    private Label inputActionHeader;
    private HBoxContainer inputEventsContainer;
    private Button addInputEvent;

    /// <summary>
    ///   The godot specific action name
    /// </summary>
    /// <example>
    ///   g_move_left, g_zoom_in
    /// </example>
    [JsonProperty]
    public string InputName { get; set; }

    /// <summary>
    ///   The string presented to the user
    /// </summary>
    /// <example>
    ///   Move left, Zoom in
    /// </example>
    [JsonProperty]
    public string DisplayName { get; set; }

    /// <summary>
    ///   All the associated inputs executing this action
    /// </summary>
    [JsonProperty]
    public ObservableCollection<InputEventItem> Inputs { get; set; }

    /// <summary>
    ///   Sets up the inputs and adds them as children.
    /// </summary>
    public override void _Ready()
    {
        inputActionHeader = GetNode<Label>(InputActionHeaderPath);
        inputEventsContainer = GetNode<HBoxContainer>(InputEventsContainerPath);
        addInputEvent = GetNode<Button>(AddInputEventPath);

        inputActionHeader.Text = DisplayName;

        foreach (var input in Inputs)
        {
            input.AssociatedAction = this;
            inputEventsContainer.AddChild(input);
        }

        inputEventsContainer.MoveChild(addInputEvent, Inputs.Count);

        Inputs.CollectionChanged += OnInputsChanged;
    }

    public override bool Equals(object obj)
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
        return InputName != null ? InputName.GetHashCode() : 0;
    }

    /// <summary>
    ///   The small + button has been pressed
    /// </summary>
    internal void OnAddEventButtonPressed()
    {
        var newInput = (InputEventItem)InputGroupList.InputEventItemScene.Instance();
        newInput.AssociatedAction = this;
        newInput.JustAdded = true;
        Inputs.Add(newInput);
    }

    protected override void Dispose(bool disposing)
    {
        AssociatedGroup = null;
        foreach (var inputEventItem in Inputs)
        {
            inputEventItem.Dispose();
        }

        base.Dispose(disposing);
    }

    private void OnInputsChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                foreach (InputEventItem newItem in e.NewItems)
                {
                    inputEventsContainer.AddChild(newItem);
                    inputEventsContainer.MoveChild(addInputEvent, Inputs.Count);
                }

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
    }
}
