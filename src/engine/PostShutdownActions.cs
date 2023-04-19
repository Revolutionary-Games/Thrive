using Godot;

/// <summary>
///   Allows running some last bits of code before the game process exits
/// </summary>
/// <remarks>
///   <para>
///     This is implemented by having this node last in the scene tree, which ensures that this is the last node to
///     be removed from the scene tree. That is handled by <see cref="SceneManager"/>.
///   </para>
/// </remarks>
public class PostShutdownActions : Node
{
    public override void _ExitTree()
    {
        base._ExitTree();

        OnGameSceneTreeReleased();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            OnAfterGameShutdown();
        }

        base.Dispose(disposing);
    }

    private void OnGameSceneTreeReleased()
    {
    }

    /// <summary>
    ///   Called after game shutdown to allow some checks to run then. Note that the dispose calls order doesn't seem
    ///   to follow the scene tree order so this is not exact
    /// </summary>
    private void OnAfterGameShutdown()
    {
        if (ToolTipHelper.CountRegisteredToolTips() > 0)
        {
            GD.PrintErr("Some tooltips have not been unregistered on game shutdown");
        }
    }
}
