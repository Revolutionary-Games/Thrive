/// <summary>
///   Interface extracted to make GUI generic parameters work
/// </summary>
public interface IHexEditorGUI : IEditorGUI
{
    void ResetSymmetryButton();
    void SetSymmetry(HexEditorSymmetry newSymmetry);
}
