/// <summary>
///   The Symmetry setting of the hex based Editor.
/// </summary>
public enum HexEditorSymmetry
{
    /// <summary>
    ///   No symmetry in the editor.
    /// </summary>
    None,

    /// <summary>
    ///   Symmetry across the X-Axis in the editor.
    /// </summary>
    XAxisSymmetry,

    /// <summary>
    ///   Symmetry across both the X and the Y axis in the editor.
    /// </summary>
    FourWaySymmetry,

    /// <summary>
    ///   Symmetry across the X and Y axis, as well as across center, in the editor.
    /// </summary>
    SixWaySymmetry,
}
