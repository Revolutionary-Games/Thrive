using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

[JsonConverter(typeof(InputGroupItemConverter))]
public class InputGroupItem : VBoxContainer
{
    [Export]
    public NodePath InputGroupHeaderPath;

    [Export]
    public NodePath InputActionsContainerPath;

    private Label inputGroupHeader;
    private VBoxContainer inputActionsContainer;

    [JsonProperty]
    public string GroupName { get; set; }

    [JsonProperty]
    public IList<InputActionItem> Actions { get; set; }

    [JsonProperty]
    public IList<string> EnvironmentId { get; set; }

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

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        foreach (var inputActionItem in Actions)
        {
            inputActionItem.Dispose();
        }
    }
}
