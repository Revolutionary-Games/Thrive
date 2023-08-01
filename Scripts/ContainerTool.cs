namespace Scripts;

using System.Collections.Generic;
using System.Threading.Tasks;
using ScriptsBase.ToolBases;

public class ContainerTool : ContainerToolBase<Program.ContainerOptions>
{
    public ContainerTool(Program.ContainerOptions options) : base(options)
    {
    }

    protected override string ExportFileNameBase => "godot-ci";
    protected override string ImagesAndConfigsFolder => "docker";
    protected override (string BuildRelativeFolder, string? TargetToStopAt) DefaultImageToBuild => ("ci", null);
    protected override string ImageNameBase => "thrive/godot-ci";

    protected override Task<bool> PostCheckBuild(string tagOrId)
    {
        return CheckDotnetSdkWasInstalled(tagOrId);
    }

    protected override IEnumerable<string> ImagesToPullIfTheyAreOld()
    {
        // For now we use the Fedora images so they can never be that old
        yield break;
    }
}
