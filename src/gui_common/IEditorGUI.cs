/// <summary>
///   Interface extracted to make generic editor parameters not depend on each other in a loop
/// </summary>
public interface IEditorGUI
{
    public bool Visible { get; set; }
}
