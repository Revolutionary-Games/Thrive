using Godot;

/// <summary>
///   This is the last autoloaded class to perform some actions there
/// </summary>
public class PostStartupActions : Node
{
    private PostStartupActions()
    {
        // Queue window title set as setting it in the autoloads doesn't work yet
        Invoke.Instance.Perform(() => { OS.SetWindowTitle("Thrive - " + Constants.Version); });
    }
}
