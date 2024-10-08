/// <summary>
///   These values have to match what is configured in "CellEditorComponent.tscn"
/// </summary>
public enum ResourceLimitingMode
{
    AllResources = 0,

    WithoutGlucose,
    WithoutIron,
    WithoutHydrogenSulfide,

    NoExternalResources,
}
