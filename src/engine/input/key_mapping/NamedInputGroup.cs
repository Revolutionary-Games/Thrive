using System.Collections.Generic;

/// <summary>
///   A group of controls. Controls are shown by group in the key rebind menu
/// </summary>
public class NamedInputGroup
{
    public string GroupName { get; set; }
    public IReadOnlyList<string> EnvironmentId { get; set; }
    public IReadOnlyList<NamedInputAction> Actions { get; set; }
}
