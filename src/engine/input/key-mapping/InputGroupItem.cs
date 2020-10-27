using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

public class InputGroupItem : VBoxContainer
{
    [Export]
    [JsonIgnore]
    public NodePath InputGroupHeaderPath;

    [Export]
    [JsonIgnore]
    public NodePath InputActionsContainerPath;

    private Label inputGroupHeader;
    private VBoxContainer inputActionsContainer;

    [JsonProperty]
    public string GroupName { get; set; }

    [JsonProperty]
    public IList<InputActionItem> Actions { get; set; }

    public override void _Ready()
    {
        inputGroupHeader = GetNode<Label>(InputGroupHeaderPath);
        inputActionsContainer = GetNode<VBoxContainer>(InputActionsContainerPath);

        inputGroupHeader.Text = GroupName;

        // Add the actions to the DOM
        foreach (var action in Actions)
        {
            inputActionsContainer.AddChild(action);
        }
    }
}
