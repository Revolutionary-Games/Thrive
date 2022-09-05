namespace Scripts;

using System.Threading.Tasks;
using ScriptsBase.Utilities;

public class ContainerTool : ContainerToolBase<Program.ContainerOptions>
{
    public ContainerTool(Program.ContainerOptions options) : base(options)
    {
    }

    protected override string ExportFileNameBase => "godot-ci";
    protected override string ImagesAndConfigsFolder => "docker";
    protected override string DefaultImageToBuild => "ci";
    protected override string ImageNameBase => "thrive/godot-ci";

    protected override Task<bool> PostCheckBuild(string tagOrId)
    {
        return CheckDotnetSdkWasInstalled(tagOrId);
    }
}
