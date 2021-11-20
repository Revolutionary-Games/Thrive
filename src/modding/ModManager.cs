using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using Newtonsoft.Json;
using Directory = Godot.Directory;
using File = Godot.File;
using Path = System.IO.Path;

/// <summary>
///   Provides GUI for managing enabled mods
/// </summary>
public class ModManager : Control
{
    [Export]
    public NodePath LeftArrowPath;

    [Export]
    public NodePath RightArrowPath;

    [Export]
    public NodePath AvailableModsContainerPath;

    [Export]
    public NodePath EnabledModsContainerPath;

    [Export]
    public NodePath OpenModInfoButtonPath;

    [Export]
    public NodePath OpenModUrlButtonPath;

    [Export]
    public NodePath DisableAllModsButtonPath;

    [Export]
    public NodePath SelectedModNamePath;

    [Export]
    public NodePath SelectedModIconPath;

    [Export]
    public NodePath SelectedModAuthorPath;

    [Export]
    public NodePath SelectedModVersionPath;

    [Export]
    public NodePath SelectedModRecommendedThriveVersionPath;

    [Export]
    public NodePath SelectedModMinimumThriveVersionPath;

    [Export]
    public NodePath SelectedModDescriptionPath;

    [Export]
    public NodePath ApplyChangesButtonPath;

    [Export]
    public NodePath UnAppliedChangesWarningPath;

    [Export]
    public NodePath ModFullInfoPopupPath;

    [Export]
    public NodePath FullInfoNamePath;

    [Export]
    public NodePath FullInfoAuthorPath;

    [Export]
    public NodePath FullInfoVersionPath;

    [Export]
    public NodePath FullInfoDescriptionPath;

    [Export]
    public NodePath FullInfoLongDescriptionPath;

    [Export]
    public NodePath FullInfoIconFilePath;

    [Export]
    public NodePath FullInfoInfoUrlPath;

    [Export]
    public NodePath FullInfoLicensePath;

    [Export]
    public NodePath FullInfoRecommendedThrivePath;

    [Export]
    public NodePath FullInfoMinimumThrivePath;

    [Export]
    public NodePath FullInfoMaximumThrivePath;

    [Export]
    public NodePath FullInfoPckNamePath;

    [Export]
    public NodePath FullInfoModAssemblyPath;

    [Export]
    public NodePath FullInfoAssemblyModClassPath;

    [Export]
    public NodePath NewModGUIPath;

    [Export]
    public NodePath ModCreateErrorDialogPath;

    private readonly List<FullModDetails> validMods = new();

    private List<FullModDetails> notEnabledMods;
    private List<FullModDetails> enabledMods;

    private Button leftArrow;
    private Button rightArrow;

    private ItemList availableModsContainer;
    private ItemList enabledModsContainer;

    private Button openModInfoButton;
    private Button openModUrlButton;
    private Button disableAllModsButton;
    private Label selectedModName;
    private TextureRect selectedModIcon;
    private Label selectedModAuthor;
    private Label selectedModVersion;
    private Label selectedModRecommendedThriveVersion;
    private Label selectedModMinimumThriveVersion;
    private Label selectedModDescription;

    private Button applyChangesButton;

    private CustomDialog unAppliedChangesWarning;

    private CustomDialog modFullInfoPopup;
    private Label fullInfoName;
    private Label fullInfoAuthor;
    private Label fullInfoVersion;
    private Label fullInfoDescription;
    private Label fullInfoLongDescription;
    private Label fullInfoIconFile;
    private Label fullInfoInfoUrl;
    private Label fullInfoLicense;
    private Label fullInfoRecommendedThrive;
    private Label fullInfoMinimumThrive;
    private Label fullInfoMaximumThrive;
    private Label fullInfoPckName;
    private Label fullInfoModAssembly;
    private Label fullInfoAssemblyModClass;

    private NewModGUI newModGUI;

    private ErrorDialog modCreateErrorDialog;

    private FullModDetails selectedMod;

    [Signal]
    public delegate void OnClosed();

    /// <summary>
    ///   Loads mod info from a folder
    /// </summary>
    /// <param name="folder">Folder to load from</param>
    /// <returns>The info object if the info was valid, null otherwise</returns>
    public static ModInfo LoadModInfo(string folder)
    {
        var infoFile = Path.Combine(folder, Constants.MOD_INFO_FILE_NAME);

        using var file = new File();

        if (file.Open(infoFile, File.ModeFlags.Read) != Error.Ok)
        {
            GD.PrintErr("Can't read mod info file at: ", infoFile);
            return null;
        }

        var data = file.GetAsText();

        return ParseModInfoString(data, false);
    }

    /// <summary>
    ///   Parses a <see cref="ModInfo"/> object from a string
    /// </summary>
    /// <param name="data">The string to parse</param>
    /// <param name="throwOnError">If true exceptions are thrown instead of returning null</param>
    /// <returns>The parsed info</returns>
    /// <exception cref="ArgumentException">
    ///   If <see cref="throwOnError"/> is specified this is thrown if extra validation fails
    /// </exception>
    /// <exception cref="JsonException">Thrown if JSON parsing or JSON validation fails</exception>
    public static ModInfo ParseModInfoString(string data, bool throwOnError)
    {
        ModInfo info;

        try
        {
            info = JsonSerializer.Create().Deserialize<ModInfo>(new JsonTextReader(new StringReader(data)));
        }
        catch (JsonException e)
        {
            if (throwOnError)
                throw;

            GD.PrintErr("Can't read mod info due to JSON exception: ", e);
            return null;
        }

        if (!ValidateModInfo(info, throwOnError))
            return null;

        return info;
    }

    /// <summary>
    ///   Checks that ModInfo has no invalid data
    /// </summary>
    /// <param name="info">The mod info to validate</param>
    /// <param name="throwOnError">If true throws an error</param>
    /// <returns>True if info is valid</returns>
    /// <exception cref="ArgumentException">On invalid info if <see cref="throwOnError"/> is true</exception>
    public static bool ValidateModInfo(ModInfo info, bool throwOnError)
    {
        if (!string.IsNullOrEmpty(info?.Icon))
        {
            if (!IsAllowedModPath(info.Icon))
            {
                if (throwOnError)
                {
                    throw new ArgumentException(TranslationServer.Translate("INVALID_ICON_PATH"));
                }

                GD.PrintErr("Invalid icon specified for mod: ", info.Icon);
                return false;
            }
        }

        if (info?.InfoUrl != null)
        {
            if (info.InfoUrl.Scheme != "http" && info.InfoUrl.Scheme != "https")
            {
                if (throwOnError)
                {
                    throw new ArgumentException(TranslationServer.Translate("INVALID_URL_SCHEME"));
                }

                GD.PrintErr("Disallowed URI scheme in: ", info.InfoUrl);
                return false;
            }
        }

        if (info?.ModAssembly != null && info.AssemblyModClass == null)
        {
            if (throwOnError)
            {
                throw new ArgumentException(TranslationServer.Translate("ASSEMBLY_CLASS_REQUIRED"));
            }

            GD.PrintErr("AssemblyModClass must be set if ModAssembly is set");
            return false;
        }

        return true;
    }

    public override void _Ready()
    {
        base._Ready();

        leftArrow = GetNode<Button>(LeftArrowPath);
        rightArrow = GetNode<Button>(RightArrowPath);

        availableModsContainer = GetNode<ItemList>(AvailableModsContainerPath);
        enabledModsContainer = GetNode<ItemList>(EnabledModsContainerPath);

        openModInfoButton = GetNode<Button>(OpenModInfoButtonPath);
        openModUrlButton = GetNode<Button>(OpenModUrlButtonPath);
        disableAllModsButton = GetNode<Button>(DisableAllModsButtonPath);
        selectedModName = GetNode<Label>(SelectedModNamePath);
        selectedModIcon = GetNode<TextureRect>(SelectedModIconPath);
        selectedModAuthor = GetNode<Label>(SelectedModAuthorPath);
        selectedModVersion = GetNode<Label>(SelectedModVersionPath);
        selectedModRecommendedThriveVersion = GetNode<Label>(SelectedModRecommendedThriveVersionPath);
        selectedModMinimumThriveVersion = GetNode<Label>(SelectedModMinimumThriveVersionPath);
        selectedModDescription = GetNode<Label>(SelectedModDescriptionPath);

        applyChangesButton = GetNode<Button>(ApplyChangesButtonPath);

        unAppliedChangesWarning = GetNode<CustomDialog>(UnAppliedChangesWarningPath);

        modFullInfoPopup = GetNode<CustomDialog>(ModFullInfoPopupPath);
        fullInfoName = GetNode<Label>(FullInfoNamePath);
        fullInfoAuthor = GetNode<Label>(FullInfoAuthorPath);
        fullInfoVersion = GetNode<Label>(FullInfoVersionPath);
        fullInfoDescription = GetNode<Label>(FullInfoDescriptionPath);
        fullInfoLongDescription = GetNode<Label>(FullInfoLongDescriptionPath);
        fullInfoIconFile = GetNode<Label>(FullInfoIconFilePath);
        fullInfoInfoUrl = GetNode<Label>(FullInfoInfoUrlPath);
        fullInfoLicense = GetNode<Label>(FullInfoLicensePath);
        fullInfoRecommendedThrive = GetNode<Label>(FullInfoRecommendedThrivePath);
        fullInfoMinimumThrive = GetNode<Label>(FullInfoMinimumThrivePath);
        fullInfoMaximumThrive = GetNode<Label>(FullInfoMaximumThrivePath);
        fullInfoPckName = GetNode<Label>(FullInfoPckNamePath);
        fullInfoModAssembly = GetNode<Label>(FullInfoModAssemblyPath);
        fullInfoAssemblyModClass = GetNode<Label>(FullInfoAssemblyModClassPath);

        newModGUI = GetNode<NewModGUI>(NewModGUIPath);

        // This is hidden in the editor to make selecting UI elements there easier
        newModGUI.Visible = true;

        modCreateErrorDialog = GetNode<ErrorDialog>(ModCreateErrorDialogPath);

        UpdateSelectedModInfo();
    }

    /// <summary>
    ///   Refreshes things that need refreshing when this is opened
    /// </summary>
    public void OnOpened()
    {
        // TODO: OnOpened being required to be called probably means that you can't directly run the mod manager
        // scene from Godot editor, probably needs to change to the approach to be to automatically call this each
        // time this becomes visible like the save manager
        enabledMods = new List<FullModDetails>();
        notEnabledMods = new List<FullModDetails>();

        RefreshAvailableMods();
        RefreshEnabledMods();

        availableModsContainer.UnselectAll();
        enabledModsContainer.UnselectAll();

        UpdateOverallModButtons();
    }

    private static bool IsAllowedModPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        if (path.Contains("//") || path.Contains("..") || path.StartsWith("/", StringComparison.Ordinal))
            return false;

        return true;
    }

    private void RefreshAvailableMods()
    {
        if (availableModsContainer.IsAnythingSelected())
        {
            selectedMod = null;
            UpdateSelectedModInfo();
        }

        availableModsContainer.Clear();

        // TODO: only reload new or gone mods and not the entire list
        // TODO: maybe this should be done in a background thread (or maybe icon loading is the slower part)
        validMods.Clear();
        validMods.AddRange(LoadValidMods());

        notEnabledMods = validMods.Where(m => !IsModEnabled(m)).Concat(notEnabledMods.Where(validMods.Contains))
            .Distinct()
            .ToList();

        foreach (var mod in notEnabledMods)
        {
            availableModsContainer.AddItem(mod.InternalName, LoadModIcon(mod));
        }

        // If we found new mod folders that happen to be enabled already, add the mods to that list
        var foundStillEnabledMods = validMods.Where(IsModEnabled);

        foreach (var newMod in foundStillEnabledMods.Where(m => !enabledMods.Contains(m)))
        {
            enabledMods.Add(newMod);

            enabledModsContainer.AddItem(newMod.InternalName, LoadModIcon(newMod));
        }

        UpdateOverallModButtons();
    }

    private void RefreshEnabledMods()
    {
        if (enabledModsContainer.IsAnythingSelected())
        {
            selectedMod = null;
            UpdateSelectedModInfo();
        }

        enabledModsContainer.Clear();

        enabledMods = validMods.Where(IsModEnabled).ToList();

        foreach (var mod in enabledMods)
        {
            enabledModsContainer.AddItem(mod.InternalName, LoadModIcon(mod));
        }
    }

    private bool IsModEnabled(FullModDetails mod)
    {
        return Settings.Instance.EnabledMods.Value.Contains(mod.InternalName) || enabledMods.Contains(mod);
    }

    private void UpdateSelectedModInfo()
    {
        if (selectedMod != null)
        {
            selectedModName.Text = selectedMod.Info.Name;
            selectedModIcon.Texture = LoadModIcon(selectedMod);
            selectedModAuthor.Text = selectedMod.Info.Author;
            selectedModVersion.Text = selectedMod.Info.Version;
            selectedModRecommendedThriveVersion.Text = selectedMod.Info.RecommendedThriveVersion;
            selectedModMinimumThriveVersion.Text = selectedMod.Info.MinimumThriveVersion;
            selectedModDescription.Text = selectedMod.Info.Description;
            openModUrlButton.Disabled = selectedMod.Info.InfoUrl == null;

            openModInfoButton.Disabled = false;

            if (notEnabledMods.Contains(selectedMod))
            {
                leftArrow.Disabled = true;
                rightArrow.Disabled = false;
            }
            else
            {
                leftArrow.Disabled = false;
                rightArrow.Disabled = true;
            }
        }
        else
        {
            selectedModName.Text = TranslationServer.Translate("NO_SELECTED_MOD");
            selectedModIcon.Texture = null;
            selectedModAuthor.Text = string.Empty;
            selectedModVersion.Text = string.Empty;
            selectedModRecommendedThriveVersion.Text = string.Empty;
            selectedModMinimumThriveVersion.Text = string.Empty;
            selectedModDescription.Text = string.Empty;
            openModUrlButton.Disabled = true;
            openModInfoButton.Disabled = true;

            leftArrow.Disabled = true;
            rightArrow.Disabled = true;
        }
    }

    private Texture LoadModIcon(FullModDetails mod)
    {
        if (string.IsNullOrEmpty(mod.Info?.Icon))
            return null;

        var image = new Image();
        image.Load(Path.Combine(mod.Folder, mod.Info.Icon));

        var texture = new ImageTexture();
        texture.CreateFromImage(image);

        return texture;
    }

    /// <summary>
    ///   Loads info for valid mods
    /// </summary>
    /// <returns>The valid mod names and their info</returns>
    private List<FullModDetails> LoadValidMods()
    {
        var mods = FindModFolders();
        var result = new List<FullModDetails> { Capacity = mods.Count };

        foreach (var modFolder in mods)
        {
            var info = LoadModInfo(modFolder);

            if (info == null)
            {
                GD.PrintErr("Can't read mod info from folder: ", modFolder);
                continue;
            }

            result.Add(new FullModDetails(Path.GetFileName(modFolder))
                { Folder = modFolder, Info = info });
        }

        var previousLength = result.Count;

        result = result.Distinct().ToList();

        if (result.Count != previousLength)
        {
            GD.PrintErr("Multiple mods detected with the same name, only one of them is usable");
        }

        return result;
    }

    /// <summary>
    ///   Finds existing mod folders
    /// </summary>
    /// <returns>List of mod folders that contain mod files</returns>
    private List<string> FindModFolders()
    {
        var result = new List<string>();

        using var currentDirectory = new Directory();

        foreach (var location in Constants.ModLocations)
        {
            if (!currentDirectory.DirExists(location))
                continue;

            if (currentDirectory.Open(location) != Error.Ok)
            {
                GD.PrintErr("Failed to open potential mod folder for reading at: ", location);
                continue;
            }

            if (currentDirectory.ListDirBegin(true, true) != Error.Ok)
            {
                GD.PrintErr("Failed to begin directory listing");
                continue;
            }

            while (true)
            {
                var item = currentDirectory.GetNext();

                if (string.IsNullOrEmpty(item))
                    break;

                if (currentDirectory.DirExists(item))
                {
                    var modsFolder = Path.Combine(location, item);

                    if (currentDirectory.FileExists(Path.Combine(item, Constants.MOD_INFO_FILE_NAME)))
                    {
                        // Found a mod folder
                        result.Add(modsFolder);
                    }
                }
            }

            currentDirectory.ListDirEnd();
        }

        return result;
    }

    /// <summary>
    ///   Opens the user manageable mod folder. Creates it if it doesn't exist already
    /// </summary>
    private void OpenModsFolder()
    {
        var folder = Constants.ModLocations[Constants.ModLocations.Count - 1];

        GD.Print("Opening mod folder: ", folder);

        using var directory = new Directory();

        directory.MakeDirRecursive(folder);

        FolderHelpers.OpenFolder(folder);
    }

    private void EnableModPressed()
    {
        if (selectedMod == null)
        {
            GD.PrintErr("No mod is selected");
            return;
        }

        if (ModIncludesCode(selectedMod.Info))
        {
            // TODO: show a warning popup that can be permanently dismissed
        }

        Texture icon = null;

        foreach (var index in availableModsContainer.GetSelectedItems())
        {
            icon = availableModsContainer.GetItemIcon(index);
            availableModsContainer.RemoveItem(index);
        }

        enabledModsContainer.AddItem(selectedMod.InternalName, icon);

        notEnabledMods.Remove(selectedMod);
        enabledMods.Add(selectedMod);

        OnModChangedLists();
    }

    private void DisableModPressed()
    {
        if (selectedMod == null)
        {
            GD.PrintErr("No mod is selected");
            return;
        }

        Texture icon = null;

        foreach (var index in enabledModsContainer.GetSelectedItems())
        {
            icon = enabledModsContainer.GetItemIcon(index);
            enabledModsContainer.RemoveItem(index);
        }

        availableModsContainer.AddItem(selectedMod.InternalName, icon);

        enabledMods.Remove(selectedMod);
        notEnabledMods.Add(selectedMod);

        OnModChangedLists();
    }

    private void DisableAllPressed()
    {
        if (enabledModsContainer.IsAnythingSelected())
        {
            selectedMod = null;
            UpdateSelectedModInfo();
        }

        while (enabledModsContainer.GetItemCount() > 0)
        {
            var icon = enabledModsContainer.GetItemIcon(0);
            var text = enabledModsContainer.GetItemText(0);
            enabledModsContainer.RemoveItem(0);

            availableModsContainer.AddItem(text, icon);
        }

        notEnabledMods.AddRange(enabledMods);
        enabledMods.Clear();

        UpdateOverallModButtons();
    }

    private void OnModChangedLists()
    {
        selectedMod = null;
        UpdateSelectedModInfo();
        enabledModsContainer.UnselectAll();
        availableModsContainer.UnselectAll();

        UpdateOverallModButtons();
    }

    private void UpdateOverallModButtons()
    {
        applyChangesButton.Disabled =
            Settings.Instance.EnabledMods.Value.ToHashSet()
                .SetEquals(enabledMods.Select(m => m.InternalName));

        disableAllModsButton.Disabled = enabledMods.Count < 1;
    }

    private void ApplyChanges()
    {
        GD.Print("Applying changes to enabled mods");

        Settings.Instance.EnabledMods.Value = enabledMods.Select(m => m.InternalName).ToList();
        ModLoader.Instance.LoadMods();

        GD.Print("Saving settings with new mod list");
        if (!Settings.Instance.Save())
        {
            GD.PrintErr("Failed to save settings");
        }

        applyChangesButton.Disabled = true;
    }

    private void AvailableModSelected(int index)
    {
        var newName = availableModsContainer.GetItemText(index);
        var newItem = validMods.FirstOrDefault(m => m.InternalName == newName);

        if (!Equals(selectedMod, newItem))
        {
            selectedMod = newItem;
            UpdateSelectedModInfo();
        }

        if (enabledModsContainer.IsAnythingSelected())
            enabledModsContainer.UnselectAll();
    }

    private void EnabledModSelected(int index)
    {
        var newName = enabledModsContainer.GetItemText(index);
        var newItem = validMods.FirstOrDefault(m => m.InternalName == newName);

        if (!Equals(selectedMod, newItem))
        {
            selectedMod = newItem;
            UpdateSelectedModInfo();
        }

        if (availableModsContainer.IsAnythingSelected())
            availableModsContainer.UnselectAll();
    }

    private void OpenInfoUrlPressed()
    {
        if (selectedMod?.Info == null)
        {
            GD.PrintErr("No mod is selected");
            return;
        }

        if (OS.ShellOpen(selectedMod.Info.InfoUrl.ToString()) != Error.Ok)
        {
            GD.PrintErr("Failed to open mod URL: ", selectedMod.Info.InfoUrl);
        }
    }

    private bool ModIncludesCode(ModInfo info)
    {
        // TODO: somehow needs to check the .pck file to see if it has gdscript in it
        // TODO: check whether scene or resource files can have embedded gdscript in them

        // For now just detect if a C# assembly is defined
        return !string.IsNullOrEmpty(info.ModAssembly);
    }

    private void OpenModInfoPopup()
    {
        var info = selectedMod?.Info;
        if (info == null)
        {
            GD.PrintErr("No mod is selected");
            return;
        }

        fullInfoName.Text = info.Name;
        fullInfoAuthor.Text = info.Author;
        fullInfoVersion.Text = info.Version;
        fullInfoDescription.Text = info.Description;
        fullInfoLongDescription.Text = info.LongDescription;
        fullInfoIconFile.Text = info.Icon;
        fullInfoInfoUrl.Text = info.InfoUrl == null ? string.Empty : info.InfoUrl.ToString();
        fullInfoLicense.Text = info.License;
        fullInfoRecommendedThrive.Text = info.RecommendedThriveVersion;
        fullInfoMinimumThrive.Text = info.MinimumThriveVersion;
        fullInfoMaximumThrive.Text = info.MaximumThriveVersion;
        fullInfoPckName.Text = info.PckToLoad;
        fullInfoModAssembly.Text = info.ModAssembly;
        fullInfoAssemblyModClass.Text = info.AssemblyModClass;

        modFullInfoPopup.PopupCenteredShrink();
    }

    private void CloseModInfoPopup()
    {
        GUICommon.Instance.PlayButtonPressSound();
        modFullInfoPopup.Hide();
    }

    private void NewModPressed()
    {
        newModGUI.Open();
    }

    private void SetupNewModFolder(string data)
    {
        FullModDetails parsedData;

        try
        {
            parsedData = JsonSerializer.Create()
                .Deserialize<FullModDetails>(new JsonTextReader(new StringReader(data)));

            if (parsedData == null)
                throw new Exception("deserialized value is null");
        }
        catch (Exception e)
        {
            GD.PrintErr("Can't create mod due to parse failure on data: ", e);
            return;
        }

        var serialized = new StringWriter();

        JsonSerializer.Create(new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
        }).Serialize(new JsonTextWriter(serialized) { Indentation = 4 }, parsedData.Info);
        var modInfoText = serialized.ToString();

        GD.Print("Creating new mod at: ", parsedData.Folder);

        using var folder = new Directory();
        if (folder.MakeDirRecursive(parsedData.Folder) != Error.Ok)
        {
            modCreateErrorDialog.ErrorMessage = TranslationServer.Translate("ERROR_CREATING_FOLDER");
            modCreateErrorDialog.ExceptionInfo = null;
            modCreateErrorDialog.PopupCenteredShrink();
            return;
        }

        using var file = new File();
        if (file.Open(Path.Combine(parsedData.Folder, Constants.MOD_INFO_FILE_NAME), File.ModeFlags.Write) != Error.Ok)
        {
            modCreateErrorDialog.ErrorMessage = TranslationServer.Translate("ERROR_CREATING_INFO_FILE");
            modCreateErrorDialog.ExceptionInfo = null;
            modCreateErrorDialog.PopupCenteredShrink();
            return;
        }

        file.StoreString(modInfoText);
        file.Close();

        GD.Print("Mod folder created, trying to open: ", parsedData.Folder);
        FolderHelpers.OpenFolder(parsedData.Folder);

        RefreshAvailableMods();
    }

    private void BackPressed()
    {
        if (applyChangesButton.Disabled)
        {
            GUICommon.Instance.PlayButtonPressSound();
            EmitSignal(nameof(OnClosed));
        }
        else
        {
            unAppliedChangesWarning.PopupCenteredShrink();
        }
    }

    private void ConfirmBackWithUnAppliedChanges()
    {
        EmitSignal(nameof(OnClosed));
    }
}
