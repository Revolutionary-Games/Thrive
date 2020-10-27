using System.Collections.ObjectModel;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Used by OptionsMenu>Inputs>InputGroupContainer
/// </summary>
public class InputGroupList : VBoxContainer
{
    public override void _Ready()
    {
        AddChild(new Label { Text = "Hello there" }); // Not even this gets shown

        var inputGroupItemScene = GD.Load<PackedScene>("res://src/engine/input/key-mapping/InputGroupItem.tscn");
        var inputActionItemScene = GD.Load<PackedScene>("res://src/engine/input/key-mapping/InputActionItem.tscn");
        var inputEventItemScene = GD.Load<PackedScene>("res://src/engine/input/key-mapping/InputEventItem.tscn");

        // Load json
        var definition =
            new[]
            {
                new
                {
                    GroupName = string.Empty,
                    Actions = new[]
                    {
                        new
                        {
                            InputName = string.Empty,
                            DisplayName = string.Empty,
                        },
                    },
                },
            };

        using var file = new File();
        file.Open("res://simulation_parameters/common/input_options.json", File.ModeFlags.Read);
        var fileContent = file.GetAsText();
        var jsonResult = JsonConvert.DeserializeAnonymousType(fileContent, definition);
        file.Close();

        // Add inputGroups to the DOM
        foreach (var groupItem in jsonResult)
        {
            // Create InputGroupItem
            var input = (InputGroupItem)inputGroupItemScene.Instance();
            input.Actions = groupItem.Actions.Select(p =>
            {
                // Create InputActionItem
                var inputAction = (InputActionItem)inputActionItemScene.Instance();
                inputAction.InputName = p.InputName;
                inputAction.DisplayName = p.DisplayName;
                inputAction.Inputs = new ObservableCollection<InputEventItem>(
                    InputMap.GetActionList(p.InputName)
                        .OfType<InputEventWithModifiers>()
                        .Select(x =>
                        {
                            var inputEvent = (InputEventItem)inputEventItemScene.Instance();
                            inputEvent.AssociatedEvent = x;
                            return inputEvent;
                        }));
                return inputAction;
            }).ToList();
            input.GroupName = groupItem.GroupName;

            // Add the InputGroupItem
            AddChild(input);
        }
    }
}
