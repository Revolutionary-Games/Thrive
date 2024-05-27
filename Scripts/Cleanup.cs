namespace Scripts;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ScriptsBase.Checks;
using ScriptsBase.Utilities;
using SharedBase.Utilities;

/// <summary>
///   Performs a full cleanup on the local version. Godot Editor needs to be closed while running.
/// </summary>
public static class Cleanup
{
    private static readonly IEnumerable<string> FoldersToDelete = new List<string>
    {
        ".godot",
        ".mono",
        JetBrainsCheck.JET_BRAINS_CACHE,
    };

    public static async Task<bool> Run(Program.CleanupOptions options, CancellationToken cancellationToken)
    {
        foreach (var folder in FoldersToDelete)
        {
            if (!Directory.Exists(folder))
                continue;

            ColourConsole.WriteNormalLine($"Deleting {folder}");

            try
            {
                Directory.Delete(folder, true);
            }
            catch (Exception e)
            {
                ColourConsole.WriteError($"Failed to delete folder: {e}");
            }

            if (cancellationToken.IsCancellationRequested)
                return false;
        }

        if (cancellationToken.IsCancellationRequested)
            return false;

        if (options.GitReset == true)
        {
            ColourConsole.WriteInfoLine("Doing git reset --hard HEAD");

            await GitRunHelpers.Reset("./", "HEAD", true, cancellationToken);
        }

        ColourConsole.WriteSuccessLine("Cleanup succeeded");

        return true;
    }
}
