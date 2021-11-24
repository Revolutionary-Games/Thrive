using System;
using System.Globalization;
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
        if (!ReadControlsToEditedInfo())
        {
            GUICommon.Instance.PlayButtonPressSound();
            return;
        }

        var serialized = ValidateFormData();

        if (serialized == null)
        {
            GUICommon.Instance.PlayButtonPressSound();
            return;
        }

        ClearError();

        dialog.Hide();
        EmitSignal(nameof(OnAccepted), serialized);
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
        name.Text = editedInfo.Name;
        internalName.Text = editedInfo.InternalName;
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

    private bool ReadControlsToEditedInfo()
    {
        editedInfo.Name = name.Text;
        editedInfo.InternalName = internalName.Text;
        editedInfo.Author = author.Text;
        editedInfo.Version = version.Text;
        editedInfo.Description = description.Text;
        editedInfo.LongDescription = longDescription.Text;
        editedInfo.Icon = iconFile.Text;
        editedInfo.License = license.Text;
        editedInfo.RecommendedThriveVersion = recommendedThrive.Text;
        editedInfo.MinimumThriveVersion = minimumThrive.Text;
        editedInfo.MaximumThriveVersion = maximumThrive.Text;
        editedInfo.PckToLoad = pckName.Text;
        editedInfo.ModAssembly = modAssembly.Text;
        editedInfo.AssemblyModClass = assemblyModClass.Text;

        if (string.IsNullOrWhiteSpace(infoUrl.Text))
        {
            editedInfo.InfoUrl = null;
        }
        else
        {
            if (Uri.TryCreate(infoUrl.Text, UriKind.Absolute, out Uri parsed))
            {
                editedInfo.InfoUrl = parsed;
            }
            else
            {
                SetError(TranslationServer.Translate("INVALID_URL_FORMAT"));
                return false;
            }
        }

        return true;
    }

    private string ValidateFormData()
    {
        if (string.IsNullOrWhiteSpace(editedInfo.InternalName))
        {
            SetError(TranslationServer.Translate("INTERNAL_NAME_REQUIRED"));
            return null;
        }

        if (ModLoader.LoadModInfo(editedInfo.InternalName, false) != null)
        {
            SetError(TranslationServer.Translate("INTERNAL_NAME_IN_USE"));
            return null;
        }

        var serialized = new StringWriter();

        var finalResult = new FullModDetails(editedInfo.InternalName)
        {
            Info = editedInfo,
            Folder = Path.Combine(Constants.ModLocations[Constants.ModLocations.Count - 1], internalName.Text),
        };

        try
        {
            JsonSerializer.Create().Serialize(serialized, finalResult);
        }
        catch (JsonSerializationException e)
        {
            SetError(string.Format(CultureInfo.CurrentCulture,
                TranslationServer.Translate("MISSING_OR_INVALID_REQUIRED_FIELD"), e.Message));
            return null;
        }

        try
        {
            ModManager.ValidateModInfo(editedInfo, true);
        }
        catch (Exception e)
        {
            SetError(string.Format(CultureInfo.CurrentCulture,
                TranslationServer.Translate("ADDITIONAL_VALIDATION_FAILED"), e.Message));
            return null;
        }

        return serialized.ToString();
    }

    private void SetError(string message)
    {
        if (message == null)
        {
            ClearError();
        }

        errorDisplay.Text = string.Format(CultureInfo.CurrentCulture, TranslationServer.Translate("FORM_ERROR_MESSAGE"),
            message);
    }

    private void ClearError()
    {
        errorDisplay.Text = string.Empty;
    }
}
