using System.Collections.Generic;
using System.Linq;

public class NamedInputGroup
{
    public string GroupName { get; set; }
    public IReadOnlyList<string> EnvironmentId { get; set; }
    public IReadOnlyList<NamedInputAction> Actions { get; set; }

    public InputGroupItem ToGodotObject(InputDataList data)
    {
        var result = (InputGroupItem)InputGroupList.InputGroupItemScene.Instance();

        result.EnvironmentId = EnvironmentId.ToList();
        result.GroupName = GroupName;
        result.Actions = Actions.Select(p => p.ToGodotObject(data[p.InputName], result)).ToList();

        return result;
    }
}
