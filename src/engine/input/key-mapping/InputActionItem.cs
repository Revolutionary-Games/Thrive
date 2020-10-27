using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Godot;
using Newtonsoft.Json;

public class InputActionItem : VBoxContainer
{
    [Export]
    public NodePath InputActionHeaderPath;

    [Export]
    public NodePath InputEventsContainerPath;

    private Label inputActionHeader;
    private VBoxContainer inputEventsContainer;

    [JsonProperty]
    public string InputName { get; set; }

    [JsonProperty]
    public string DisplayName { get; set; }

    [JsonIgnore]
    public ObservableCollection<InputEventItem> Inputs { get; internal set; }

    public override void _Ready()
    {
        inputActionHeader = GetNode<Label>(InputActionHeaderPath);
        inputEventsContainer = GetNode<VBoxContainer>(InputEventsContainerPath);

        inputActionHeader.Text = DisplayName;

        foreach (var input in Inputs)
        {
            inputEventsContainer.AddChild(input);
        }

        Inputs.CollectionChanged += OnInputsChanged;
    }

    private void OnInputsChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                foreach (InputEventItem newItem in e.NewItems)
                {
                    AddChild(newItem);
                }

                break;
            case NotifyCollectionChangedAction.Remove:
                foreach (InputEventItem oldItem in e.OldItems)
                {
                    RemoveChild(oldItem);
                }

                break;
            default:
                throw new NotSupportedException($"{e.Action} is not supported on {nameof(Inputs)}");
        }
    }
}
