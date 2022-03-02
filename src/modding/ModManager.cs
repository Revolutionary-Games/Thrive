﻿using System;
using System.Collections.Generic;
using System.Globalization;
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
    public NodePath LeftArrowPath = null!;

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
    public NodePath FullInfoModConfigPath = null!;

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
    public NodePath ConfigItemListPath = null!;

    [Export]
    public NodePath ConfigContainerPath = null!;

    [Export]
    public NodePath ConfigPanelContainerPath = null!;

    [Export]
    public NodePath ModLoaderContainerPath = null!;

    [Export]
    public PackedScene ConfigItemScene = null!;

    private readonly List<FullModDetails> validMods = new();

    private List<FullModDetails>? notEnabledMods;
    private List<FullModDetails>? enabledMods;

    private Button leftArrow = null!;
    private Button rightArrow = null!;

    private ItemList availableModsContainer = null!;
    private ItemList enabledModsContainer = null!;
    private ItemList modErrorsContainer = null!;
    private ItemList configModContainer = null!;

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
    private Button oneshotLoadingCheckbox = null!;

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
    private Label fullInfoLongDescription = null!;
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
    private Label fullInfoModConfig = null!;

    private Button openWorkshopButton = null!;
    private Button modUploaderButton = null!;

    private BoxContainer configContainer = null!;
    private MarginContainer configPanelContainer = null!;
    private TabContainer modLoaderContainer = null!;

    private NewModGUI newModGUI = null!;

    private ErrorDialog modCreateErrorDialog = null!;

    private ModUploader modUploader = null!;

    private ErrorDialog modErrorDialog = null!;

    private CustomDialog restartRequired = null!;

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
    ///   Tries to read the config file of a mod and returns it.
    /// </summary>
    /// <returns> Gets a array of ModConfigItemInfo from a ModInfo, null otherwise</returns>
    public static ModConfigItemInfo[] GetModConfigList(FullModDetails currentMod)
    {
        if (FileHelpers.Exists(Path.Combine(currentMod.Folder, currentMod.Info.ConfigToLoad ?? string.Empty)))
        {
            var infoFile = Path.Combine(currentMod.Folder, currentMod.Info.ConfigToLoad ?? string.Empty);

            using var file = new File();

            if (file.Open(infoFile, File.ModeFlags.Read) != Error.Ok)
            {
                GD.PrintErr("Can't read config info file at: ", infoFile);
                return null!;
            }

            return JsonConvert.DeserializeObject<ModConfigItemInfo[]>(file.GetAsText())!;
        }

        return null!;
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

        if (info.ModAssembly != null && info.AssemblyModClass == null)
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
        modErrorsContainer = GetNode<ItemList>(ModErrorsContainerPath);
        configModContainer = GetNode<ItemList>(ConfigItemListPath);

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
        oneshotLoadingCheckbox = GetNode<CheckBox>(OneshotLoadingCheckboxPath);

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
        fullInfoLongDescription = GetNode<Label>(FullInfoLongDescriptionPath);
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
        fullInfoModConfig = GetNode<Label>(FullInfoModConfigPath);

        modLoaderContainer = GetNode<TabContainer>(ModLoaderContainerPath);
        configContainer = GetNode<BoxContainer>(ConfigContainerPath);
        configPanelContainer = GetNode<MarginContainer>(ConfigPanelContainerPath);

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

        // The tab title has to be set here as they are normally set by node name in TabContainer
        // Which means they won't be translated at all
        modLoaderContainer.SetTabTitle(0, TranslationServer.Translate("MOD_LOADER_TAB"));
        modLoaderContainer.SetTabTitle(1, TranslationServer.Translate("MOD_ERRORS_TAB"));
        modLoaderContainer.SetTabTitle(2, TranslationServer.Translate("MOD_CONFIGURATION_TAB"));

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

    public void UpdateLoadPosition(int startIndex = 0)
    {
        if (enabledMods != null)
        {
            for (int index = startIndex; index < enabledMods.Count; ++index)
            {
                enabledMods[index].LoadPosition = index;
            }
        }
    }

    /// <summary>
    ///   Gets a List of FullModDetails of all the mods that have a config file
    /// </summary>
    /// <returns> A List of FullModDetails that are loaded that have a ConfigFile </returns>
    public List<FullModDetails> GetAllConfigurableMods()
    {
        var resultArray = new List<FullModDetails>();
        if (enabledMods != null)
        {
            foreach (FullModDetails currentMod in enabledMods)
            {
                if (currentMod.CurrentConfiguration != null)
                {
                    resultArray.Add(currentMod);
                }
            }
        }

        return resultArray;
    }

    /// <summary>
    ///   This saves the Mod Settings/Config to a file
    /// </summary>
    /// <returns> True on success, false if the file can't be written .</returns>
    /// <remarks>
    ///   This was based on the Save method from Settings.cs
    /// </remarks>
    public bool SaveAllModsSettings()
    {
        using var file = new File();
        var error = file.Open(Constants.MOD_CONFIGURATION_FILE, File.ModeFlags.Write);

        if (error != Error.Ok)
        {
            GD.PrintErr("Couldn't open mod configuration file for writing.");
            return false;
        }

        var modConfigList = GetAllConfigurableMods();
        var savedConfig = new Dictionary<string, Dictionary<string, object>>();
        foreach (FullModDetails currentMod in modConfigList)
        {
            savedConfig.Add(currentMod.InternalName,
                currentMod.CurrentConfiguration ?? new Dictionary<string, object>());
        }

        file.StoreString(JsonConvert.SerializeObject(savedConfig, Formatting.Indented));
        file.Close();

        return true;
    }

    /// <summary>
    ///   This loads the all of the Mod Settings/Config from a file
    /// </summary>
    /// <returns> The SavedConfig on success, null if the file can't be read.</returns>
    public Dictionary<string, Dictionary<string, object>> LoadAllModsSettings()
    {
        using var file = new File();
        var error = file.Open(Constants.MOD_CONFIGURATION_FILE, File.ModeFlags.Read);

        if (error != Error.Ok)
        {
            GD.PrintErr("Couldn't open mod configuration file for reading.");
            return null!;
        }

        var savedConfig =
            JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(file.GetAsText());
        file.Close();

        return savedConfig!;
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
    ///   Refreshes things that need refreshing when this is opened
    /// </summary>
    private void OnOpened()
    {
        // TODO: OnOpened being required to be called probably means that you can't directly run the mod manager
        // scene from Godot editor, probably needs to change to the approach to be to automatically call this each
        // time this becomes visible like the save manager
        enabledMods = new List<FullModDetails>();
        notEnabledMods = new List<FullModDetails>();

        RefreshAvailableMods();
        RefreshEnabledMods();
        RefreshConfigList();

        if (ModLoader.Instance.GetModErrors().Count > 0)
        {
            RefreshModErrors();
        }

        availableModsContainer.UnselectAll();
        enabledModsContainer.UnselectAll();

        UpdateOverallModButtons();

        UpdateLoadPosition();
    }

    private void RefreshAvailableMods()
    {
        if (notEnabledMods == null || enabledMods == null)
            throw new InvalidOperationException("The mod manager was not opened yet");

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

        notEnabledMods = validMods.Where(m => !IsModEnabled(m)).Concat(notEnabledMods.Where(validMods.Contains))
            .Distinct()
            .ToList();

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
        var index = 0;
        foreach (var currentError in modErrors)
        {
            var mod = currentError.Mod;
            modErrorsContainer.AddItem(mod.InternalName, LoadModIcon(mod));
            modErrorsContainer.SetItemMetadata(index, currentError.ErrorMessage);
            ++index;
        }
    }

    private void RefreshConfigList()
    {
        if (enabledMods is null)
        {
            return;
        }

        if (configModContainer.IsAnythingSelected())
        {
            selectedMod = null;
            UpdateSelectedModInfo();
        }

        configModContainer.Clear();

        var savedConfig = LoadAllModsSettings();
        if (savedConfig is null)
        {
            return;
        }

        // TODO: It should only load mods that are actually loaded but I am not sure how...
        foreach (var currentMod in enabledMods)
        {
            if (currentMod != null)
            {
                VerifyConfigFileExist(currentMod);
                if (currentMod.CurrentConfiguration != null)
                {
                    if (savedConfig.ContainsKey(currentMod.InternalName))
                    {
                        currentMod.CurrentConfiguration = savedConfig[currentMod.InternalName];
                    }

                    configModContainer.AddItem(currentMod.InternalName, LoadModIcon(currentMod));
                    SetModToolTip(configModContainer, currentMod);
                }
            }
        }
    }

    private void OnClearConfigButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        foreach (var currentItem in configContainer.GetChild(0).GetChildren())
        {
            var currentItemInfo = currentItem as ModConfigItemInfo;
            currentItemInfo?.UpdateUI();
        }
    }

    private void OnResetConfigButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        var configItemArray = configContainer.GetChild(0).GetChildren();

        int index = 0;
        foreach (ModConfigItemInfo currentItemInfo in configItemArray)
        {
            if (selectedMod != null && currentItemInfo != null)
            {
                VerifyConfigFileExist(selectedMod);
                if (selectedMod.ConfigurationInfoList != null)
                {
                    currentItemInfo.Value = selectedMod.ConfigurationInfoList[index].Value;
                    if (selectedMod?.CurrentConfiguration != null)
                    {
                        if (currentItemInfo.ID != null && currentItemInfo.Value != null)
                        {
                            selectedMod.CurrentConfiguration[currentItemInfo.ID] = currentItemInfo.Value;
                        }
                    }
                }

                currentItemInfo.UpdateUI();
            }

            ++index;
        }

        if (selectedMod != null)
        {
            SaveAllModsSettings();
        }
    }

    private void RefreshEnabledMods()
    {
        if (enabledMods is null)
        {
            return;
        }

        if (enabledModsContainer.IsAnythingSelected())
        {
            selectedMod = null;
            UpdateSelectedModInfo();
        }

        enabledModsContainer.Clear();

        enabledMods = validMods.Where(IsModEnabled).ToList();

        foreach (var mod in enabledMods)
        {
            if (mod != null)
            {
                enabledModsContainer.AddItem(mod.InternalName, LoadModIcon(mod));
                if (!string.IsNullOrEmpty(mod.Info.Description))
                {
                    SetModToolTip(enabledModsContainer, mod);
                }
            }
        }
    }

    private bool IsModEnabled(FullModDetails mod)
    {
        return Settings.Instance.EnabledMods.Value.Contains(mod.InternalName) || enabledMods!.Contains(mod);
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

            if (!(string.IsNullOrEmpty(selectedMod.Info.RecommendedThriveVersion) &&
                    string.IsNullOrEmpty(selectedMod.Info.MinimumThriveVersion)))
            {
                selectedModThriveVersionContainer.Visible = true;
                selectedModRecommendedThriveVersionContainer.Visible =
                    !string.IsNullOrEmpty(selectedMod.Info.RecommendedThriveVersion);
                selectedModMinimumThriveVersionContainer.Visible =
                    !string.IsNullOrEmpty(selectedMod.Info.MinimumThriveVersion);
                selectedModThriveVersionHSeparator.Visible = selectedModMinimumThriveVersionContainer.Visible &&
                    selectedModRecommendedThriveVersionContainer.Visible;
                selectedModRecommendedThriveVersion.Text = selectedMod.Info.RecommendedThriveVersion;
                selectedModMinimumThriveVersion.Text = selectedMod.Info.MinimumThriveVersion;
            }
            else
            {
                selectedModThriveVersionContainer.Visible = false;
            }

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
                foreach (ImageTexture currentPreviewImage in loadedPreviewImages)
                {
                    var currentPreviewImageNode = new TextureRect();
                    currentPreviewImageNode.Texture = currentPreviewImage;
                    currentPreviewImageNode.Expand = true;
                    currentPreviewImageNode.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;

                    selectedModPreviewImagesContainer.AddChild(currentPreviewImageNode);
                }

                selectedModPreviewImagesContainer.CurrentTab = 0;
                galleryLabel.Text = "1/" + selectedModPreviewImagesContainer.GetTabCount();
                galleryRightButton.Disabled = selectedModPreviewImagesContainer.CurrentTab >=
                    selectedModPreviewImagesContainer.GetTabCount() - 1;
                galleryLeftButton.Disabled = selectedModPreviewImagesContainer.CurrentTab <= 0;
            }
            else
            {
                selectedModGalleryContainer.Visible = false;
            }

            openModInfoButton.Disabled = false;

            dependencyButton.Visible = selectedMod.Info.Dependencies != null && selectedMod.Info.Dependencies.Count > 0;
            requiredModsButton.Visible =
                selectedMod.Info.RequiredMods != null && selectedMod.Info.RequiredMods.Count > 0;

            incompatibleButton.Visible =
                selectedMod.Info.IncompatibleMods != null && selectedMod.Info.IncompatibleMods.Count > 0;
            loadOrderButton.Visible = selectedMod.Info.LoadBefore != null || selectedMod.Info.LoadAfter != null;
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

    private List<ImageTexture> LoadModPreviewImages(FullModDetails mod)
    {
        if (mod.Info.PreviewImages == null)
            return new List<ImageTexture>();

        var returnValue = new List<ImageTexture>();
        foreach (string currentImagePath in mod.Info.PreviewImages)
        {
            if (string.IsNullOrEmpty(currentImagePath))
                return null!;

            var image = new Image();
            image.Load(Path.Combine(mod.Folder, currentImagePath));

            var texture = new ImageTexture();
            texture.CreateFromImage(image);

            returnValue.Add(texture);
        }

        return returnValue;
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

            var name = Path.GetFileName(modFolder);

            if (info.InternalName != name)
            {
                GD.PrintErr("Mod internal name (", info.InternalName, ") doesn't match name of folder (", name,
                    ")");
                continue;
            }

            result.Add(new FullModDetails(name, modFolder, info) { IsCompatibleVersion = ModHelpers.GetVersionCompatibility(info) });
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

        Texture? icon = null;

        foreach (var index in availableModsContainer.GetSelectedItems())
        {
            icon = availableModsContainer.GetItemIcon(index);
            availableModsContainer.RemoveItem(index);
        }

        enabledModsContainer.AddItem(selectedMod.InternalName, icon);
        SetSelectedModToolTip(enabledModsContainer);

        notEnabledMods!.Remove(selectedMod);
        enabledMods!.Add(selectedMod);

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

        Texture? icon = null;

        foreach (var index in enabledModsContainer.GetSelectedItems())
        {
            icon = enabledModsContainer.GetItemIcon(index);
            enabledModsContainer.RemoveItem(index);
        }

        availableModsContainer.AddItem(selectedMod.InternalName, icon);
        SetSelectedModToolTip(availableModsContainer);

        enabledMods!.Remove(selectedMod);
        notEnabledMods!.Add(selectedMod);

        OnModChangedLists();
    }

    private void SetSelectedModToolTip(ItemList selectedContainer)
    {
        selectedContainer.SetItemTooltip(selectedContainer.GetItemCount() - 1,
                string.IsNullOrEmpty(selectedMod?.Info.Description) ?
                    selectedMod?.InternalName :
                    selectedMod?.Info.Description);
    }

    private void SetModToolTip(ItemList selectedContainer, FullModDetails info)
    {
        selectedContainer.SetItemTooltip(selectedContainer.GetItemCount() - 1,
                string.IsNullOrEmpty(info.Info.Description) ?
                    info.InternalName :
                    info.Info.Description);
    }

    private void DisableAllPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        if (notEnabledMods == null || enabledMods == null)
        {
            GD.PrintErr("Can't disable all as the mod manager was not opened yet");
            return;
        }

        if (enabledModsContainer.IsAnythingSelected())
        {
            selectedMod = null;
            UpdateSelectedModInfo();
        }

        while (enabledModsContainer.GetItemCount() > 0)
        {
            var icon = enabledModsContainer.GetItemIcon(0);
            var text = enabledModsContainer.GetItemText(0);
            var toolTip = enabledModsContainer.GetItemTooltip(0);

            enabledModsContainer.RemoveItem(0);

            availableModsContainer.AddItem(text, icon);
            availableModsContainer.SetItemTooltip(availableModsContainer.GetItemCount() - 1, toolTip);
        }

        notEnabledMods.AddRange(enabledMods);
        enabledMods.Clear();

        UpdateOverallModButtons();
    }

    private void EnableAllPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        if (enabledMods is null || notEnabledMods is null)
        {
            return;
        }

        if (availableModsContainer.IsAnythingSelected())
        {
            selectedMod = null;
            UpdateSelectedModInfo();
        }

        while (availableModsContainer.GetItemCount() > 0)
        {
            var icon = availableModsContainer.GetItemIcon(0);
            var text = availableModsContainer.GetItemText(0);
            var toolTip = availableModsContainer.GetItemTooltip(0);
            availableModsContainer.RemoveItem(0);

            enabledModsContainer.AddItem(text, icon);
            enabledModsContainer.SetItemTooltip(enabledModsContainer.GetItemCount() - 1, toolTip);
        }

        enabledMods.AddRange(notEnabledMods);
        notEnabledMods.Clear();

        UpdateLoadPosition();
        UpdateOverallModButtons();
    }

    private void OnModChangedLists()
    {
        selectedMod = null;
        UpdateSelectedModInfo();
        enabledModsContainer.UnselectAll();
        availableModsContainer.UnselectAll();

        UpdateLoadPosition();
        UpdateOverallModButtons();
    }

    private void UpdateOverallModButtons()
    {
        if (enabledMods is null || notEnabledMods is null)
        {
            return;
        }

        applyChangesButton.Disabled =
            Settings.Instance.EnabledMods.Value.ToList()
                .SequenceEqual(enabledMods!.Select(m => m.InternalName));

        var isEnabledModsEmpty = enabledMods.Count < 1;
        resetButton.Disabled = isEnabledModsEmpty;
        checkButton.Disabled = isEnabledModsEmpty;
        disableAllModsButton.Disabled = isEnabledModsEmpty;

        disableAllModsButton.Disabled = enabledMods!.Count < 1;
        enableAllModsButton.Disabled = notEnabledMods!.Count < 1;
    }

    private void ApplyChanges()
    {
        if (notEnabledMods == null || enabledMods == null)
        {
            GD.PrintErr("Can't apply changes as the mod manager was not opened yet");
            return;
        }

        var checkResult = ModLoader.IsValidModList(enabledMods);
        if (checkResult.ErrorType >= 0)
        {
            LoadEnabledMods();
        }
        else
        {
            var warningText = new LocalizedString("MOD_LOAD_ERROR_WARNING", "\n\n", ModHelpers.CheckResultToString(checkResult, enabledMods), "\n\n", "ARE_YOU_SURE_TO_LOAD_MOD");

            loadWarningDialog.DialogText = warningText.ToString();
            loadWarningDialog.PopupCenteredShrink();
        }
    }

    private void LoadEnabledMods()
    {
        if (notEnabledMods == null || enabledMods == null)
        {
            return;
        }

        GD.Print("Applying changes to enabled mods");

        Settings.Instance.EnabledMods.Value = enabledMods?.Select(m => m.InternalName).ToList() ?? new List<string>();

        var modLoader = ModLoader.Instance;
        modLoader.LoadMods();

        var errors = modLoader.GetModErrors();

        if (errors.Count > 0)
        {
            modErrorDialog.ExceptionInfo = string.Join("\n", errors.Select(e => e.ErrorMessage));
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

        RefreshConfigList();
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

        leftArrow.Disabled = true;
        rightArrow.Disabled = false;
        modErrorsContainer.UnselectAll();
        configModContainer.UnselectAll();
        errorInfoLabel.Hide();
        configPanelContainer.Visible = false;
        moveModUpButton.Disabled = availableModsContainer.IsSelected(0);
        moveModDownButton.Disabled = availableModsContainer.IsSelected(availableModsContainer.GetItemCount() - 1);
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

        leftArrow.Disabled = false;
        rightArrow.Disabled = true;
        modErrorsContainer.UnselectAll();
        configModContainer.UnselectAll();
        errorInfoLabel.Hide();
        configPanelContainer.Visible = false;
        moveModUpButton.Disabled = enabledModsContainer.IsSelected(0);
        moveModDownButton.Disabled = enabledModsContainer.IsSelected(enabledModsContainer.GetItemCount() - 1);
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
        configModContainer.UnselectAll();
        errorInfoLabel.Text = (string)modErrorsContainer.GetItemMetadata(index);
        errorInfoLabel.Show();
        configPanelContainer.Visible = false;
        moveModUpButton.Disabled = true;
        moveModDownButton.Disabled = true;
    }

    private void ModConfigModSelected(int index)
    {
        var newName = enabledModsContainer.GetItemText(index);
        var newItem = validMods.FirstOrDefault(m => m.InternalName == newName);
        if (newItem == null)
        {
            return;
        }

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
        modErrorsContainer.UnselectAll();
        errorInfoLabel.Hide();
        moveModUpButton.Disabled = true;
        moveModDownButton.Disabled = true;

        VerifyConfigFileExist(newItem);

        if (newItem.ConfigurationInfoList?.Length > 0)
        {
            configPanelContainer.Visible = true;
            if (configContainer.GetChildCount() > 0)
            {
                configContainer.RemoveChild(configContainer.GetChild(0));
            }

            if (newItem.ConfigNodes == null)
            {
                newItem.ConfigNodes = ConfigMenuSetup(newItem.ConfigurationInfoList,
                    newItem.CurrentConfiguration ?? new Dictionary<string, object>());
            }
            else
            {
                configContainer.AddChild(newItem.ConfigNodes);
            }
        }
        else
        {
            configPanelContainer.Visible = false;
        }
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
        if (selectedMod is null)
        {
            return;
        }

        otherModInfoDialog.WindowTitle = TranslationServer.Translate("MOD_DEPENDENCIES");
        var infoText = string.Empty;

        var currentModDependencies = selectedMod.Info.Dependencies;
        if (currentModDependencies != null)
        {
            foreach (string currentDependency in currentModDependencies)
            {
                if (!string.IsNullOrWhiteSpace(currentDependency))
                {
                    infoText += "* " + currentDependency + "\n";
                }
            }
        }
        else
        {
            infoText += TranslationServer.Translate("NO_MOD_DEPENDENCIES");
        }

        otherModInfoDialog.DialogText = infoText;
        otherModInfoDialog.PopupCenteredShrink();
    }

    private void OnRequiredModsPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        if (selectedMod is null)
        {
            return;
        }

        otherModInfoDialog.WindowTitle = TranslationServer.Translate("MOD_REQUIRED_MODS");
        var infoText = string.Empty;

        var currentModRequiredMods = selectedMod.Info.RequiredMods;
        if (currentModRequiredMods != null)
        {
            foreach (string currentRequiredMod in currentModRequiredMods)
            {
                if (!string.IsNullOrWhiteSpace(currentRequiredMod))
                {
                    infoText += "* " + currentRequiredMod + "\n";
                }
            }
        }
        else
        {
            infoText += TranslationServer.Translate("NO_REQUIRED_MODS");
        }

        otherModInfoDialog.DialogText = infoText;
        otherModInfoDialog.PopupCenteredShrink();
    }

    private void OnIncompatiblePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        if (selectedMod is null)
        {
            return;
        }

        otherModInfoDialog.WindowTitle = TranslationServer.Translate("INCOMPATIBLE_WITH");
        var infoText = string.Empty;

        var currentModIncompatibleMods = selectedMod.Info.IncompatibleMods;
        if (currentModIncompatibleMods != null)
        {
            foreach (string currentIncompatibleMod in currentModIncompatibleMods)
            {
                if (!string.IsNullOrWhiteSpace(currentIncompatibleMod))
                {
                    infoText += "* " + currentIncompatibleMod + "\n";
                }
            }
        }
        else
        {
            infoText += TranslationServer.Translate("NO_MOD_INCOMPATIBLE");
        }

        otherModInfoDialog.DialogText = infoText;
        otherModInfoDialog.PopupCenteredShrink();
    }

    private void OnLoadOrderPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        if (selectedMod is null)
        {
            return;
        }

        otherModInfoDialog.WindowTitle = TranslationServer.Translate("LOAD_ORDER");
        var infoText = string.Empty;

        var currentModLoadBefore = selectedMod.Info.LoadBefore;
        var currentModLoadAfter = selectedMod.Info.LoadAfter;
        if (currentModLoadBefore != null || currentModLoadAfter != null)
        {
            if (currentModLoadAfter != null)
            {
                infoText += TranslationServer.Translate("MOD_LOAD_AFTER") + "\n";
                foreach (string currentLoadAfterMod in currentModLoadAfter)
                {
                    if (!string.IsNullOrWhiteSpace(currentLoadAfterMod))
                    {
                        infoText += "* " + currentLoadAfterMod + "\n";
                    }
                }
            }

            // If there both 'load before' and 'load after' is going to display add a empty line between them
            if (currentModLoadBefore != null && currentModLoadAfter != null)
            {
                infoText += "\n";
            }

            if (currentModLoadBefore != null)
            {
                infoText += TranslationServer.Translate("MOD_LOAD_BEFORE") + "\n";
                foreach (string currentLoadBeforeMod in currentModLoadBefore)
                {
                    if (!string.IsNullOrWhiteSpace(currentLoadBeforeMod))
                    {
                        infoText += "* " + currentLoadBeforeMod + "\n";
                    }
                }
            }
        }
        else
        {
            infoText += TranslationServer.Translate("MOD_NO_LOAD_ORDER");
        }

        otherModInfoDialog.DialogText = infoText;
        otherModInfoDialog.PopupCenteredShrink();
    }

    /// <summary>
    ///   Fills the ConfigContainer with all of ConfigItems
    /// </summary>
    private Control ConfigMenuSetup(ModConfigItemInfo[] modConfigList, Dictionary<string, object> modConfigDictionary)
    {
        // Holder of all the config item for easier removal
        VBoxContainer configTreeNode = new VBoxContainer();

        foreach (var currentItemInfo in modConfigList)
        {
            HBoxContainer? currentItem;
            modConfigDictionary.TryGetValue(currentItemInfo.ID ?? string.Empty, out var configValue);

            currentItem = ConfigItemScene.Instance() as HBoxContainer;
            if (currentItem is null)
            {
                continue;
            }

            var currentItemLabel = currentItem.GetChild(0) as Label;

            // Set the name and tooltip of the item
            if (currentItemLabel != null)
            {
                currentItemLabel.Text = (currentItemInfo.DisplayName ?? currentItemInfo.ID) + ":";
                currentItem.HintTooltip = currentItemInfo.Description ?? string.Empty;
            }

            // Setup the UI based on it type
            switch (currentItemInfo.Type?.ToLower(CultureInfo.CurrentCulture))
            {
                case "integer":
                    var intNumberSpinner = new SpinBox();
                    intNumberSpinner.Rounded = true;
                    intNumberSpinner.MinValue = currentItemInfo.MinimumValue;
                    intNumberSpinner.Value = Convert.ToInt32(configValue ?? default(int),
                        CultureInfo.CurrentCulture);
                    intNumberSpinner.MaxValue = currentItemInfo.MaximumValue;
                    currentItem.AddChild(intNumberSpinner);
                    break;
                case "float":
                    var floatNumberSpinner = new SpinBox();
                    floatNumberSpinner.Rounded = false;
                    floatNumberSpinner.Step = 0.1;
                    floatNumberSpinner.MinValue = currentItemInfo.MinimumValue;
                    floatNumberSpinner.Value = Convert.ToDouble(configValue ?? default(double),
                        CultureInfo.CurrentCulture);
                    floatNumberSpinner.MaxValue = currentItemInfo.MaximumValue;
                    currentItem.AddChild(floatNumberSpinner);
                    break;
                case "integer range":
                    var intNumberSlider = new HSlider();
                    intNumberSlider.Rounded = true;
                    intNumberSlider.MinValue = currentItemInfo.MinimumValue;
                    intNumberSlider.Value = Convert.ToInt32(configValue ?? default(int),
                        CultureInfo.CurrentCulture);
                    intNumberSlider.MaxValue = currentItemInfo.MaximumValue;
                    intNumberSlider.SizeFlagsHorizontal = 3;
                    currentItem.AddChild(intNumberSlider);
                    break;
                case "float range":
                    var floatNumberSlider = new HSlider();
                    floatNumberSlider.Rounded = false;
                    floatNumberSlider.Step = 0.1;
                    floatNumberSlider.MinValue = currentItemInfo.MinimumValue;
                    floatNumberSlider.Value = Convert.ToDouble(configValue ?? default(double),
                        CultureInfo.CurrentCulture);
                    floatNumberSlider.MaxValue = currentItemInfo.MaximumValue;
                    floatNumberSlider.SizeFlagsHorizontal = 3;
                    currentItem.AddChild(floatNumberSlider);
                    break;
                case "boolean":
                    var booleanCheckbutton = new CheckButton();
                    booleanCheckbutton.Pressed =
                        Convert.ToBoolean(configValue ?? default(bool), CultureInfo.CurrentCulture);
                    booleanCheckbutton.Flat = true;
                    currentItem.AddChild(booleanCheckbutton);
                    break;
                case "string":
                    var stringLineEdit = new LineEdit();
                    stringLineEdit.SizeFlagsHorizontal = 3;
                    stringLineEdit.Text = (string?)(configValue ?? default(string));
                    stringLineEdit.MaxLength = (int)currentItemInfo.MaximumValue;
                    currentItem.AddChild(stringLineEdit);
                    break;
                case "title":
                    if (currentItemLabel != null)
                    {
                        currentItemLabel.Text = currentItemInfo.DisplayName ?? currentItemInfo.ID ?? string.Empty;
                        currentItem.Alignment = BoxContainer.AlignMode.Center;
                    }

                    break;
                case "option":
                    var optionButton = new OptionButton();
                    foreach (var optionItem in currentItemInfo.GetAllOptions())
                    {
                        optionButton.AddItem(optionItem);
                    }

                    optionButton.Selected = Convert.ToInt32(configValue ?? default(int),
                        CultureInfo.CurrentCulture);
                    currentItem.AddChild(optionButton);
                    break;
                case "colour":
                    var regularColorPickerButton = new ColorPickerButton();
                    regularColorPickerButton.EditAlpha = false;
                    regularColorPickerButton.Color =
                        new Color(Convert.ToString(configValue));
                    regularColorPickerButton.Text = "Color";
                    currentItem.AddChild(regularColorPickerButton);
                    break;
                case "colour with alpha":
                    var colorAlphaPickerButton = new ColorPickerButton();
                    colorAlphaPickerButton.Color = new Color(Convert.ToString(configValue));
                    colorAlphaPickerButton.Text = "Color";
                    currentItem.AddChild(colorAlphaPickerButton);
                    break;
            }

            // Get the script from the current item
            var currentItemNodeInfo = currentItem as ModConfigItemInfo;

            if (currentItemNodeInfo != null)
            {
                // Set all of the data from current item to the node
                if (currentItemInfo.ID != null)
                {
                    currentItemNodeInfo.Value = configValue;
                }

                currentItemNodeInfo.ID = currentItemInfo.ID;
                currentItemNodeInfo.DisplayName = currentItemInfo.DisplayName;
                currentItemNodeInfo.Description = currentItemInfo.Description;
                currentItemNodeInfo.MaximumValue = currentItemInfo.MaximumValue;
                currentItemNodeInfo.MinimumValue = currentItemInfo.MinimumValue;
                currentItemNodeInfo.Type = currentItemInfo.Type;
                currentItemNodeInfo.Options = currentItemInfo.Options;
            }

            // Add it to the container
            configTreeNode.AddChild(currentItem);
        }

        // Finally adds everything to the SceneTree
        configContainer.AddChild(configTreeNode);
        return configTreeNode;
    }

    private void GalleryRightArrowPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        selectedModPreviewImagesContainer.CurrentTab += 1;
        galleryLabel.Text = (selectedModPreviewImagesContainer.CurrentTab + 1) + "/" +
            selectedModPreviewImagesContainer.GetTabCount();
        galleryRightButton.Disabled = selectedModPreviewImagesContainer.CurrentTab >=
            selectedModPreviewImagesContainer.GetTabCount() - 1;
        galleryLeftButton.Disabled = selectedModPreviewImagesContainer.CurrentTab <= 0;
    }

    private void GalleryLeftArrowPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        selectedModPreviewImagesContainer.CurrentTab -= 1;
        galleryLabel.Text = (selectedModPreviewImagesContainer.CurrentTab + 1) + "/" +
            selectedModPreviewImagesContainer.GetTabCount();
        galleryRightButton.Disabled = selectedModPreviewImagesContainer.CurrentTab >=
            selectedModPreviewImagesContainer.GetTabCount() - 1;
        galleryLeftButton.Disabled = selectedModPreviewImagesContainer.CurrentTab <= 0;
    }

    private void MoveButtonPressed(bool moveUp, int amount)
    {
        if (notEnabledMods is null || enabledMods is null)
        {
            return;
        }

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

        MoveItem(chosenList, chosenModList, moveUp, chosenList.GetSelectedItems()[0], amount);

        var currentIndex = chosenList.GetSelectedItems()[0];

        moveModUpButton.Disabled = currentIndex == 0;
        moveModDownButton.Disabled = currentIndex >= chosenList.GetItemCount() - amount;

        UpdateOverallModButtons();
    }

    /// <summary>
    ///   Handles the movement of the ItemList by any amount
    /// </summary>
    private void MoveItem(ItemList list, List<FullModDetails> modList, bool moveUp, int currentIndex, int amount)
    {
        GUICommon.Instance.PlayButtonPressSound();
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
        UpdateLoadPosition(moveUp ? newIndex : currentIndex);
    }

    private void ResetPressed()
    {
        // Basically just a macro to disabled all the mods then apply the changes
        DisableAllPressed();
        ApplyChanges();
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
        fullInfoLongDescription.Text = info.LongDescription;
        fullInfoFromWorkshop.Text = selectedMod!.Workshop ?
            TranslationServer.Translate("THIS_IS_WORKSHOP_MOD") :
            TranslationServer.Translate("THIS_IS_LOCAL_MOD");
        fullInfoIconFile.Text = info.Icon;
        string fullInfoPreviewImagesText = string.Empty;
        info.PreviewImages?.ForEach(s => fullInfoPreviewImagesText += "* " + s + "\n");
        fullInfoPreviewImagesFile.Text = fullInfoPreviewImagesText;
        fullInfoInfoUrl.Text = info.InfoUrl == null ? string.Empty : info.InfoUrl.ToString();
        fullInfoLicense.Text = info.License;
        fullInfoRecommendedThrive.Text = info.RecommendedThriveVersion;
        fullInfoMinimumThrive.Text = info.MinimumThriveVersion;
        fullInfoMaximumThrive.Text = info.MaximumThriveVersion;
        fullInfoPckName.Text = info.PckToLoad;
        fullInfoModAssembly.Text = info.ModAssembly;
        fullInfoAssemblyModClass.Text = info.AssemblyModClass;
        fullInfoModConfig.Text = info.ConfigToLoad;
        string fullInfoDependenciesText = string.Empty;
        info.Dependencies?.ForEach(s => fullInfoDependenciesText += "* " + s + "\n");
        fullInfoDependencies.Text = fullInfoDependenciesText;
        string fullInfoRequiredModsText = string.Empty;
        info.RequiredMods?.ForEach(s => fullInfoRequiredModsText += "* " + s + "\n");
        fullInfoRequiredMods.Text = fullInfoRequiredModsText;
        string fullInfoLoadBeforeText = string.Empty;
        info.LoadBefore?.ForEach(s => fullInfoLoadBeforeText += "* " + s + "\n");
        fullInfoLoadBefore.Text = fullInfoLoadBeforeText;
        string fullInfoLoadAfterText = string.Empty;
        info.LoadAfter?.ForEach(s => fullInfoLoadAfterText += "* " + s + "\n");
        fullInfoLoadAfter.Text = fullInfoLoadAfterText;
        string fullInfoIncompatibleModsText = string.Empty;
        info.IncompatibleMods?.ForEach(s => fullInfoIncompatibleModsText += "* " + s + "\n");
        fullInfoIncompatibleMods.Text = fullInfoIncompatibleModsText;

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

        if (enabledMods is null)
        {
            return;
        }

        // Checks if there is anything that is going to be loaded first
        if (enabledMods.Count <= 0)
        {
            return;
        }

        var checkResult = ModLoader.IsValidModList(enabledMods);
        var resultText = string.Empty;

        if (checkResult.ErrorType < 0)
        {
            resultText = TranslationServer.Translate("MOD_LIST_CONTAIN_ERRORS") + "\n\n" +
                ModHelpers.CheckResultToString(checkResult, enabledMods) + "\n\n";
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

        if (!string.IsNullOrWhiteSpace(parsedData.Info.ConfigToLoad))
        {
            GD.Print("Creating Config File");
            if (file.Open(Path.Combine(parsedData.Folder, parsedData.Info.ConfigToLoad ?? string.Empty),
                    File.ModeFlags.Write) == Error.Ok)
            {
                file.StoreString("[\n]");
            }
            else
            {
                GD.PrintErr("Can't create mod config file: ", file.GetError());
            }

            file.Close();
        }

        GD.Print("Mod folder created, trying to open: ", parsedData.Folder);
        FolderHelpers.OpenFolder(parsedData.Folder);

        RefreshAvailableMods();
    }

    /// <summary>
    ///   Make sure the ConfigurationList variable is not null and if it can't find it
    ///   Then it returns a blank array of ModConfigItemInfo
    /// </summary>
    private void VerifyConfigFileExist(FullModDetails checkedModInfo)
    {
        // Checks if it null or empty
        if (checkedModInfo.ConfigurationInfoList == null || checkedModInfo.ConfigurationInfoList.Length < 1)
        {
            if (checkedModInfo.Info.ConfigToLoad != null &&
                FileHelpers.Exists(Path.Combine(checkedModInfo.Folder, checkedModInfo.Info.ConfigToLoad)))
            {
                checkedModInfo.ConfigurationInfoList = GetModConfigList(checkedModInfo);
            }

            ModConfigItemInfo[] currentConfigList =
                checkedModInfo.ConfigurationInfoList ?? Array.Empty<ModConfigItemInfo>();
            Dictionary<string, object> tempDictionary = new Dictionary<string, object>();
            for (int index = 0; index < currentConfigList.Length; ++index)
            {
                if (currentConfigList[index].ID != null && currentConfigList[index].Value != null)
                {
                    tempDictionary[currentConfigList[index].ID ?? string.Empty] =
                        currentConfigList[index].Value ?? new Dictionary<string, object>();
                }
            }

            checkedModInfo.CurrentConfiguration = tempDictionary;
        }
    }

    private void OpenModUploader()
    {
        GUICommon.Instance.PlayButtonPressSound();

        // Don't allow uploading workshop mods again
        modUploader.Open(validMods.Where(m => !m.Workshop));
    }

    private void ApplyModConfig()
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (selectedMod is null)
        {
            return;
        }

        var configItemArray = configContainer.GetChild(0).GetChildren();
        for (int i = 0; i < configItemArray.Count; i++)
        {
            ModConfigItemInfo currentItemInfo = (ModConfigItemInfo)configItemArray[i];
            if (currentItemInfo != null)
            {
                // Update the values from UI
                currentItemInfo.UpdateInternalValue();

                if (selectedMod != null && selectedMod.CurrentConfiguration != null && currentItemInfo.ID != null)
                {
                    selectedMod.CurrentConfiguration[currentItemInfo.ID] =
                        currentItemInfo.Value ?? new Dictionary<string, object>();
                }
            }
        }

        if (selectedMod?.CurrentConfiguration != null)
        {
            if (ModLoader.Instance.LoadedModAssemblies.ContainsKey(selectedMod?.InternalName ?? string.Empty))
            {
                ModLoader.Instance.LoadedModAssemblies[selectedMod?.InternalName ?? string.Empty]
                    ?.UpdatedConfiguration(selectedMod?.CurrentConfiguration ?? new Dictionary<string, object>());
            }
        }

        SaveAllModsSettings();
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
