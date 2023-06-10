using System;
using System.IO;
using System.Linq;
using Godot;
using Newtonsoft.Json;
using Environment = System.Environment;
using File = Godot.File;
using Path = System.IO.Path;

public class NewModGUI : Control
{
    [Export]
    public NodePath? DialogPath;

    [Export]
    public NodePath InternalNamePath = null!;

    [Export]
    public NodePath NamePath = null!;

    [Export]
    public NodePath AuthorPath = null!;

    [Export]
    public NodePath VersionPath = null!;

    [Export]
    public NodePath DescriptionPath = null!;

    [Export]
    public NodePath LongDescriptionPath = null!;

    [Export]
    public NodePath IconFilePath = null!;

    [Export]
    public NodePath PreviewImagesFilePath = null!;

    [Export]
    public NodePath InfoUrlPath = null!;

    [Export]
    public NodePath LicensePath = null!;

    [Export]
    public NodePath RecommendedThrivePath = null!;

    [Export]
    public NodePath MinimumThrivePath = null!;

    [Export]
    public NodePath MaximumThrivePath = null!;

    [Export]
    public NodePath PckNamePath = null!;

    [Export]
    public NodePath ModAssemblyPath = null!;

    [Export]
    public NodePath AssemblyModClassPath = null!;

    [Export]
    public NodePath DependenciesPath = null!;

    [Export]
    public NodePath RequiredModsPath = null!;

    [Export]
    public NodePath LoadBeforePath = null!;

    [Export]
    public NodePath LoadAfterPath = null!;

    [Export]
    public NodePath IncompatibleModsPath = null!;

    [Export]
    public NodePath IconFileDialogPath = null!;

    [Export]
    public NodePath PckFileDialogPath = null!;

    [Export]
    public NodePath AssemblyFileDialogPath = null!;

    [Export]
    public NodePath PreviewFileDialogPath = null!;

    [Export]
    public NodePath AssemblyModAutoHarmonyPath = null!;

    [Export]
    public NodePath ErrorDisplayPath = null!;

#pragma warning disable CA2213
    private CustomWindow dialog = null!;

    private LineEdit internalName = null!;
    private LineEdit name = null!;
    private LineEdit author = null!;
    private LineEdit version = null!;
    private LineEdit description = null!;
    private TextEdit longDescription = null!;
    private LineEdit iconFile = null!;
    private LineEdit previewImagesFile = null!;
    private LineEdit infoUrl = null!;
    private LineEdit license = null!;
    private LineEdit recommendedThrive = null!;
    private LineEdit minimumThrive = null!;
    private LineEdit maximumThrive = null!;
    private LineEdit pckName = null!;
    private LineEdit modAssembly = null!;
    private LineEdit assemblyModClass = null!;
    private CheckBox assemblyModAutoHarmony = null!;
    private LineEdit dependencies = null!;
    private LineEdit requiredMods = null!;
    private LineEdit loadBefore = null!;
    private LineEdit loadAfter = null!;
    private LineEdit incompatibleMods = null!;

    private FileDialog iconFileDialog = null!;
    private FileDialog pckFileDialog = null!;
    private FileDialog assemblyFileDialog = null!;
    private FileDialog previewFileDialog = null!;

    private Label errorDisplay = null!;
#pragma warning restore CA2213

    private ModInfo? editedInfo;

    [Signal]
    public delegate void OnCancelled();

    /// <summary>
    ///   Emitted when creation is accepted. Contains the full JSON serialized <see cref="FullModDetails"/> object.
    /// </summary>
    [Signal]
    public delegate void OnAccepted(string newModInfo);

    private enum DialogFileTypes
    {
        IconFile,
        AssemblyFile,
        PckFile,
    }

    public override void _Ready()
    {
        dialog = GetNode<CustomWindow>(DialogPath);

        internalName = GetNode<LineEdit>(InternalNamePath);
        name = GetNode<LineEdit>(NamePath);
        author = GetNode<LineEdit>(AuthorPath);
        version = GetNode<LineEdit>(VersionPath);
        description = GetNode<LineEdit>(DescriptionPath);
        longDescription = GetNode<TextEdit>(LongDescriptionPath);
        iconFile = GetNode<LineEdit>(IconFilePath);
        previewImagesFile = GetNode<LineEdit>(PreviewImagesFilePath);
        infoUrl = GetNode<LineEdit>(InfoUrlPath);
        license = GetNode<LineEdit>(LicensePath);
        recommendedThrive = GetNode<LineEdit>(RecommendedThrivePath);
        minimumThrive = GetNode<LineEdit>(MinimumThrivePath);
        maximumThrive = GetNode<LineEdit>(MaximumThrivePath);
        pckName = GetNode<LineEdit>(PckNamePath);
        modAssembly = GetNode<LineEdit>(ModAssemblyPath);
        assemblyModClass = GetNode<LineEdit>(AssemblyModClassPath);
        dependencies = GetNode<LineEdit>(DependenciesPath);
        requiredMods = GetNode<LineEdit>(RequiredModsPath);
        loadBefore = GetNode<LineEdit>(LoadBeforePath);
        loadAfter = GetNode<LineEdit>(LoadAfterPath);
        incompatibleMods = GetNode<LineEdit>(IncompatibleModsPath);
        assemblyModAutoHarmony = GetNode<CheckBox>(AssemblyModAutoHarmonyPath);

        iconFileDialog = GetNode<FileDialog>(IconFileDialogPath);
        iconFileDialog.CurrentDir = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        iconFileDialog.AddFilter(Constants.FILE_DIALOG_IMAGE_FILTER);
        pckFileDialog = GetNode<FileDialog>(PckFileDialogPath);
        pckFileDialog.CurrentDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        pckFileDialog.AddFilter(Constants.FILE_DIALOG_PCK_FILTER);
        assemblyFileDialog = GetNode<FileDialog>(AssemblyFileDialogPath);
        assemblyFileDialog.CurrentDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        assemblyFileDialog.AddFilter(Constants.FILE_DIALOG_ASSEMBLY_FILTER);
        previewFileDialog = GetNode<FileDialog>(PreviewFileDialogPath);
        previewFileDialog.CurrentDir = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        previewFileDialog.AddFilter(Constants.FILE_DIALOG_IMAGE_FILTER);

        errorDisplay = GetNode<Label>(ErrorDisplayPath);
    }

    public void Open()
    {
        ResetForm();

        dialog.PopupCenteredShrink();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (DialogPath != null)
            {
                DialogPath.Dispose();
                InternalNamePath.Dispose();
                NamePath.Dispose();
                AuthorPath.Dispose();
                VersionPath.Dispose();
                DescriptionPath.Dispose();
                LongDescriptionPath.Dispose();
                IconFilePath.Dispose();
                InfoUrlPath.Dispose();
                LicensePath.Dispose();
                RecommendedThrivePath.Dispose();
                MinimumThrivePath.Dispose();
                MaximumThrivePath.Dispose();
                PckNamePath.Dispose();
                ModAssemblyPath.Dispose();
                AssemblyModClassPath.Dispose();
                AssemblyModAutoHarmonyPath.Dispose();
                ErrorDisplayPath.Dispose();
                DependenciesPath.Dispose();
                RequiredModsPath.Dispose();
                LoadBeforePath.Dispose();
                LoadAfterPath.Dispose();
                IncompatibleModsPath.Dispose();
                IconFileDialogPath.Dispose();
                PckFileDialogPath.Dispose();
                AssemblyFileDialogPath.Dispose();
                PreviewFileDialogPath.Dispose();
                PreviewImagesFilePath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void Closed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(nameof(OnCancelled));
    }

    private void Cancel()
    {
        dialog.Hide();
    }

    private void Create()
    {
        if (editedInfo == null)
        {
            GD.PrintErr("Create called with edited info being null");
            return;
        }

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
        name.Text = editedInfo!.Name;
        internalName.Text = editedInfo.InternalName;
        author.Text = editedInfo.Author;
        version.Text = editedInfo.Version;
        description.Text = editedInfo.Description;
        longDescription.Text = editedInfo.LongDescription;
        iconFile.Text = editedInfo.Icon;
        previewImagesFile.Text =
            editedInfo.PreviewImages == null ? string.Empty : string.Join(", ", editedInfo.PreviewImages);
        infoUrl.Text = editedInfo.InfoUrl == null ? string.Empty : editedInfo.InfoUrl.ToString();
        license.Text = editedInfo.License;
        recommendedThrive.Text = editedInfo.RecommendedThriveVersion;
        minimumThrive.Text = editedInfo.MinimumThriveVersion;
        maximumThrive.Text = editedInfo.MaximumThriveVersion;
        pckName.Text = editedInfo.PckToLoad;
        modAssembly.Text = editedInfo.ModAssembly;
        assemblyModClass.Text = editedInfo.AssemblyModClass;
        dependencies.Text = editedInfo.Dependencies == null ? string.Empty : string.Join(", ", editedInfo.Dependencies);
        requiredMods.Text = editedInfo.RequiredMods == null ? string.Empty : string.Join(", ", editedInfo.RequiredMods);
        loadBefore.Text = editedInfo.LoadBeforeThis == null ?
            string.Empty :
            string.Join(", ", editedInfo.LoadBeforeThis);
        loadAfter.Text = editedInfo.LoadAfterThis == null ? string.Empty : string.Join(", ", editedInfo.LoadAfterThis);
        incompatibleMods.Text = editedInfo.IncompatibleMods == null ?
            string.Empty :
            string.Join(", ", editedInfo.IncompatibleMods);
        assemblyModAutoHarmony.Pressed = editedInfo.UseAutoHarmony ?? false;
    }

    private bool ReadControlsToEditedInfo()
    {
        editedInfo!.Name = name.Text;
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
        editedInfo.PreviewImages = StringUtils.SplitByComma(previewImagesFile.Text);
        editedInfo.Dependencies = StringUtils.SplitByComma(dependencies.Text);
        editedInfo.RequiredMods = StringUtils.SplitByComma(requiredMods.Text);
        editedInfo.LoadBeforeThis = StringUtils.SplitByComma(loadBefore.Text);
        editedInfo.LoadAfterThis = StringUtils.SplitByComma(loadAfter.Text);
        editedInfo.IncompatibleMods = StringUtils.SplitByComma(incompatibleMods.Text);
        editedInfo.UseAutoHarmony = assemblyModAutoHarmony.Pressed;

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

    private string? ValidateFormData()
    {
        if (editedInfo == null)
            throw new InvalidOperationException("Validate form called without editing info");

        if (string.IsNullOrWhiteSpace(editedInfo.InternalName))
        {
            SetError(TranslationServer.Translate("INTERNAL_NAME_REQUIRED"));
            return null;
        }

        if (!char.IsUpper(editedInfo.InternalName, 0))
        {
            SetError(TranslationServer.Translate("INTERNAL_NAME_REQUIRES_CAPITAL"));
            return null;
        }

        if (ModLoader.LoadModInfo(editedInfo.InternalName, false) != null)
        {
            SetError(TranslationServer.Translate("INTERNAL_NAME_IN_USE"));
            return null;
        }

        var serialized = new StringWriter();

        var finalResult = new FullModDetails(editedInfo.InternalName,
            Path.Combine(Constants.ModLocations[Constants.ModLocations.Count - 1], internalName.Text), editedInfo);

        try
        {
            JsonSerializer.Create().Serialize(serialized, finalResult);
        }
        catch (JsonSerializationException e)
        {
            SetError(TranslationServer.Translate("MISSING_OR_INVALID_REQUIRED_FIELD").FormatSafe(e.Message));
            return null;
        }

        try
        {
            ModManager.ValidateModInfo(editedInfo, true);
        }
        catch (Exception e)
        {
            SetError(TranslationServer.Translate("ADDITIONAL_VALIDATION_FAILED").FormatSafe(e.Message));
            return null;
        }

        return serialized.ToString();
    }

    private void SetError(string? message)
    {
        if (message == null)
        {
            ClearError();
        }

        errorDisplay.Text = TranslationServer.Translate("FORM_ERROR_MESSAGE").FormatSafe(message);
    }

    private void ClearError()
    {
        errorDisplay.Text = string.Empty;
    }

    private void ChooseIconButtonPressed()
    {
        iconFileDialog.PopupCentered();
    }

    private void DialogSingleFileSelected(string dialogSelected)
    {
        using var file = new File();

        switch ((DialogFileTypes)Enum.Parse(typeof(DialogFileTypes), dialogSelected))
        {
            case DialogFileTypes.IconFile:
            {
                if (!file.FileExists(iconFileDialog.CurrentFile))
                {
                    iconFile.Text = iconFileDialog.CurrentFile;
                }

                break;
            }

            case DialogFileTypes.AssemblyFile:
            {
                if (!file.FileExists(assemblyFileDialog.CurrentFile))
                {
                    modAssembly.Text = assemblyFileDialog.CurrentFile;
                }

                break;
            }

            case DialogFileTypes.PckFile:
            {
                if (!file.FileExists(pckFileDialog.CurrentFile))
                {
                    pckName.Text = pckFileDialog.CurrentFile;
                }

                break;
            }
        }
    }

    private void ChoosePckButtonPressed()
    {
        pckFileDialog.PopupCentered();
    }

    private void ChooseAssemblyButtonPressed()
    {
        assemblyFileDialog.PopupCentered();
    }

    private void ChooseImagesButtonPressed()
    {
        previewFileDialog.PopupCentered();
    }

    private void PreviewFileDialogFilesSelected(string[] paths)
    {
        string[] allFileNames = paths.Select(Path.GetFileName).ToArray();
        previewImagesFile.Text = string.Join(", ", allFileNames);
    }
}
