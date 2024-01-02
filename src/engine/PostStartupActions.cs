using System.Globalization;
using Godot;

/// <summary>
///   This is the last autoloaded class to perform some actions there
/// </summary>
public class PostStartupActions : Node
{
    private PostStartupActions()
    {
        if (Engine.EditorHint)
        {
            // Skip these actions when running in the Godot editor
            return;
        }

        // Queue window title set as setting it in the autoloads doesn't work yet
        Invoke.Instance.Perform(() => { OS.SetWindowTitle("Thrive - " + Constants.Version); });
    }

    public override void _Ready()
    {
        // TODO: do we need to figure out a way to print this info earlier in the game logs?
        // Simulation parameters are now loaded so we can print the build info
        var info = SimulationParameters.Instance.GetBuildInfoIfExists();

        if (info == null)
        {
            GD.Print("No build info file exists, can't tell exact commit");
        }
        else
        {
#if DEBUG
            GD.Print("This is a debug version of the game (not exported) build info may not be updated so take " +
                "the following with a huge bag of salt:");
#endif
            if (info.DevBuild)
                GD.Print("This is a DEVBUILD, early release version of Thrive!");

            var time = info.BuiltAt.ToString("F", CultureInfo.InvariantCulture);
            GD.Print("This version of Thrive was built at ", time, " from commit ", info.Commit, " on branch ",
                info.Branch);
        }
    }
}
