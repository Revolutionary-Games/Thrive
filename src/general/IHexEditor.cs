/// <summary>
///   Interface extracted to make GUI generic parameters work
/// </summary>
public interface IHexEditor : IEditor
{
    public HexEditorSymmetry Symmetry { get; set; }

    /// <summary>
    ///   True when there are hexes that are not connected to the rest
    /// </summary>
    public bool HasIslands { get; }
}
