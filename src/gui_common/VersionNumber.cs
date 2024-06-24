using System.Globalization;
using Godot;

/// <summary>
///   Shows a version label
/// </summary>
public partial class VersionNumber : Label
{
    [Export]
    public bool ShowDevBuildInfo = true;

    public override void _Ready()
    {
        UpdateVersion();
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        Localization.Instance.OnTranslationsChanged += UpdateVersion;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        Localization.Instance.OnTranslationsChanged -= UpdateVersion;
    }

    private void UpdateVersion()
    {
        var info = SimulationParameters.Instance.GetBuildInfoIfExists();

        var version = Constants.Version;

        if (info is not { DevBuild: true })
        {
            Text = Constants.Version;
            return;
        }

        var time = info.BuiltAt.ToLocalTime().ToString("G", CultureInfo.CurrentCulture);
        Text = Localization.Translate("DEVBUILD_VERSION_INFO").FormatSafe(info.Commit, info.Branch, time, version);
    }
}
