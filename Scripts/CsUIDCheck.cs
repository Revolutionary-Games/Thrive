namespace Scripts;

using System.Collections.Generic;
using System.IO;
using ScriptsBase.Checks.FileTypes;

/// <summary>
///   Checks that Godot has been opened since any C# files have been added.
/// </summary>
public class CsUIDCheck : FileCheck
{
    public const string UID_EXTENSION = ".uid";

    public CsUIDCheck() : base(".cs")
    {
    }

    public override async IAsyncEnumerable<string> Handle(string path)
    {
        var uidPath = path + UID_EXTENSION;

        if (!File.Exists(uidPath))
        {
            yield return $"Matching UID file ({uidPath}) does not exist. " +
                "Please open Godot to generate missing uid files.";

            yield break;
        }

        var fileInfo = new FileInfo(uidPath);

        if (fileInfo.Length <= 0)
        {
            yield return $"Matching UID file ({uidPath}) is empty. " +
                "Please open Godot to generate missing uid files.";
        }
    }
}
