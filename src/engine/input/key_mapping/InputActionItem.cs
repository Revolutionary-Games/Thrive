using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Godot;
using Newtonsoft.Json;

[JsonConverter(typeof(InputActionItemConverter))]
public class InputActionItem : VBoxContainer
{
    [Export]
    public NodePath AddInputEventPath;

    [Export]
    public NodePath InputActionHeaderPath;

    [Export]
    public NodePath InputEventsContainerPath;

    internal InputGroupItem AssociatedGroup;

    private Label inputActionHeader;
    private HBoxContainer inputEventsContainer;
    private Button addInputEvent;

    [JsonProperty]
    public string InputName { get; set; }

    [JsonProperty]
    public string DisplayName { get; set; }

    [JsonProperty]
    public ObservableCollection<InputEventItem> Inputs { get; set; }

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

    protected override void Dispose(bool disposing)
    {
        AssociatedGroup = null;
        foreach (var inputEventItem in Inputs)
        {
            inputEventItem.Dispose();
        }

        base.Dispose(disposing);
    }

    internal void OnAddEventButtonPressed()
    {
        var newInput = (InputEventItem)InputGroupList.InputEventItemScene.Instance();
        newInput.AssociatedAction = this;
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
