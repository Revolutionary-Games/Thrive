using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Godot;
using Newtonsoft.Json;
using Environment = System.Environment;
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
    public NodePath PreviewImagesFilePath;

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
    public NodePath DependenciesPath;

    [Export]
    public NodePath RequiredModsPath;

    [Export]
    public NodePath LoadBeforePath;

    [Export]
    public NodePath LoadAfterPath;

    [Export]
    public NodePath IncompatibleModsPath;

    [Export]
    public NodePath ModConfigPath;

    [Export]
    public NodePath EnableConfigCheckboxPath;

    [Export]
    public NodePath IconFileDialogPath;

    [Export]
    public NodePath PckFileDialogPath;

    [Export]
    public NodePath AssemblyFileDialogPath;

    [Export]
    public NodePath PreviewFileDialogPath;

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
    private LineEdit previewImagesFile;
    private LineEdit infoUrl;
    private LineEdit license;
    private LineEdit recommendedThrive;
    private LineEdit minimumThrive;
    private LineEdit maximumThrive;
    private LineEdit pckName;
    private LineEdit modAssembly;
    private LineEdit assemblyModClass;
    private LineEdit dependencies;
    private LineEdit requiredMods;
    private LineEdit loadBefore;
    private LineEdit loadAfter;
    private LineEdit incompatibleMods;
    private LineEdit modConfig;

    private CheckButton enableConfigCheckbox;

    private FileDialog iconFileDialog;
    private FileDialog pckFileDialog;
    private FileDialog assemblyFileDialog;
    private FileDialog previewFileDialog;

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
        modConfig = GetNode<LineEdit>(ModConfigPath);

        enableConfigCheckbox = GetNode<CheckButton>(EnableConfigCheckboxPath);

        iconFileDialog = GetNode<FileDialog>(IconFileDialogPath);
        iconFileDialog.CurrentDir = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        pckFileDialog = GetNode<FileDialog>(PckFileDialogPath);
        pckFileDialog.CurrentDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        assemblyFileDialog = GetNode<FileDialog>(AssemblyFileDialogPath);
        assemblyFileDialog.CurrentDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        previewFileDialog = GetNode<FileDialog>(PreviewFileDialogPath);
        previewFileDialog.CurrentDir = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

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
        loadBefore.Text = editedInfo.LoadBefore == null ? string.Empty : string.Join(", ", editedInfo.LoadBefore);
        loadAfter.Text = editedInfo.LoadAfter == null ? string.Empty : string.Join(", ", editedInfo.LoadAfter);
        incompatibleMods.Text = editedInfo.IncompatibleMods == null ?
            string.Empty :
            string.Join(", ", editedInfo.IncompatibleMods);

        enableConfigCheckbox.SetPressedNoSignal(!string.IsNullOrWhiteSpace(editedInfo.ConfigToLoad));
        modConfig.Editable = enableConfigCheckbox.Pressed;
        modConfig.Text = string.IsNullOrWhiteSpace(editedInfo.ConfigToLoad) ? string.Empty : editedInfo.ConfigToLoad;
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

        if (!string.IsNullOrWhiteSpace(previewImagesFile.Text))
        {
            var previewImagesList = new List<string>();
            Array.ForEach(previewImagesFile.Text.Split(","), s => previewImagesList.Add(s.Trim()));
            editedInfo.PreviewImages = previewImagesList;
        }

        if (!string.IsNullOrWhiteSpace(dependencies.Text))
        {
            var dependenciesList = new List<string>();
            Array.ForEach(dependencies.Text.Split(","), s => dependenciesList.Add(s.Trim()));
            editedInfo.Dependencies = dependenciesList;
        }

        if (!string.IsNullOrWhiteSpace(requiredMods.Text))
        {
            var requiredModsList = new List<string>();
            Array.ForEach(requiredMods.Text.Split(","), s => requiredModsList.Add(s.Trim()));
            editedInfo.RequiredMods = requiredModsList;
        }

        if (!string.IsNullOrWhiteSpace(loadBefore.Text))
        {
            var loadBeforeList = new List<string>();
            Array.ForEach(loadBefore.Text.Split(","), s => loadBeforeList.Add(s.Trim()));
            editedInfo.LoadBefore = loadBeforeList;
        }

        if (!string.IsNullOrWhiteSpace(loadAfter.Text))
        {
            var loadAfterList = new List<string>();
            Array.ForEach(loadAfter.Text.Split(","), s => loadAfterList.Add(s.Trim()));
            editedInfo.LoadAfter = loadAfterList;
        }

        if (!string.IsNullOrWhiteSpace(incompatibleMods.Text))
        {
            var incompatibleModsList = new List<string>();
            Array.ForEach(incompatibleMods.Text.Split(","), s => incompatibleModsList.Add(s.Trim()));
            editedInfo.IncompatibleMods = incompatibleModsList;
        }

        if (enableConfigCheckbox.Pressed && !string.IsNullOrWhiteSpace(modConfig.Text))
        {
            editedInfo.ConfigToLoad = modConfig.Text;
            if (!modConfig.Text.EndsWith(".json"))
            {
                editedInfo.ConfigToLoad += ".json";
            }
        }

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

    private void ChooseIconButtonPressed()
    {
        iconFileDialog.PopupCentered();
    }

    private void IconFileDialogFileSelected(string path)
    {
        if (Path.GetFileName(path) == iconFileDialog.CurrentFile)
        {
            iconFile.Text = iconFileDialog.CurrentFile;
        }
    }

    private void ChoosePckButtonPressed()
    {
        pckFileDialog.PopupCentered();
    }

    private void PckFileDialogFileSelected(string path)
    {
        if (Path.GetFileName(path) == pckFileDialog.CurrentFile)
        {
            pckName.Text = pckFileDialog.CurrentFile;
        }
    }

    private void ChooseAssemblyButtonPressed()
    {
        assemblyFileDialog.PopupCentered();
    }

    private void AssemblyFileDialogFileSelected(string path)
    {
        if (Path.GetFileName(path) == assemblyFileDialog.CurrentFile)
        {
            modAssembly.Text = assemblyFileDialog.CurrentFile;
        }
    }

    private void ChooseImagesButtonPressed()
    {
        previewFileDialog.PopupCentered();
    }

    private void PreviewFileDialogFilesSelected(string[] paths)
    {
        List<string> allFileNames = new List<string>();
        Array.ForEach(paths, currentPath => allFileNames.Add(Path.GetFileName(currentPath)));
        previewImagesFile.Text = string.Join(", ", allFileNames);
    }

    private void EnableConfigCheckboxToggled(bool enabled)
    {
        modConfig.Editable = enabled;
    }
}
