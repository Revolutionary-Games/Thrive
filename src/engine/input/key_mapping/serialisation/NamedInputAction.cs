using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

public class NamedInputAction
{
    public string InputName { get; set; }
    public string Name { get; set; }

    public InputActionItem ToGodotObject(IEnumerable<ThriveInputEventWithModifiers> data, InputGroupItem caller)
    {
        var result = (InputActionItem)InputGroupList.InputActionItemScene.Instance();

        result.InputName = InputName;
        result.DisplayName = Name;
        result.AssociatedGroup = caller;
        result.Inputs = new ObservableCollection<InputEventItem>(data.Select(p =>
        {
            var res = (InputEventItem)InputGroupList.InputEventItemScene.Instance();
            res.AssociatedAction = result;
            res.AssociatedEvent = p;
            return res;
        }));

        return result;
    }
}
