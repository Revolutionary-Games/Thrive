using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   An input group shown in the input tab in the options.
///   Has multiple <see cref="InputActionItem">InputActions</see>.
/// </summary>
[JsonConverter(typeof(InputGroupItemConverter))]
public class InputGroupItem : VBoxContainer
{
    [Export]
    public NodePath InputGroupHeaderPath;

    [Export]
    public NodePath InputActionsContainerPath;

    private Label inputGroupHeader;
    private VBoxContainer inputActionsContainer;

    /// <summary>
    ///   The display name for the group
    /// </summary>
    /// <example>
    ///   Movement
    /// </example>
    [JsonProperty]
    public string GroupName { get; set; }

    /// <summary>
    ///   The associated actions the group contains
    /// </summary>
    [JsonProperty]
    public IList<InputActionItem> Actions { get; set; }

    /// <summary>
    ///   A list of environments the actions of the group are associated with.
    ///   Used by the conflict detection.
    /// </summary>
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
