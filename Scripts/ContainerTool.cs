namespace Scripts;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ScriptsBase.ToolBases;
using ScriptsBase.Utilities;

public class ContainerTool : ContainerToolBase<Program.ContainerOptions>
{
    public ContainerTool(Program.ContainerOptions options) : base(options)
    {
        ColourConsole.WriteInfoLine($"Selected image type to build: {options.Image}");
    }

    protected override string ExportFileNameBase => options.Image switch
    {
        ImageType.CI => "godot-ci",
        ImageType.NativeBuilder => "native-builder",
        _ => throw new InvalidOperationException("Unknown image type"),
    };

    protected override string ImagesAndConfigsFolder => "podman";

    protected override (string BuildRelativeFolder, string? TargetToStopAt) DefaultImageToBuild => options.Image switch
    {
        ImageType.CI => ("ci", null),
        ImageType.NativeBuilder => ("native_builder", null),
        _ => throw new InvalidOperationException("Unknown image type"),
    };

    protected override string ImageNameBase => $"thrive/{ExportFileNameBase}";

    protected override bool SaveByDefault => options.Image != ImageType.NativeBuilder;

    protected override Task<bool> PostCheckBuild(string tagOrId)
    {
        if (options.Image == ImageType.NativeBuilder)
        {
            // No post build check for now in the native container as it doesn't even have dotnet
            return Task.FromResult(true);
        }

        return CheckDotnetSdkWasInstalled(tagOrId);
    }

    protected override IEnumerable<string> ImagesToPullIfTheyAreOld()
    {
        if (options.Image == ImageType.NativeBuilder)
        {
            // To update the image the relevant Dockerfile must also be updated
            // ReSharper disable once StringLiteralTypo
            yield return "almalinux:9";
        }

        // For now the CI uses the Fedora images so they can never be that old
    }
}
