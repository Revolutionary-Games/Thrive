using System.Collections.Generic;

public class NamedInputGroup
{
    public string GroupName { get; set; }
    public IReadOnlyList<string> EnvironmentId { get; set; }
    public IReadOnlyList<NamedInputAction> Actions { get; set; }
}
