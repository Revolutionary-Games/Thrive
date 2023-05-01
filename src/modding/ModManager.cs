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
    public NodePath? LeftArrowPath;

    [Export]
    public NodePath RightArrowPath = null!;

    [Export]
    public NodePath AvailableModsContainerPath = null!;

    [Export]
    public NodePath EnabledModsContainerPath = null!;

    [Export]
    public NodePath OpenModInfoButtonPath = null!;

    [Export]
    public NodePath OpenModUrlButtonPath = null!;

    [Export]
    public NodePath DisableAllModsButtonPath = null!;

    [Export]
    public NodePath EnableAllModsButtonPath = null!;

    [Export]
    public NodePath SelectedModInfoBoxPath = null!;

    [Export]
    public NodePath SelectedModNamePath = null!;

    [Export]
    public NodePath SelectedModIconPath = null!;

    [Export]
    public NodePath SelectedModPreviewImagesContainerPath = null!;

    [Export]
    public NodePath SelectedModGalleryContainerPath = null!;

    [Export]
    public NodePath SelectedModAuthorPath = null!;

    [Export]
    public NodePath SelectedModVersionPath = null!;

    [Export]
    public NodePath SelectedModRecommendedThriveVersionPath = null!;

    [Export]
    public NodePath SelectedModMinimumThriveVersionPath = null!;

    [Export]
    public NodePath SelectedModDescriptionPath = null!;

    [Export]
    public NodePath SelectedModFromWorkshopPath = null!;

    [Export]
    public NodePath ModErrorDialogPath = null!;

    [Export]
    public NodePath RestartRequiredPath = null!;

    [Export]
    public NodePath ApplyChangesButtonPath = null!;

    [Export]
    public NodePath GalleryLeftButtonPath = null!;

    [Export]
    public NodePath GalleryRightButtonPath = null!;

    [Export]
    public NodePath MoveModUpButtonPath = null!;

    [Export]
    public NodePath MoveModDownButtonPath = null!;

    [Export]
    public NodePath ResetButtonPath = null!;

    [Export]
    public NodePath DependencyButtonPath = null!;

    [Export]
    public NodePath RequiredModsButtonPath = null!;

    [Export]
    public NodePath IncompatibleButtonPath = null!;

    [Export]
    public NodePath LoadOrderButtonPath = null!;

    [Export]
    public NodePath CheckButtonPath = null!;

    [Export]
    public NodePath GalleryLabelPath = null!;

    [Export]
    public NodePath UnAppliedChangesWarningPath = null!;

    [Export]
    public NodePath ModFullInfoPopupPath = null!;

    [Export]
    public NodePath FullInfoNamePath = null!;

    [Export]
    public NodePath FullInfoInternalNamePath = null!;

    [Export]
    public NodePath FullInfoAuthorPath = null!;

    [Export]
    public NodePath FullInfoVersionPath = null!;

    [Export]
    public NodePath FullInfoDescriptionPath = null!;

    [Export]
    public NodePath FullInfoLongDescriptionPath = null!;

    [Export]
    public NodePath FullInfoFromWorkshopPath = null!;

    [Export]
    public NodePath FullInfoIconFilePath = null!;

    [Export]
    public NodePath FullInfoPreviewImagesFilePath = null!;

    [Export]
    public NodePath FullInfoInfoUrlPath = null!;

    [Export]
    public NodePath FullInfoLicensePath = null!;

    [Export]
    public NodePath FullInfoRecommendedThrivePath = null!;

    [Export]
    public NodePath FullInfoMinimumThrivePath = null!;

    [Export]
    public NodePath FullInfoMaximumThrivePath = null!;

    [Export]
    public NodePath FullInfoPckNamePath = null!;

    [Export]
    public NodePath FullInfoModAssemblyPath = null!;

    [Export]
    public NodePath FullInfoAssemblyModClassPath = null!;

    [Export]
    public NodePath FullInfoDependenciesPath = null!;

    [Export]
    public NodePath FullInfoRequiredModsPath = null!;

    [Export]
    public NodePath FullInfoLoadBeforePath = null!;

    [Export]
    public NodePath FullInfoLoadAfterPath = null!;

    [Export]
    public NodePath FullInfoIncompatibleModsPath = null!;

    [Export]
    public NodePath FullInfoAutoHarmonyPath = null!;

    [Export]
    public NodePath OpenWorkshopButtonPath = null!;

    [Export]
    public NodePath ModUploaderButtonPath = null!;

    [Export]
    public NodePath NewModGUIPath = null!;

    [Export]
    public NodePath ModCreateErrorDialogPath = null!;

    [Export]
    public NodePath ModUploaderPath = null!;

    [Export]
    public NodePath SelectedModRecommendedThriveVersionContainerPath = null!;

    [Export]
    public NodePath SelectedModMinimumThriveVersionContainerPath = null!;

    [Export]
    public NodePath SelectedModThriveVersionContainerPath = null!;

    [Export]
    public NodePath SelectedModThriveVersionHSeparatorPath = null!;

    [Export]
    public NodePath ModCheckResultDialogPath = null!;

    [Export]
    public NodePath LoadWarningDialogPath = null!;

    [Export]
    public NodePath OtherModInfoDialogPath = null!;

    [Export]
    public NodePath ModErrorsContainerPath = null!;

    [Export]
    public NodePath ErrorInfoLabelPath = null!;

    [Export]
    public NodePath OneshotLoadingCheckboxPath = null!;

    [Export]
    public NodePath ModLoaderContainerPath = null!;

    private readonly List<FullModDetails> validMods = new();

    private readonly List<FullModDetails> notEnabledMods = new();
    private readonly List<FullModDetails> enabledMods = new();

#pragma warning disable CA2213
    private Button leftArrow = null!;
    private Button rightArrow = null!;

    private ItemList availableModsContainer = null!;
    private ItemList enabledModsContainer = null!;
    private ItemList modErrorsContainer = null!;

    private Label errorInfoLabel = null!;

    private Button openModInfoButton = null!;
    private Button openModUrlButton = null!;
    private Button disableAllModsButton = null!;
    private Button enableAllModsButton = null!;
    private Button galleryLeftButton = null!;
    private Button galleryRightButton = null!;
    private Button resetButton = null!;
    private Button moveModUpButton = null!;
    private Button dependencyButton = null!;
    private Button requiredModsButton = null!;
    private Button incompatibleButton = null!;
    private Button loadOrderButton = null!;
    private Button checkButton = null!;
    private Button moveModDownButton = null!;
    private CustomCheckBox oneshotLoadingCheckbox = null!;

    private Label galleryLabel = null!;
    private Label selectedModName = null!;
    private Label selectedFromWorkshop = null!;
    private TextureRect selectedModIcon = null!;
    private MarginContainer selectedModInfoBox = null!;
    private TabContainer selectedModPreviewImagesContainer = null!;
    private VBoxContainer selectedModGalleryContainer = null!;
    private VBoxContainer selectedModRecommendedThriveVersionContainer = null!;
    private VBoxContainer selectedModMinimumThriveVersionContainer = null!;
    private HBoxContainer selectedModThriveVersionContainer = null!;
    private Label selectedModAuthor = null!;
    private Label selectedModVersion = null!;
    private Label selectedModRecommendedThriveVersion = null!;
    private Label selectedModMinimumThriveVersion = null!;
    private CustomRichTextLabel selectedModDescription = null!;
    private HSeparator selectedModThriveVersionHSeparator = null!;

    private Button applyChangesButton = null!;

    private CustomDialog unAppliedChangesWarning = null!;
    private CustomConfirmationDialog modCheckResultDialog = null!;
    private CustomConfirmationDialog loadWarningDialog = null!;
    private CustomConfirmationDialog otherModInfoDialog = null!;

    private CustomDialog modFullInfoPopup = null!;
    private Label fullInfoName = null!;
    private Label fullInfoInternalName = null!;
    private Label fullInfoAuthor = null!;
    private Label fullInfoVersion = null!;
    private Label fullInfoDescription = null!;
    private CustomRichTextLabel fullInfoLongDescription = null!;
    private Label fullInfoFromWorkshop = null!;
    private Label fullInfoIconFile = null!;
    private Label fullInfoPreviewImagesFile = null!;
    private Label fullInfoInfoUrl = null!;
    private Label fullInfoLicense = null!;
    private Label fullInfoRecommendedThrive = null!;
    private Label fullInfoMinimumThrive = null!;
    private Label fullInfoMaximumThrive = null!;
    private Label fullInfoPckName = null!;
    private Label fullInfoModAssembly = null!;
    private Label fullInfoAssemblyModClass = null!;
    private Label fullInfoDependencies = null!;
    private Label fullInfoRequiredMods = null!;
    private Label fullInfoLoadBefore = null!;
    private Label fullInfoLoadAfter = null!;
    private Label fullInfoIncompatibleMods = null!;
    private Label fullInfoAutoHarmony = null!;

    private Button openWorkshopButton = null!;
    private Button modUploaderButton = null!;

    private TabContainer modLoaderContainer = null!;

    private NewModGUI newModGUI = null!;

    private ErrorDialog modCreateErrorDialog = null!;

    private ModUploader modUploader = null!;

    private ErrorDialog modErrorDialog = null!;

    private CustomDialog restartRequired = null!;
#pragma warning restore CA2213

    private FullModDetails? selectedMod;

    /// <summary>
    ///   Used to automatically refresh this object when it becomes visible after being invisible
    /// </summary>
    private bool wasVisible;

    [Signal]
    public delegate void OnClosed();

    /// <summary>
    ///   Loads mod info from a folder
    /// </summary>
    /// <param name="folder">Folder to load from</param>
    /// <returns>The info object if the info was valid, null otherwise</returns>
    public static ModInfo? LoadModInfo(string folder)
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
    ///   Loads the icon for a mod
    /// </summary>
    /// <param name="mod">Mod to load the icon for</param>
    /// <returns>The loaded icon or null if mod doesn't have icon set</returns>
    public static Texture? LoadModIcon(FullModDetails mod)
    {
        if (string.IsNullOrEmpty(mod.Info.Icon))
            return null;

        var image = new Image();
        image.Load(Path.Combine(mod.Folder, mod.Info.Icon!));

        var texture = new ImageTexture();
        texture.CreateFromImage(image);

        return texture;
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
    public static ModInfo? ParseModInfoString(string data, bool throwOnError)
    {
        ModInfo info;

        try
        {
            info = JsonSerializer.Create().Deserialize<ModInfo>(new JsonTextReader(new StringReader(data))) ??
                throw new JsonException("Deserialized mod info is null");
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
        if (!string.IsNullOrEmpty(info.Icon))
        {
            if (!IsAllowedModPath(info.Icon!))
            {
                if (throwOnError)
                {
                    throw new ArgumentException(TranslationServer.Translate("INVALID_ICON_PATH"));
                }

                GD.PrintErr("Invalid icon specified for mod: ", info.Icon);
                return false;
            }
        }

        if (info.InfoUrl != null)
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

        if (info.ModAssembly != null && info.AssemblyModClass == null && info.UseAutoHarmony != true)
        {
            if (throwOnError)
            {
                throw new ArgumentException(TranslationServer.Translate("ASSEMBLY_CLASS_REQUIRED"));
            }

            GD.PrintErr("AssemblyModClass must be set if ModAssembly is set (and auto harmony is not used)");
            return false;
        }

        if (info.UseAutoHarmony == true && string.IsNullOrEmpty(info.ModAssembly))
        {
            if (throwOnError)
            {
                throw new ArgumentException(TranslationServer.Translate("ASSEMBLY_REQUIRED_WITH_HARMONY"));
            }

            GD.PrintErr("ModAssembly must be set if UseAutoHarmony is true");
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
        modErrorsContainer = GetNode<ItemList>(ModErrorsContainerPath);

        errorInfoLabel = GetNode<Label>(ErrorInfoLabelPath);

        openModInfoButton = GetNode<Button>(OpenModInfoButtonPath);
        openModUrlButton = GetNode<Button>(OpenModUrlButtonPath);
        disableAllModsButton = GetNode<Button>(DisableAllModsButtonPath);
        enableAllModsButton = GetNode<Button>(EnableAllModsButtonPath);
        galleryLeftButton = GetNode<Button>(GalleryLeftButtonPath);
        galleryRightButton = GetNode<Button>(GalleryRightButtonPath);
        moveModUpButton = GetNode<Button>(MoveModUpButtonPath);
        moveModDownButton = GetNode<Button>(MoveModDownButtonPath);
        resetButton = GetNode<Button>(ResetButtonPath);
        dependencyButton = GetNode<Button>(DependencyButtonPath);
        requiredModsButton = GetNode<Button>(RequiredModsButtonPath);
        incompatibleButton = GetNode<Button>(IncompatibleButtonPath);
        loadOrderButton = GetNode<Button>(LoadOrderButtonPath);
        checkButton = GetNode<Button>(CheckButtonPath);
        oneshotLoadingCheckbox = GetNode<CustomCheckBox>(OneshotLoadingCheckboxPath);

        galleryLabel = GetNode<Label>(GalleryLabelPath);
        selectedModGalleryContainer = GetNode<VBoxContainer>(SelectedModGalleryContainerPath);
        selectedModName = GetNode<Label>(SelectedModNamePath);
        selectedFromWorkshop = GetNode<Label>(SelectedModFromWorkshopPath);
        selectedModIcon = GetNode<TextureRect>(SelectedModIconPath);
        selectedModPreviewImagesContainer = GetNode<TabContainer>(SelectedModPreviewImagesContainerPath);
        selectedModInfoBox = GetNode<MarginContainer>(SelectedModInfoBoxPath);
        selectedModAuthor = GetNode<Label>(SelectedModAuthorPath);
        selectedModVersion = GetNode<Label>(SelectedModVersionPath);
        selectedModRecommendedThriveVersion = GetNode<Label>(SelectedModRecommendedThriveVersionPath);
        selectedModMinimumThriveVersion = GetNode<Label>(SelectedModMinimumThriveVersionPath);
        selectedModDescription = GetNode<CustomRichTextLabel>(SelectedModDescriptionPath);
        selectedModThriveVersionHSeparator = GetNode<HSeparator>(SelectedModThriveVersionHSeparatorPath);

        selectedModRecommendedThriveVersionContainer =
            GetNode<VBoxContainer>(SelectedModRecommendedThriveVersionContainerPath);
        selectedModMinimumThriveVersionContainer = GetNode<VBoxContainer>(SelectedModMinimumThriveVersionContainerPath);
        selectedModThriveVersionContainer = GetNode<HBoxContainer>(SelectedModThriveVersionContainerPath);

        applyChangesButton = GetNode<Button>(ApplyChangesButtonPath);

        unAppliedChangesWarning = GetNode<CustomDialog>(UnAppliedChangesWarningPath);
        modCheckResultDialog = GetNode<CustomConfirmationDialog>(ModCheckResultDialogPath);
        loadWarningDialog = GetNode<CustomConfirmationDialog>(LoadWarningDialogPath);
        otherModInfoDialog = GetNode<CustomConfirmationDialog>(OtherModInfoDialogPath);

        modFullInfoPopup = GetNode<CustomDialog>(ModFullInfoPopupPath);
        fullInfoName = GetNode<Label>(FullInfoNamePath);
        fullInfoInternalName = GetNode<Label>(FullInfoInternalNamePath);
        fullInfoAuthor = GetNode<Label>(FullInfoAuthorPath);
        fullInfoVersion = GetNode<Label>(FullInfoVersionPath);
        fullInfoDescription = GetNode<Label>(FullInfoDescriptionPath);
        fullInfoLongDescription = GetNode<CustomRichTextLabel>(FullInfoLongDescriptionPath);
        fullInfoIconFile = GetNode<Label>(FullInfoIconFilePath);
        fullInfoPreviewImagesFile = GetNode<Label>(FullInfoPreviewImagesFilePath);
        fullInfoFromWorkshop = GetNode<Label>(FullInfoFromWorkshopPath);
        fullInfoInfoUrl = GetNode<Label>(FullInfoInfoUrlPath);
        fullInfoLicense = GetNode<Label>(FullInfoLicensePath);
        fullInfoRecommendedThrive = GetNode<Label>(FullInfoRecommendedThrivePath);
        fullInfoMinimumThrive = GetNode<Label>(FullInfoMinimumThrivePath);
        fullInfoMaximumThrive = GetNode<Label>(FullInfoMaximumThrivePath);
        fullInfoPckName = GetNode<Label>(FullInfoPckNamePath);
        fullInfoModAssembly = GetNode<Label>(FullInfoModAssemblyPath);
        fullInfoAssemblyModClass = GetNode<Label>(FullInfoAssemblyModClassPath);
        fullInfoDependencies = GetNode<Label>(FullInfoDependenciesPath);
        fullInfoRequiredMods = GetNode<Label>(FullInfoRequiredModsPath);
        fullInfoLoadBefore = GetNode<Label>(FullInfoLoadBeforePath);
        fullInfoLoadAfter = GetNode<Label>(FullInfoLoadAfterPath);
        fullInfoIncompatibleMods = GetNode<Label>(FullInfoIncompatibleModsPath);
        fullInfoAutoHarmony = GetNode<Label>(FullInfoAutoHarmonyPath);

        modLoaderContainer = GetNode<TabContainer>(ModLoaderContainerPath);

        openWorkshopButton = GetNode<Button>(OpenWorkshopButtonPath);
        modUploaderButton = GetNode<Button>(ModUploaderButtonPath);

        newModGUI = GetNode<NewModGUI>(NewModGUIPath);
        modUploader = GetNode<ModUploader>(ModUploaderPath);

        modErrorDialog = GetNode<ErrorDialog>(ModErrorDialogPath);
        restartRequired = GetNode<CustomDialog>(RestartRequiredPath);

        // These are hidden in the editor to make selecting UI elements there easier
        newModGUI.Visible = true;
        modUploader.Visible = true;

        modCreateErrorDialog = GetNode<ErrorDialog>(ModCreateErrorDialogPath);

        UpdateSelectedModInfo();
        UpdateTabTitles();

        if (!SteamHandler.Instance.IsLoaded)
        {
            openWorkshopButton.Visible = false;
            modUploaderButton.Visible = false;
        }
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        bool isCurrentlyVisible = IsVisibleInTree();

        if (isCurrentlyVisible && !wasVisible)
        {
            GD.Print("Mod loader has become visible");
            OnOpened();
        }

        wasVisible = isCurrentlyVisible;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged)
        {
            UpdateTabTitles();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (LeftArrowPath != null)
            {
                LeftArrowPath.Dispose();
                RightArrowPath.Dispose();
                AvailableModsContainerPath.Dispose();
                EnabledModsContainerPath.Dispose();
                OpenModInfoButtonPath.Dispose();
                OpenModUrlButtonPath.Dispose();
                DisableAllModsButtonPath.Dispose();
                SelectedModNamePath.Dispose();
                SelectedModIconPath.Dispose();
                SelectedModAuthorPath.Dispose();
                SelectedModVersionPath.Dispose();
                SelectedModRecommendedThriveVersionPath.Dispose();
                SelectedModMinimumThriveVersionPath.Dispose();
                SelectedModDescriptionPath.Dispose();
                ApplyChangesButtonPath.Dispose();
                UnAppliedChangesWarningPath.Dispose();
                ModFullInfoPopupPath.Dispose();
                FullInfoNamePath.Dispose();
                FullInfoInternalNamePath.Dispose();
                FullInfoAuthorPath.Dispose();
                FullInfoVersionPath.Dispose();
                FullInfoDescriptionPath.Dispose();
                FullInfoLongDescriptionPath.Dispose();
                FullInfoFromWorkshopPath.Dispose();
                FullInfoIconFilePath.Dispose();
                FullInfoInfoUrlPath.Dispose();
                FullInfoLicensePath.Dispose();
                FullInfoRecommendedThrivePath.Dispose();
                FullInfoMinimumThrivePath.Dispose();
                FullInfoMaximumThrivePath.Dispose();
                FullInfoPckNamePath.Dispose();
                FullInfoModAssemblyPath.Dispose();
                FullInfoAssemblyModClassPath.Dispose();
                FullInfoAutoHarmonyPath.Dispose();
                OpenWorkshopButtonPath.Dispose();
                ModUploaderButtonPath.Dispose();
                NewModGUIPath.Dispose();
                ModCreateErrorDialogPath.Dispose();
                ModUploaderPath.Dispose();
                ModErrorDialogPath.Dispose();
                RestartRequiredPath.Dispose();
                GalleryLeftButtonPath.Dispose();
                MoveModUpButtonPath.Dispose();
                MoveModDownButtonPath.Dispose();
                ResetButtonPath.Dispose();
                DependencyButtonPath.Dispose();
                RequiredModsButtonPath.Dispose();
                IncompatibleButtonPath.Dispose();
                LoadOrderButtonPath.Dispose();
                CheckButtonPath.Dispose();
                GalleryLabelPath.Dispose();
                SelectedModRecommendedThriveVersionContainerPath.Dispose();
                SelectedModMinimumThriveVersionContainerPath.Dispose();
                SelectedModThriveVersionContainerPath.Dispose();
                SelectedModThriveVersionHSeparatorPath.Dispose();
                ModCheckResultDialogPath.Dispose();
                LoadWarningDialogPath.Dispose();
                OtherModInfoDialogPath.Dispose();
                ModErrorsContainerPath.Dispose();
                ErrorInfoLabelPath.Dispose();
                OneshotLoadingCheckboxPath.Dispose();
                ModLoaderContainerPath.Dispose();
                EnableAllModsButtonPath.Dispose();
                SelectedModInfoBoxPath.Dispose();
                SelectedModPreviewImagesContainerPath.Dispose();
                SelectedModGalleryContainerPath.Dispose();
                FullInfoDependenciesPath.Dispose();
                FullInfoPreviewImagesFilePath.Dispose();
                FullInfoRequiredModsPath.Dispose();
                FullInfoLoadBeforePath.Dispose();
                FullInfoLoadAfterPath.Dispose();
                FullInfoIncompatibleModsPath.Dispose();
                SelectedModFromWorkshopPath.Dispose();
                GalleryRightButtonPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private static bool IsAllowedModPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        if (path.Contains("//") || path.Contains("..") || path.StartsWith("/", StringComparison.Ordinal))
            return false;

        return true;
    }

    /// <summary>
    ///   Finds existing mod folders
    /// </summary>
    /// <returns>List of mod folders that contain mod files</returns>
    private static List<string> FindModFolders()
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
    ///   Loads info for valid mods
    /// </summary>
    /// <returns>The valid mod names and their info</returns>
    private static List<FullModDetails> LoadValidMods()
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

            var name = Path.GetFileName(modFolder);

            if (info.InternalName != name)
            {
                GD.PrintErr("Mod internal name (", info.InternalName, ") doesn't match name of folder (", name,
                    ")");
                continue;
            }

            result.Add(new FullModDetails(name, modFolder, info)
                { IsCompatibleVersion = info.GetVersionCompatibility() });
        }

        var previousLength = result.Count;

        result = result.Distinct().ToList();

        if (result.Count != previousLength)
        {
            GD.PrintErr("Multiple mods detected with the same name, only one of them is usable");
        }

        return result;
    }

    private void UpdateTabTitles()
    {
        // The tab title has to be set here as they are normally set by node name in TabContainer
        // Which means they won't be translated at all
        modLoaderContainer.SetTabTitle(0, TranslationServer.Translate("MOD_LOADER_TAB"));
        modLoaderContainer.SetTabTitle(1, TranslationServer.Translate("MOD_ERRORS_TAB"));
    }

    /// <summary>
    ///   Refreshes things that need refreshing when this is opened
    /// </summary>
    private void OnOpened()
    {
        // TODO: OnOpened being required to be called probably means that you can't directly run the mod manager
        // scene from Godot editor, probably needs to change to the approach to be to automatically call this each
        // time this becomes visible like the save manager

        RefreshAvailableMods();
        RefreshEnabledMods();

        if (ModLoader.Instance.ModErrors.Any())
        {
            RefreshModErrors();
        }

        availableModsContainer.UnselectAll();
        enabledModsContainer.UnselectAll();

        UpdateOverallModButtons();
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

        foreach (var workshopMod in ModLoader.LoadWorkshopModsList())
        {
            if (validMods.Any(m => m.InternalName == workshopMod.InternalName))
            {
                GD.Print("Hiding workshop mod \"", workshopMod.InternalName,
                    "\" as there's already an existing mod with that name");
                continue;
            }

            validMods.Add(workshopMod);
        }

        // Clear the mods the mod loader will use to make sure it doesn't use outdated workshop mod list
        ModLoader.Instance.OnNewWorkshopModsInstalled();

        notEnabledMods.Clear();
        notEnabledMods.AddRange(validMods.Where(m => !IsModEnabled(m)).Distinct());

        foreach (var mod in notEnabledMods)
        {
            availableModsContainer.AddItem(mod.InternalName, LoadModIcon(mod));
            SetModToolTip(availableModsContainer, mod);
        }

        // If we found new mod folders that happen to be enabled already, add the mods to that list
        var foundStillEnabledMods = validMods.Where(IsModEnabled);

        foreach (var newMod in foundStillEnabledMods.Where(
                     m => !enabledMods.Contains(m) && !notEnabledMods.Contains(m)))
        {
            enabledMods.Add(newMod);
            enabledModsContainer.AddItem(newMod.InternalName, LoadModIcon(newMod));
        }

        UpdateOverallModButtons();
    }

    private void RefreshModErrors()
    {
        if (modErrorsContainer.IsAnythingSelected())
        {
            selectedMod = null;
            UpdateSelectedModInfo();
        }

        modErrorsContainer.Clear();

        var modErrors = ModLoader.Instance.GetAndClearModErrors();

        int modErrorsIndex = 0;
        foreach (var error in modErrors)
        {
            if (error.ModDetails == null)
            {
                modErrorsContainer.AddItem(error.ModInternalName);
            }
            else
            {
                modErrorsContainer.AddItem(error.ModInternalName, LoadModIcon(error.ModDetails));
            }

            modErrorsContainer.SetItemMetadata(modErrorsIndex, error.ErrorMessage);
            ++modErrorsIndex;
        }
    }

    private void RefreshEnabledMods()
    {
        if (enabledModsContainer.IsAnythingSelected())
        {
            selectedMod = null;
            UpdateSelectedModInfo();
        }

        enabledModsContainer.Clear();

        enabledMods.Clear();
        enabledMods.AddRange(validMods.Where(IsModEnabled));

        foreach (var mod in enabledMods)
        {
            enabledModsContainer.AddItem(mod.InternalName, LoadModIcon(mod));
            SetModToolTip(enabledModsContainer, mod);
        }
    }

    private bool IsModEnabled(FullModDetails mod)
    {
        return Settings.Instance.EnabledMods.Value.Contains(mod.InternalName);
    }

    private void UpdateSelectedModInfo()
    {
        if (selectedMod != null)
        {
            selectedModInfoBox.Visible = true;

            selectedModName.Text = selectedMod.Info.Name;
            selectedModIcon.Texture = LoadModIcon(selectedMod);
            selectedModAuthor.Text = selectedMod.Info.Author;
            selectedModVersion.Text = selectedMod.Info.Version;
            selectedFromWorkshop.Text = selectedMod.Workshop ?
                TranslationServer.Translate("THIS_IS_WORKSHOP_MOD") :
                TranslationServer.Translate("THIS_IS_LOCAL_MOD");

            selectedModThriveVersionContainer.Visible = true;
            selectedModRecommendedThriveVersionContainer.Visible = true;
            selectedModRecommendedThriveVersion.Text = selectedMod.Info.RecommendedThriveVersion;
            selectedModMinimumThriveVersionContainer.Visible = true;
            selectedModThriveVersionHSeparator.Visible = true;
            selectedModMinimumThriveVersion.Text = selectedMod.Info.MinimumThriveVersion;

            selectedModDescription.ExtendedBbcode = selectedMod.Info.LongDescription ?? selectedMod.Info.Description;
            openModUrlButton.Disabled = selectedMod.Info.InfoUrl == null;

            if (selectedModPreviewImagesContainer.GetChildCount() > 0)
            {
                selectedModPreviewImagesContainer.QueueFreeChildren();
            }

            List<ImageTexture> loadedPreviewImages = LoadModPreviewImages(selectedMod);
            if (loadedPreviewImages.Count > 0)
            {
                selectedModGalleryContainer.Visible = true;
                foreach (var currentPreviewImage in loadedPreviewImages)
                {
                    var currentPreviewImageNode = new TextureRect
                    {
                        Texture = currentPreviewImage,
                        Expand = true,
                        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    };

                    selectedModPreviewImagesContainer.AddChild(currentPreviewImageNode);
                }

                selectedModPreviewImagesContainer.CurrentTab = 0;
                UpdateGalleryUI();
            }
            else
            {
                selectedModGalleryContainer.Visible = false;
            }

            openModInfoButton.Disabled = false;

            dependencyButton.Visible = selectedMod.Info.Dependencies is { Count: > 0 };
            requiredModsButton.Visible =
                selectedMod.Info.RequiredMods is { Count: > 0 };

            incompatibleButton.Visible =
                selectedMod.Info.IncompatibleMods is { Count: > 0 };
            loadOrderButton.Visible = selectedMod.Info.LoadBeforeThis != null || selectedMod.Info.LoadAfterThis != null;
        }
        else
        {
            selectedModInfoBox.Visible = false;

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
            moveModUpButton.Disabled = true;
            moveModDownButton.Disabled = true;
        }

        errorInfoLabel.Hide();
    }

    private void UpdateGalleryUI()
    {
        galleryLabel.Text = (selectedModPreviewImagesContainer.CurrentTab + 1) + TranslationServer.Translate("MOD_LOADER_GALLERY_DIVIDER") +
            selectedModPreviewImagesContainer.GetTabCount();
        galleryRightButton.Disabled = selectedModPreviewImagesContainer.CurrentTab >=
            selectedModPreviewImagesContainer.GetTabCount() - 1;
        galleryLeftButton.Disabled = selectedModPreviewImagesContainer.CurrentTab <= 0;
    }

    private List<ImageTexture> LoadModPreviewImages(FullModDetails mod)
    {
        var returnValue = new List<ImageTexture>();

        if (mod.Info.PreviewImages != null)
        {
            foreach (string currentImagePath in mod.Info.PreviewImages)
            {
                if (string.IsNullOrEmpty(currentImagePath))
                    return returnValue;

                var image = new Image();
                image.Load(Path.Combine(mod.Folder, currentImagePath));

                var texture = new ImageTexture();
                texture.CreateFromImage(image);

                returnValue.Add(texture);
            }
        }

        return returnValue;
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
        GUICommon.Instance.PlayButtonPressSound();

        if (selectedMod == null)
        {
            GD.PrintErr("No mod is selected");
            return;
        }

        if (ModIncludesCode(selectedMod.Info))
        {
            // TODO: show a warning popup that can be permanently dismissed
        }

        foreach (var index in availableModsContainer.GetSelectedItems())
        {
            MoveModInItemList(availableModsContainer, enabledModsContainer, index);
        }

        notEnabledMods.Remove(selectedMod);
        enabledMods.Add(selectedMod);

        OnModChangedLists();
    }

    private void DisableModPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (selectedMod == null)
        {
            GD.PrintErr("No mod is selected");
            return;
        }

        foreach (var index in enabledModsContainer.GetSelectedItems())
        {
            MoveModInItemList(enabledModsContainer, availableModsContainer, index);
        }

        enabledMods.Remove(selectedMod);
        notEnabledMods.Add(selectedMod);

        OnModChangedLists();
    }

    private void SetModToolTip(ItemList selectedContainer, FullModDetails info, int index = -1)
    {
        if (index <= -1)
        {
            index = selectedContainer.GetItemCount() - 1;
        }

        selectedContainer.SetItemTooltip(index,
            string.IsNullOrEmpty(info.Info.Description) ? info.InternalName : info.Info.Description);
    }

    private void DisableAllPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (enabledModsContainer.IsAnythingSelected())
        {
            selectedMod = null;
            UpdateSelectedModInfo();
        }

        while (enabledModsContainer.GetItemCount() > 0)
        {
            MoveModInItemList(enabledModsContainer, availableModsContainer);
        }

        notEnabledMods.AddRange(enabledMods);
        enabledMods.Clear();

        UpdateOverallModButtons();
    }

    private void MoveModInItemList(ItemList fromContainer, ItemList toContainer, int index = 0)
    {
        var icon = fromContainer.GetItemIcon(index);
        var text = fromContainer.GetItemText(index);
        var toolTip = fromContainer.GetItemTooltip(index);

        fromContainer.RemoveItem(index);

        toContainer.AddItem(text, icon);
        toContainer.SetItemTooltip(toContainer.GetItemCount() - 1, toolTip);
    }

    private void EnableAllPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (availableModsContainer.IsAnythingSelected())
        {
            selectedMod = null;
            UpdateSelectedModInfo();
        }

        while (availableModsContainer.GetItemCount() > 0)
        {
            MoveModInItemList(availableModsContainer, enabledModsContainer);
        }

        enabledMods.AddRange(notEnabledMods);
        notEnabledMods.Clear();

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
        var isNotChanged = Settings.Instance.EnabledMods.Value.ToList()
            .SequenceEqual(enabledMods.Select(m => m.InternalName));
        applyChangesButton.Disabled = isNotChanged;
        resetButton.Disabled = isNotChanged;
        
        var isEnabledModsEmpty = enabledMods.Count < 1;
        checkButton.Disabled = isEnabledModsEmpty;
        disableAllModsButton.Disabled = isEnabledModsEmpty;

        disableAllModsButton.Disabled = enabledMods.Count < 1;
        enableAllModsButton.Disabled = notEnabledMods.Count < 1;
    }

    private void ApplyChanges()
    {
        var checkResult = ModLoader.IsValidModList(enabledMods);
        if (checkResult.ErrorType >= 0)
        {
            LoadEnabledMods();
        }
        else
        {
            var warningText = new LocalizedString("MOD_LOAD_ERROR_WARNING",
                ModHelpers.CheckResultToString(checkResult));

            loadWarningDialog.DialogText = warningText.ToString();
            loadWarningDialog.PopupCenteredShrink();
        }
    }

    private void LoadEnabledMods()
    {
        GD.Print("Applying changes to enabled mods");

        Settings.Instance.EnabledMods.Value = enabledMods.Select(m => m.InternalName).ToList();

        var modLoader = ModLoader.Instance;
        modLoader.LoadMods();

        if (modLoader.ModErrors.Any())
        {
            modErrorDialog.ExceptionInfo = string.Join("\n", modLoader.ModErrors.Select(e => e.ErrorMessage));
            modErrorDialog.PopupCenteredShrink();
        }

        if (modLoader.RequiresRestart)
        {
            restartRequired.PopupCenteredShrink();
        }

        if (!oneshotLoadingCheckbox.Pressed)
        {
            GD.Print("Saving settings with new mod list");
            if (!Settings.Instance.Save())
            {
                GD.PrintErr("Failed to save settings");
            }
        }

        RefreshModErrors();
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

        UpdateModLoaderContainerUI(availableModsContainer, false);
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

        UpdateModLoaderContainerUI(enabledModsContainer, true);
    }

    private void UpdateModLoaderContainerUI(ItemList activeContainer, bool isRightSide)
    {
        leftArrow.Disabled = !isRightSide;
        rightArrow.Disabled = isRightSide;
        modErrorsContainer.UnselectAll();
        errorInfoLabel.Hide();
        moveModUpButton.Disabled = activeContainer.IsSelected(0);
        moveModDownButton.Disabled = activeContainer.IsSelected(activeContainer.GetItemCount() - 1);
    }

    private void ErrorModItemListSelected(int index)
    {
        var newName = modErrorsContainer.GetItemText(index);
        var newItem = validMods.FirstOrDefault(m => m.InternalName == newName);

        if (!Equals(selectedMod, newItem))
        {
            selectedMod = newItem;
            UpdateSelectedModInfo();
        }

        if (availableModsContainer.IsAnythingSelected())
            availableModsContainer.UnselectAll();

        if (enabledModsContainer.IsAnythingSelected())
            enabledModsContainer.UnselectAll();

        leftArrow.Disabled = true;
        rightArrow.Disabled = true;
        errorInfoLabel.Text = (string)modErrorsContainer.GetItemMetadata(index);
        errorInfoLabel.Show();
        moveModUpButton.Disabled = true;
        moveModDownButton.Disabled = true;
    }

    private void OpenInfoUrlPressed()
    {
        if (selectedMod?.Info.InfoUrl == null)
        {
            GD.PrintErr("No mod is selected or it has no info url");
            return;
        }

        if (OS.ShellOpen(selectedMod.Info.InfoUrl.ToString()) != Error.Ok)
        {
            GD.PrintErr("Failed to open mod URL: ", selectedMod.Info.InfoUrl);
        }
    }

    private void OnDependencyPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        if (selectedMod == null)
        {
            return;
        }

        OpenOtherModInfoDialog(
            TranslationServer.Translate("MOD_DEPENDENCIES"),
            TranslationServer.Translate("NO_MOD_DEPENDENCIES"),
            selectedMod.Info.Dependencies ?? new List<string>());
    }

    private void OpenOtherModInfoDialog(string title, string emptyListMessage, List<string> shownItems)
    {
        otherModInfoDialog.WindowTitle = title;
        var infoText = new LocalizedStringBuilder();

        if (shownItems.Count <= 0)
        {
            foreach (string currentItem in shownItems)
            {
                if (!string.IsNullOrWhiteSpace(currentItem))
                {
                    infoText.Append(System.Environment.NewLine);
                    infoText.Append(new LocalizedString("FORMATTED_LIST_ITEM", currentItem));
                }
            }
        }
        else
        {
            infoText.Append(emptyListMessage);
        }

        otherModInfoDialog.DialogText = infoText.ToString();
        otherModInfoDialog.PopupCenteredShrink();
    }

    private void OnRequiredModsPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        if (selectedMod == null)
        {
            return;
        }

        OpenOtherModInfoDialog(
            TranslationServer.Translate("MOD_REQUIRED_MODS"),
            TranslationServer.Translate("NO_REQUIRED_MODS"),
            selectedMod.Info.RequiredMods ?? new List<string>());
    }

    private void OnIncompatiblePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        if (selectedMod == null)
        {
            return;
        }

        OpenOtherModInfoDialog(
            TranslationServer.Translate("INCOMPATIBLE_WITH"),
            TranslationServer.Translate("NO_MOD_INCOMPATIBLE"),
            selectedMod.Info.IncompatibleMods ?? new List<string>());
    }

    private void OnLoadOrderPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        if (selectedMod == null)
        {
            return;
        }

        otherModInfoDialog.WindowTitle = TranslationServer.Translate("LOAD_ORDER");
        var infoText = new LocalizedStringBuilder();

        var currentModLoadBefore = selectedMod.Info.LoadBeforeThis;
        var currentModLoadAfter = selectedMod.Info.LoadAfterThis;
        if (currentModLoadBefore != null || currentModLoadAfter != null)
        {
            if (currentModLoadAfter != null)
            {
                infoText.Append(new LocalizedString("MOD_LOAD_AFTER"));
                foreach (string currentLoadAfterMod in currentModLoadAfter)
                {
                    if (!string.IsNullOrWhiteSpace(currentLoadAfterMod))
                    {
                        infoText.Append(System.Environment.NewLine);
                        infoText.Append(new LocalizedString("FORMATTED_LIST_ITEM", currentLoadAfterMod));
                    }
                }
            }

            // If there both 'load before' and 'load after' is going to display add a empty line between them
            if (currentModLoadBefore != null && currentModLoadAfter != null)
            {
                infoText.Append("\n");
            }

            if (currentModLoadBefore != null)
            {
                infoText.Append(new LocalizedString("MOD_LOAD_BEFORE"));
                foreach (string currentLoadBeforeMod in currentModLoadBefore)
                {
                    if (!string.IsNullOrWhiteSpace(currentLoadBeforeMod))
                    {
                        infoText.Append(System.Environment.NewLine);
                        infoText.Append(new LocalizedString("FORMATTED_LIST_ITEM", currentLoadBeforeMod));
                    }
                }
            }
        }
        else
        {
            infoText.Append(new LocalizedString("MOD_NO_LOAD_ORDER"));
        }

        otherModInfoDialog.DialogText = infoText.ToString();
        otherModInfoDialog.PopupCenteredShrink();
    }

    private void GalleryArrowPressed(int movementAmount)
    {
        GUICommon.Instance.PlayButtonPressSound();
        selectedModPreviewImagesContainer.CurrentTab += movementAmount;
        UpdateGalleryUI();
    }

    private void MoveButtonPressed(bool moveUp, int amount)
    {
        GUICommon.Instance.PlayButtonPressSound();

        ItemList chosenList;
        List<FullModDetails> chosenModList;

        if (availableModsContainer.IsAnythingSelected())
        {
            chosenList = availableModsContainer;
            chosenModList = notEnabledMods;
        }
        else if (enabledModsContainer.IsAnythingSelected())
        {
            chosenList = enabledModsContainer;
            chosenModList = enabledMods;
        }
        else
        {
            return;
        }

        MoveModInItemList(chosenList, chosenModList, moveUp, chosenList.GetSelectedItems()[0], amount);

        var currentIndex = chosenList.GetSelectedItems()[0];

        moveModUpButton.Disabled = currentIndex == 0;
        moveModDownButton.Disabled = currentIndex >= chosenList.GetItemCount() - amount;

        UpdateOverallModButtons();
    }

    /// <summary>
    ///   Handles the movement of the Mod in the ItemList by any amount
    /// </summary>
    private void MoveModInItemList(ItemList list, List<FullModDetails> modList, bool moveUp, int currentIndex, int amount)
    {
        int newIndex;
        if (moveUp)
        {
            newIndex = currentIndex - amount;
            if (currentIndex == 0 || newIndex < 0)
            {
                return;
            }

            list.MoveItem(currentIndex, newIndex);
        }
        else
        {
            newIndex = currentIndex + amount;
            if (currentIndex == list.GetItemCount() - amount)
            {
                return;
            }

            list.MoveItem(currentIndex, newIndex);
        }

        var movedMod = modList[currentIndex];
        modList.RemoveAt(currentIndex);
        modList.Insert(newIndex, movedMod);
    }

    private void ResetPressed()
    {
        availableModsContainer.UnselectAll();
        enabledModsContainer.UnselectAll();

        RefreshAvailableMods();
        RefreshEnabledMods();

        UpdateOverallModButtons();
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
        fullInfoInternalName.Text = info.InternalName;
        fullInfoAuthor.Text = info.Author;
        fullInfoVersion.Text = info.Version;
        fullInfoDescription.Text = info.Description;
        fullInfoLongDescription.ExtendedBbcode = info.LongDescription;
        fullInfoFromWorkshop.Text = selectedMod!.Workshop ?
            TranslationServer.Translate("THIS_IS_WORKSHOP_MOD") :
            TranslationServer.Translate("THIS_IS_LOCAL_MOD");
        fullInfoIconFile.Text = info.Icon;
        fullInfoPreviewImagesFile.Text = info.PreviewImages?.FormatAsAList();
        fullInfoInfoUrl.Text = info.InfoUrl == null ? string.Empty : info.InfoUrl.ToString();
        fullInfoLicense.Text = info.License;
        fullInfoRecommendedThrive.Text = info.RecommendedThriveVersion;
        fullInfoMinimumThrive.Text = info.MinimumThriveVersion;
        fullInfoMaximumThrive.Text = info.MaximumThriveVersion;
        fullInfoPckName.Text = info.PckToLoad;
        fullInfoModAssembly.Text = info.ModAssembly;
        fullInfoAssemblyModClass.Text = info.AssemblyModClass;
        fullInfoDependencies.Text = info.Dependencies?.FormatAsAList();
        fullInfoRequiredMods.Text = info.RequiredMods?.FormatAsAList();
        fullInfoLoadBefore.Text = info.LoadBeforeThis?.FormatAsAList();
        fullInfoLoadAfter.Text = info.LoadAfterThis?.FormatAsAList();
        fullInfoIncompatibleMods.Text = info.IncompatibleMods?.FormatAsAList();

        fullInfoAutoHarmony.Text = info.UseAutoHarmony == true ?
            TranslationServer.Translate("USES_FEATURE") :
            TranslationServer.Translate("DOES_NOT_USE_FEATURE");

        modFullInfoPopup.PopupCenteredShrink();
    }

    private void CloseModInfoPopup()
    {
        GUICommon.Instance.PlayButtonPressSound();
        modFullInfoPopup.Hide();
    }

    private void OnCheckPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        // Checks if there is anything that is going to be loaded first
        if (enabledMods.Count <= 0)
        {
            return;
        }

        var checkResult = ModLoader.IsValidModList(enabledMods);
        var resultText = string.Empty;

        if (checkResult.IsSuccessful())
        {
            resultText = TranslationServer.Translate("MOD_LIST_CONTAIN_ERRORS") + "\n\n" +
                ModHelpers.CheckResultToString(checkResult) + "\n\n";
            resultText += TranslationServer.Translate("MOD_CHECK_AGAIN_WARNING");
        }
        else if (checkResult.ErrorType > 0)
        {
            resultText = TranslationServer.Translate("MOD_LIST_VALID");
        }

        modCheckResultDialog.DialogText = resultText;
        modCheckResultDialog.PopupCenteredShrink();
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
                    .Deserialize<FullModDetails>(new JsonTextReader(new StringReader(data))) ??
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

    private void OpenModUploader()
    {
        GUICommon.Instance.PlayButtonPressSound();

        // Don't allow uploading workshop mods again
        modUploader.Open(validMods.Where(m => !m.Workshop));
    }

    private void OpenWorkshopSite()
    {
        // TODO: once in-game mod downloads works this could open in the Steam overlay browser
        if (OS.ShellOpen("https://steamcommunity.com/app/1779200/workshop/") != Error.Ok)
        {
            GD.PrintErr("Failed to open workshop URL");
        }
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
