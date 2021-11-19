using System.IO;
using Godot;
using Newtonsoft.Json;
using Path = System.IO.Path;

public class NewModGUI : Control
{
    [Export]
    public NodePath DialogPath;

    [Export]
    public NodePath InternalNamePath;

    [Export]
    public NodePath NamePath;

    [Export]
    public NodePath AuthorPath;

    [Export]
    public NodePath VersionPath;

    [Export]
    public NodePath DescriptionPath;

    [Export]
    public NodePath LongDescriptionPath;

    [Export]
    public NodePath IconFilePath;

    [Export]
    public NodePath InfoUrlPath;

    [Export]
    public NodePath LicensePath;

    [Export]
    public NodePath RecommendedThrivePath;

    [Export]
    public NodePath MinimumThrivePath;

    [Export]
    public NodePath MaximumThrivePath;

    [Export]
    public NodePath PckNamePath;

    [Export]
    public NodePath ModAssemblyPath;

    [Export]
    public NodePath AssemblyModClassPath;

    [Export]
    public NodePath ErrorDisplayPath;

    private CustomDialog dialog;

    private LineEdit internalName;
    private LineEdit name;
    private LineEdit author;
    private LineEdit version;
    private LineEdit description;
    private TextEdit longDescription;
    private LineEdit iconFile;
    private LineEdit infoUrl;
    private LineEdit license;
    private LineEdit recommendedThrive;
    private LineEdit minimumThrive;
    private LineEdit maximumThrive;
    private LineEdit pckName;
    private LineEdit modAssembly;
    private LineEdit assemblyModClass;

    private Label errorDisplay;

    private ModInfo editedInfo;

    [Signal]
    public delegate void OnCanceled();

    /// <summary>
    ///   Emitted when creation is accepted. Contains the full JSON serialized <see cref="FullModDetails"/> object.
    /// </summary>
    [Signal]
    public delegate void OnAccepted(string newModInfo);

    public override void _Ready()
    {
        dialog = GetNode<CustomDialog>(DialogPath);

        internalName = GetNode<LineEdit>(InternalNamePath);
        name = GetNode<LineEdit>(NamePath);
        author = GetNode<LineEdit>(AuthorPath);
        version = GetNode<LineEdit>(VersionPath);
        description = GetNode<LineEdit>(DescriptionPath);
        longDescription = GetNode<TextEdit>(LongDescriptionPath);
        iconFile = GetNode<LineEdit>(IconFilePath);
        infoUrl = GetNode<LineEdit>(InfoUrlPath);
        license = GetNode<LineEdit>(LicensePath);
        recommendedThrive = GetNode<LineEdit>(RecommendedThrivePath);
        minimumThrive = GetNode<LineEdit>(MinimumThrivePath);
        maximumThrive = GetNode<LineEdit>(MaximumThrivePath);
        pckName = GetNode<LineEdit>(PckNamePath);
        modAssembly = GetNode<LineEdit>(ModAssemblyPath);
        assemblyModClass = GetNode<LineEdit>(AssemblyModClassPath);

        errorDisplay = GetNode<Label>(ErrorDisplayPath);
    }

    public void Open()
    {
        ResetForm();

        dialog.PopupCenteredShrink();
    }

    private void Closed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(nameof(OnCanceled));
    }

    private void Cancel()
    {
        dialog.Hide();
    }

    private void Create()
    {
        // TODO: validation
        /*if (false)
        {
            GUICommon.Instance.PlayButtonPressSound();
        }*/

        errorDisplay.Text = string.Empty;

        var finalResult = new FullModDetails(internalName.Text)
        {
            Info = editedInfo,
            Folder = Path.Combine(Constants.ModLocations[Constants.ModLocations.Count - 1], internalName.Text),
        };

        var serialized = new StringWriter();

        JsonSerializer.Create().Serialize(serialized, finalResult);

        Hide();
        EmitSignal(nameof(OnAccepted), serialized.ToString());
    }

    private void WikiLinkPressed()
    {
        if (OS.ShellOpen("https://wiki.revolutionarygamesstudio.com/wiki/Modding") != Error.Ok)
        {
            GD.PrintErr("Failed to open modding guide wiki page");
        }
    }

    private void ResetForm()
    {
        editedInfo = new ModInfo
        {
            Author = Settings.EnvironmentUserName,
            Version = "1.0",
            Description = TranslationServer.Translate("NEW_MOD_DEFAULT_DESCRIPTION"),
            License = "proprietary",
            RecommendedThriveVersion = Constants.Version,
            MinimumThriveVersion = Constants.Version,
        };

        ApplyEditedInfoToControls();
    }

    private void ApplyEditedInfoToControls()
    {
        internalName.Text = string.Empty;
        name.Text = editedInfo.Name;
        author.Text = editedInfo.Author;
        version.Text = editedInfo.Version;
        description.Text = editedInfo.Description;
        longDescription.Text = editedInfo.LongDescription;
        iconFile.Text = editedInfo.Icon;
        infoUrl.Text = editedInfo.InfoUrl == null ? string.Empty : editedInfo.InfoUrl.ToString();
        license.Text = editedInfo.License;
        recommendedThrive.Text = editedInfo.RecommendedThriveVersion;
        minimumThrive.Text = editedInfo.MinimumThriveVersion;
        maximumThrive.Text = editedInfo.MaximumThriveVersion;
        pckName.Text = editedInfo.PckToLoad;
        modAssembly.Text = editedInfo.ModAssembly;
        assemblyModClass.Text = editedInfo.AssemblyModClass;
    }
}
