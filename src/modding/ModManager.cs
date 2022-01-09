using System;
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
    public NodePath EnableAllModsButtonPath;

    [Export]
    public NodePath SelectedModInfoBoxPath;

    [Export]
    public NodePath SelectedModNamePath;

    [Export]
    public NodePath SelectedModIconPath;

    [Export]
    public NodePath SelectedModPreviewImagesContainerPath;

    [Export]
    public NodePath SelectedModGalleryContainerPath;

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
    public NodePath SelectedModFromWorkshopPath;

    [Export]
    public NodePath ModErrorDialogPath;

    [Export]
    public NodePath RestartRequiredPath;

    [Export]
    public NodePath ApplyChangesButtonPath;

    [Export]
    public NodePath GalleryLeftButtonPath;

    [Export]
    public NodePath GalleryRightButtonPath;

    [Export]
    public NodePath MoveModUpButtonPath;

    [Export]
    public NodePath MoveModDownButtonPath;

    [Export]
    public NodePath ResetButtonPath;

    [Export]
    public NodePath DependencyButtonPath;

    [Export]
    public NodePath IncompatibleButtonPath;

    [Export]
    public NodePath LoadOrderButtonPath;

    [Export]
    public NodePath CheckButtonPath;

    [Export]
    public NodePath GalleryLabelPath;

    [Export]
    public NodePath UnAppliedChangesWarningPath;

    [Export]
    public NodePath ModFullInfoPopupPath;

    [Export]
    public NodePath FullInfoNamePath;

    [Export]
    public NodePath FullInfoInternalNamePath;

    [Export]
    public NodePath FullInfoAuthorPath;

    [Export]
    public NodePath FullInfoVersionPath;

    [Export]
    public NodePath FullInfoDescriptionPath;

    [Export]
    public NodePath FullInfoLongDescriptionPath;

    [Export]
    public NodePath FullInfoFromWorkshopPath;

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
    public NodePath OpenWorkshopButtonPath;

    [Export]
    public NodePath ModUploaderButtonPath;

    [Export]
    public NodePath NewModGUIPath;

    [Export]
    public NodePath ModCreateErrorDialogPath;

    [Export]
    public NodePath ModUploaderPath;

    [Export]
    public NodePath SelectedModRecommendedThriveVersionContainerPath;

    [Export]
    public NodePath SelectedModMinimumThriveVersionContainerPath;

    [Export]
    public NodePath SelectedModThriveVersionContainerPath;

    [Export]
    public NodePath SelectedModThriveVersionHSeparatorPath;

    [Export]
    public NodePath ModCheckResultDialogPath;

    [Export]
    public NodePath LoadWarningDialogPath;

    [Export]
    public NodePath OtherModInfoDialogPath;

    [Export]
    public NodePath ModErrorsContainerPath;

    [Export]
    public NodePath ErrorInfoLabelPath;

    [Export]
    public NodePath OneshotLoadingCheckboxPath;

    [Export]
    public NodePath ConfigItemListPath;

    [Export]
    public NodePath ConfigContainerPath;

    [Export]
    public NodePath ConfigPanelContainerPath;

    [Export]
    public NodePath ModLoaderContainerPath;

    [Export]
    public PackedScene ConfigItemScene;

    private readonly List<FullModDetails> validMods = new();

    private List<FullModDetails> notEnabledMods;
    private List<FullModDetails> enabledMods;

    private Button leftArrow;
    private Button rightArrow;

    private ItemList availableModsContainer;
    private ItemList enabledModsContainer;
    private ItemList modErrorsContainer;
    private ItemList configModContainer;

    private Label errorInfoLabel;

    private Button openModInfoButton;
    private Button openModUrlButton;
    private Button disableAllModsButton;
    private Button enableAllModsButton;
    private Button galleryLeftButton;
    private Button galleryRightButton;
    private Button resetButton;
    private Button moveModUpButton;
    private Button dependencyButton;
    private Button incompatibleButton;
    private Button loadOrderButton;
    private Button checkButton;
    private Button moveModDownButton;
    private Button oneshotLoadingCheckbox;

    private Label galleryLabel;
    private Label selectedModName;
    private Label selectedFromWorkshop;
    private TextureRect selectedModIcon;
    private MarginContainer selectedModInfoBox;
    private TabContainer selectedModPreviewImagesContainer;
    private VBoxContainer selectedModGalleryContainer;
    private VBoxContainer selectedModRecommendedThriveVersionContainer;
    private VBoxContainer selectedModMinimumThriveVersionContainer;
    private HBoxContainer selectedModThriveVersionContainer;
    private Label selectedModAuthor;
    private Label selectedModVersion;
    private Label selectedModRecommendedThriveVersion;
    private Label selectedModMinimumThriveVersion;
    private RichTextLabel selectedModDescription;
    private HSeparator selectedModThriveVersionHSeparator;

    private Button applyChangesButton;

    private CustomDialog unAppliedChangesWarning;
    private CustomConfirmationDialog modCheckResultDialog;
    private CustomConfirmationDialog loadWarningDialog;
    private CustomConfirmationDialog otherModInfoDialog;

    private CustomDialog modFullInfoPopup;
    private Label fullInfoName;
    private Label fullInfoInternalName;
    private Label fullInfoAuthor;
    private Label fullInfoVersion;
    private Label fullInfoDescription;
    private Label fullInfoLongDescription;
    private Label fullInfoFromWorkshop;
    private Label fullInfoIconFile;
    private Label fullInfoInfoUrl;
    private Label fullInfoLicense;
    private Label fullInfoRecommendedThrive;
    private Label fullInfoMinimumThrive;
    private Label fullInfoMaximumThrive;
    private Label fullInfoPckName;
    private Label fullInfoModAssembly;
    private Label fullInfoAssemblyModClass;

    private Button openWorkshopButton;
    private Button modUploaderButton;

    private BoxContainer configContainer;
    private MarginContainer configPanelContainer;
    private TabContainer modLoaderContainer;

    private NewModGUI newModGUI;

    private ErrorDialog modCreateErrorDialog;

    private ModUploader modUploader;

    private ErrorDialog modErrorDialog;

    private CustomDialog restartRequired;

    private FullModDetails selectedMod;

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
    ///   Loads the icon for a mod
    /// </summary>
    /// <param name="mod">Mod to load the icon for</param>
    /// <returns>The loaded icon or null if mod doesn't have icon set</returns>
    public static Texture LoadModIcon(FullModDetails mod)
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
    ///   Gets a array of ModConfigItemInfo from a ModInfo
    /// </summary>
    public static ModConfigItemInfo[] GetModConfigList(FullModDetails currentMod)
    {
        if (FileHelpers.Exists(Path.Combine(currentMod.Folder, currentMod.Info.ConfigToLoad)))
        {
            var infoFile = Path.Combine(currentMod.Folder, currentMod.Info.ConfigToLoad);

            using var file = new File();

            if (file.Open(infoFile, File.ModeFlags.Read) != Error.Ok)
            {
                GD.PrintErr("Can't read config info file at: ", infoFile);
                return null;
            }

            return JsonConvert.DeserializeObject<ModConfigItemInfo[]>(file.GetAsText());
        }

        return null;
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
        selectedModDescription = GetNode<RichTextLabel>(SelectedModDescriptionPath);
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
        fullInfoFromWorkshop = GetNode<Label>(FullInfoFromWorkshopPath);
        fullInfoInfoUrl = GetNode<Label>(FullInfoInfoUrlPath);
        fullInfoLicense = GetNode<Label>(FullInfoLicensePath);
        fullInfoRecommendedThrive = GetNode<Label>(FullInfoRecommendedThrivePath);
        fullInfoMinimumThrive = GetNode<Label>(FullInfoMinimumThrivePath);
        fullInfoMaximumThrive = GetNode<Label>(FullInfoMaximumThrivePath);
        fullInfoPckName = GetNode<Label>(FullInfoPckNamePath);
        fullInfoModAssembly = GetNode<Label>(FullInfoModAssemblyPath);
        fullInfoAssemblyModClass = GetNode<Label>(FullInfoAssemblyModClassPath);

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

        modLoaderContainer.SetTabTitle(0, TranslationServer.Translate("MOD_LOADER"));
        modLoaderContainer.SetTabTitle(1, TranslationServer.Translate("MOD_ERRORS"));
        modLoaderContainer.SetTabTitle(2, TranslationServer.Translate("MOD_CONFIGURATION"));

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
        for (int index = startIndex; index < enabledMods.Count; ++index)
        {
            enabledMods[index].LoadPosition = index;
        }
    }

    /// <summary>
    ///   Gets a array of FullModDetails of all the mods that have a config file
    /// </summary>
    public List<FullModDetails> GetAllConfigurableMods()
    {
        List<FullModDetails> resultArray = new List<FullModDetails>();
        foreach (FullModDetails currentMod in enabledMods)
        {
            if (currentMod.CurrentConfiguration != null)
            {
                resultArray.Add(currentMod);
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
        Dictionary<string, Dictionary<string, object>> savedConfig =
            new Dictionary<string, Dictionary<string, object>>();
        foreach (FullModDetails currentMod in modConfigList)
        {
            savedConfig.Add(currentMod.InternalName, currentMod.CurrentConfiguration);
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
            return null;
        }

        var savedConfig =
            JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(file.GetAsText());
        file.Close();

        return savedConfig;
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

    /// <summary>
    ///   Refreshes things that need refreshing when this is opened
    /// </summary>
    private new void Update()
    {
        OnOpened();
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

    private void RefreshModErrors()
    {
        if (modErrorsContainer.IsAnythingSelected())
        {
            selectedMod = null;
            UpdateSelectedModInfo();
        }

        modErrorsContainer.Clear();

        var modErrors = ModLoader.Instance.GetModErrors();
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
        if (configModContainer.IsAnythingSelected())
        {
            selectedMod = null;
            UpdateSelectedModInfo();
        }

        configModContainer.Clear();

        var savedConfig = LoadAllModsSettings();

        // TODO: It should only load mods that are actually loaded but I am not sure how...
        foreach (var currentMod in enabledMods)
        {
            VerifyConfigFileExist(currentMod);
            if (currentMod.CurrentConfiguration != null)
            {
                if (savedConfig.ContainsKey(currentMod.InternalName))
                {
                    currentMod.CurrentConfiguration = savedConfig[currentMod.InternalName];
                }

                configModContainer.AddItem(currentMod.InternalName, LoadModIcon(currentMod));
            }
        }
    }

    private void OnClearConfigButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        foreach (var currentItem in configContainer.GetChildren())
        {
            var currentItemInfo = currentItem as ModConfigItemInfo;
            currentItemInfo?.UpdateUI();
        }
    }

    private void OnResetConfigButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        var configItemArray = configContainer.GetChildren();

        int index = 0;
        foreach (ModConfigItemInfo currentItemInfo in configItemArray)
        {
            if (selectedMod != null && currentItemInfo != null)
            {
                VerifyConfigFileExist(selectedMod);
                currentItemInfo.Value = selectedMod.ConfigurationInfoList[index].Value;
                if (currentItemInfo.ID != null)
                {
                    selectedMod.CurrentConfiguration[currentItemInfo.ID] = currentItemInfo.Value;
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

            selectedModDescription.BbcodeText = selectedMod.Info.LongDescription ?? selectedMod.Info.Description;
            openModUrlButton.Disabled = selectedMod.Info.InfoUrl == null;

            if (selectedModPreviewImagesContainer.GetChildCount() > 0)
            {
                selectedModPreviewImagesContainer.QueueFreeChildren();
            }

            List<ImageTexture> loadedPreviewImages = LoadModPreviewImages(selectedMod);
            if (loadedPreviewImages != null && loadedPreviewImages.Count > 0)
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
        if (mod.Info?.PreviewImages == null)
            return null;

        var returnValue = new List<ImageTexture>();
        foreach (string currentImagePath in mod.Info.PreviewImages)
        {
            if (string.IsNullOrEmpty(currentImagePath))
                return null;

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

            var isCompatibleVersion = 0;
            var compatibleVersionTest = 0;

            if (!string.IsNullOrEmpty(info.MinimumThriveVersion) &&
                VersionUtils.Compare(Constants.Version, info.MinimumThriveVersion) >= 0)
            {
                ++compatibleVersionTest;
            }
            else if (!string.IsNullOrEmpty(info.MinimumThriveVersion))
            {
                compatibleVersionTest--;
            }

            if (!string.IsNullOrEmpty(info.MaximumThriveVersion) &&
                VersionUtils.Compare(Constants.Version, info.MaximumThriveVersion) >= 0)
            {
                ++isCompatibleVersion;
            }
            else if (!string.IsNullOrEmpty(info.MaximumThriveVersion))
            {
                compatibleVersionTest--;
            }

            if (compatibleVersionTest >= 1)
            {
                isCompatibleVersion = 1;
            }
            else if (compatibleVersionTest == 0)
            {
                isCompatibleVersion = -1;
            }
            else
            {
                isCompatibleVersion = -2;
            }

            result.Add(new FullModDetails(name)
                { Folder = modFolder, Info = info, IsCompatibleVersion = isCompatibleVersion });
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
        GUICommon.Instance.PlayButtonPressSound();

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
        GUICommon.Instance.PlayButtonPressSound();
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
            var icon = availableModsContainer.GetItemIcon(0);
            var text = availableModsContainer.GetItemText(0);
            availableModsContainer.RemoveItem(0);

            enabledModsContainer.AddItem(text, icon);
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
        applyChangesButton.Disabled =
            Settings.Instance.EnabledMods.Value.ToList()
                .SequenceEqual(enabledMods.Select(m => m.InternalName));

        var isEnabledModsEmpty = enabledMods.Count < 1;
        resetButton.Disabled = isEnabledModsEmpty;
        checkButton.Disabled = isEnabledModsEmpty;
        disableAllModsButton.Disabled = isEnabledModsEmpty;

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
            var warningText = "The mods you want to load might cause errors.\n\n" +
                CheckResultToString(checkResult, enabledMods);
            warningText += "\n\n Are you sure you want to load these mods?";

            loadWarningDialog.DialogText = warningText;
            loadWarningDialog.PopupCenteredShrink();
        }
    }

    private void LoadEnabledMods()
    {
        GD.Print("Applying changes to enabled mods");

        Settings.Instance.EnabledMods.Value = enabledMods.Select(m => m.InternalName).ToList();

        var modLoader = ModLoader.Instance;
        modLoader.LoadMods();

        var errors = modLoader.GetModErrors();

        if (errors.Count > 0)
        {
            var text = string.Empty;
            foreach ((FullModDetails, string ErrorMessage) currentError in errors)
            {
                text += currentError.ErrorMessage;
            }

            modErrorDialog.ExceptionInfo = text;
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
        errorInfoLabel.Text = (string)modErrorsContainer.GetItemMetadata(index) ?? "ERROR NOT FOUND.";
        errorInfoLabel.Show();
        configPanelContainer.Visible = false;
        moveModUpButton.Disabled = true;
        moveModDownButton.Disabled = true;
    }

    private void ModConfigModSelected(int index)
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

        if (enabledModsContainer.IsAnythingSelected())
            enabledModsContainer.UnselectAll();

        leftArrow.Disabled = true;
        rightArrow.Disabled = true;
        modErrorsContainer.UnselectAll();
        errorInfoLabel.Hide();
        moveModUpButton.Disabled = true;
        moveModDownButton.Disabled = true;

        VerifyConfigFileExist(newItem);

        if (newItem?.ConfigurationInfoList?.Length > 0)
        {
            configPanelContainer.Visible = true;
            if (configContainer.GetChildCount() > 0)
            {
                configContainer.RemoveChildren();
            }

            ConfigMenuSetup(newItem.ConfigurationInfoList, newItem.CurrentConfiguration);
        }
        else
        {
            configPanelContainer.Visible = false;
        }
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

    private void OnDependencyPressed()
    {
        otherModInfoDialog.WindowTitle = "Dependencies";
        var infoText = string.Empty;
        GUICommon.Instance.PlayButtonPressSound();

        var currentModDependencies = selectedMod.Info.Dependencies;
        if (currentModDependencies != null)
        {
            foreach (string currentDependency in currentModDependencies)
            {
                infoText += "* " + currentDependency + "\n";
            }
        }
        else
        {
            infoText += "This mod have no Dependencies";
        }

        otherModInfoDialog.DialogText = infoText;
        otherModInfoDialog.PopupCenteredShrink();
    }

    private void OnIncompatiblePressed()
    {
        otherModInfoDialog.WindowTitle = "Incompatible With";
        var infoText = string.Empty;
        GUICommon.Instance.PlayButtonPressSound();

        var currentModIncompatibleMods = selectedMod.Info.IncompatibleMods;
        if (currentModIncompatibleMods != null)
        {
            foreach (string currentIncompatibleMod in currentModIncompatibleMods)
            {
                infoText += "* " + currentIncompatibleMod + "\n";
            }
        }
        else
        {
            infoText += "This mod is not incompatible with any other mod.";
        }

        otherModInfoDialog.DialogText = infoText;
        otherModInfoDialog.PopupCenteredShrink();
    }

    private void OnLoadOrderPressed()
    {
        otherModInfoDialog.WindowTitle = "Load Order";
        var infoText = string.Empty;
        GUICommon.Instance.PlayButtonPressSound();

        var currentModLoadBefore = selectedMod.Info.LoadBefore;
        var currentModLoadAfter = selectedMod.Info.LoadAfter;
        if (currentModLoadBefore != null || currentModLoadAfter != null)
        {
            if (currentModLoadAfter != null)
            {
                infoText += "This mod needs to be loaded after the following mods:\n";
                foreach (string currentLoadAfterMod in currentModLoadAfter)
                {
                    infoText += "* " + currentLoadAfterMod + "\n";
                }
            }

            // If there both 'load before' and 'load after' is going to display add a empty line between them
            if (currentModLoadBefore != null && currentModLoadAfter != null)
            {
                infoText += "\n";
            }

            if (currentModLoadBefore != null)
            {
                infoText += "This mod needs to be loaded before the following mods:\n";
                foreach (string currentLoadBeforeMod in currentModLoadBefore)
                {
                    infoText += "* " + currentLoadBeforeMod + "\n";
                }
            }
        }
        else
        {
            infoText += "This mod have no specified load order.";
        }

        otherModInfoDialog.DialogText = infoText;
        otherModInfoDialog.PopupCenteredShrink();
    }

    /// <summary>
    ///   Fills the ConfigContainer with all of ConfigItems
    /// </summary>
    private void ConfigMenuSetup(ModConfigItemInfo[] modConfigList, Dictionary<string, object> modConfigDictionary)
    {
        foreach (var currentItemInfo in modConfigList)
        {
            HBoxContainer currentItem;
            modConfigDictionary.TryGetValue(currentItemInfo.ID, out var configValue);

            if (currentItemInfo.ConfigNode == null)
            {
                currentItem = ConfigItemScene.Instance() as HBoxContainer;
                var currentItemLabel = currentItem.GetChild(0) as Label;

                // Set the name and tooltip of the item
                currentItemLabel.Text = (currentItemInfo.DisplayName ?? currentItemInfo.ID) + ":";
                currentItem.HintTooltip = currentItemInfo.Description ?? string.Empty;

                // Setup the UI based on it type
                switch (currentItemInfo.Type.ToLower(CultureInfo.CurrentCulture))
                {
                    case "int":
                    case "integer":
                    case "i":
                        var intNumberSpinner = new SpinBox();
                        intNumberSpinner.Rounded = true;
                        intNumberSpinner.MinValue = currentItemInfo.MinimumValue;
                        intNumberSpinner.Value = Convert.ToInt32(configValue ?? default(int),
                            CultureInfo.CurrentCulture);
                        intNumberSpinner.MaxValue = currentItemInfo.MaximumValue;
                        currentItem.AddChild(intNumberSpinner);
                        break;
                    case "float":
                    case "f":
                        var floatNumberSpinner = new SpinBox();
                        floatNumberSpinner.Rounded = false;
                        floatNumberSpinner.Step = 0.1;
                        floatNumberSpinner.MinValue = currentItemInfo.MinimumValue;
                        floatNumberSpinner.Value = Convert.ToDouble(configValue ?? default(double),
                            CultureInfo.CurrentCulture);
                        floatNumberSpinner.MaxValue = currentItemInfo.MaximumValue;
                        currentItem.AddChild(floatNumberSpinner);
                        break;
                    case "int range":
                    case "integer range":
                    case "ir":
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
                    case "fr":
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
                    case "bool":
                    case "boolean":
                    case "b":
                        var booleanCheckbutton = new CheckButton();
                        booleanCheckbutton.Pressed =
                            Convert.ToBoolean(configValue ?? default(bool), CultureInfo.CurrentCulture);
                        booleanCheckbutton.Flat = true;
                        currentItem.AddChild(booleanCheckbutton);
                        break;
                    case "string":
                    case "s":
                        var stringLineEdit = new LineEdit();
                        stringLineEdit.SizeFlagsHorizontal = 3;
                        stringLineEdit.Text = (string)(configValue ?? default(string));
                        stringLineEdit.MaxLength = (int)currentItemInfo.MaximumValue;
                        currentItem.AddChild(stringLineEdit);
                        break;
                    case "title":
                    case "t":
                        currentItemLabel.Text = currentItemInfo.DisplayName ?? currentItemInfo.ID;
                        currentItem.Alignment = BoxContainer.AlignMode.Center;
                        break;
                    case "option":
                    case "enum":
                    case "o":
                        var optionButton = new OptionButton();
                        foreach (var optionItem in currentItemInfo.GetAllOptions())
                        {
                            optionButton.AddItem(optionItem);
                        }

                        optionButton.Selected = Convert.ToInt32(configValue ?? default(int),
                            CultureInfo.CurrentCulture);
                        currentItem.AddChild(optionButton);
                        break;
                    case "color":
                    case "colour":
                    case "c":
                        var regularColorPickerButton = new ColorPickerButton();
                        regularColorPickerButton.EditAlpha = false;
                        regularColorPickerButton.Color =
                            new Color(Convert.ToString(configValue) ?? default(string));
                        regularColorPickerButton.Text = "Color";
                        currentItem.AddChild(regularColorPickerButton);
                        break;
                    case "alphacolor":
                    case "alphacolour":
                    case "ac":
                        var colorAlphaPickerButton = new ColorPickerButton();
                        colorAlphaPickerButton.Color = new Color(Convert.ToString(configValue) ?? default(string));
                        colorAlphaPickerButton.Text = "Color";
                        currentItem.AddChild(colorAlphaPickerButton);
                        break;
                }
            }
            else
            {
                currentItem = currentItemInfo.ConfigNode as HBoxContainer;
            }

            // Get the script from the current item
            var currentItemNodeInfo = currentItem as ModConfigItemInfo;

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
            currentItemInfo.ConfigNode = currentItem;

            // Finally adds to the SceneTree
            configContainer.AddChild(currentItem);
        }
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
        fullInfoFromWorkshop.Text = selectedMod.Workshop ?
            TranslationServer.Translate("THIS_IS_WORKSHOP_MOD") :
            TranslationServer.Translate("THIS_IS_LOCAL_MOD");
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

        if (checkResult.ErrorType < 0)
        {
            resultText = "The mod list contains an errors: \n\n" + CheckResultToString(checkResult, enabledMods);
            resultText += "\n\n Once you fix that error try checking again to find more errors.";
        }
        else if (checkResult.ErrorType > 0)
        {
            resultText = "The mod list has no errors and is valid.";
        }

        modCheckResultDialog.DialogText = resultText;
        modCheckResultDialog.PopupCenteredShrink();
    }

    /// <summary>
    ///   Turns the result from a check into a string of the error and how to fix it
    /// </summary>
    private string CheckResultToString((int ErrorType, int ModIndex, int OtherModIndex) checkResult,
        List<FullModDetails> list)
    {
        var result = string.Empty;

        // The mod that is causing the error
        ModInfo offendingMod = new ModInfo();
        if (checkResult.ModIndex >= 0)
        {
            offendingMod = list[checkResult.ModIndex].Info;
        }
        else
        {
            offendingMod.Name = "Unknown Mod";
        }

        // The reason why the mod is causing an error
        ModInfo otherMod = new ModInfo();
        if (checkResult.OtherModIndex >= 0)
        {
            otherMod = list[checkResult.OtherModIndex].Info;
        }
        else
        {
            otherMod.Name = "Unknown Mod";
        }

        switch (checkResult.ErrorType)
        {
            default:
                result = "The mod list has no errors and is valid.";
                break;
            case (int)ModLoader.CheckErrorStatus.IncompatibleVersion:
                result += "The '" + offendingMod.Name + "' mod is incompatible with this version of Thrive.";
                break;
            case (int)ModLoader.CheckErrorStatus.DependencyNotFound:
                string otherModName;
                if (checkResult.OtherModIndex <= offendingMod.Dependencies.Count)
                {
                    otherModName = offendingMod.Dependencies[checkResult.OtherModIndex];
                }
                else
                {
                    otherModName = "ERROR: MOD NOT FOUND";
                }

                result += "The '" + offendingMod.Name + "' mod is dependent on the '" + otherModName + "' mod.\n";
                result += "Add that mod to the mod loader before this one to fix this error.";
                break;
            case (int)ModLoader.CheckErrorStatus.InvalidDependencyOrder:
                result += "The '" + offendingMod.Name + "' mod is dependent on the '" + otherMod.Name + "' mod.\n";
                result += "Load the '" + offendingMod.Name + "' mod after the '" + otherMod.Name +
                    "' mod to fix this error.";
                break;
            case (int)ModLoader.CheckErrorStatus.IncompatibleMod:
                result += "The '" + offendingMod.Name + "' mod is incompatible with the '" + otherMod.Name + "' mod.\n";
                result += "Remove the '" + otherMod.Name + "' mod or remove this one to fix this error.";
                break;
            case (int)ModLoader.CheckErrorStatus.InvalidLoadOrderBefore:
                result += "The '" + offendingMod.Name + "' mod needs to be loaded before the '" + otherMod.Name +
                    "' mod.\n";
                result += "Load the '" + offendingMod.Name + "' mod before the '" + otherMod.Name +
                    "' to fix this error.";
                break;
            case (int)ModLoader.CheckErrorStatus.InvalidLoadOrderAfter:
                result += "The '" + offendingMod.Name + "' mod needs to be loaded after the '" + otherMod.Name +
                    "' mod.\n";
                result += "Load the '" + offendingMod.Name + "' mod after the '" + otherMod.Name +
                    "' to fix this error.";
                break;
        }

        return result;
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

    /// <summary>
    ///   Make sure the ConfigurationList variable is not null and if it can't find it
    ///   Then it returns a blank array of ModConfigItemInfo
    /// </summary>
    private void VerifyConfigFileExist(FullModDetails checkedModInfo)
    {
        // Checks if it null or empty
        if (checkedModInfo?.ConfigurationInfoList == null || checkedModInfo.ConfigurationInfoList.Length < 1)
        {
            if (checkedModInfo?.Info.ConfigToLoad != null &&
                FileHelpers.Exists(Path.Combine(checkedModInfo.Folder, checkedModInfo.Info.ConfigToLoad)))
            {
                checkedModInfo.ConfigurationInfoList = GetModConfigList(checkedModInfo);
            }
            else
            {
                GD.Print("Mod Missing Config File: " + checkedModInfo?.InternalName);
                checkedModInfo.ConfigurationInfoList = Array.Empty<ModConfigItemInfo>();
            }

            var currentConfigList = checkedModInfo.ConfigurationInfoList;
            Dictionary<string, object> tempDictionary = new Dictionary<string, object>();
            for (int index = 0; index < currentConfigList.Length; ++index)
            {
                if (currentConfigList[index].ID != null)
                {
                    tempDictionary[currentConfigList[index].ID] = currentConfigList[index].Value;
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

        var configItemArray = configContainer.GetChildren();
        foreach (ModConfigItemInfo currentItemInfo in configItemArray)
        {
            if (currentItemInfo != null)
            {
                // Update the values from UI
                currentItemInfo.UpdateInternalValue();

                if (selectedMod != null && currentItemInfo.ID != null)
                {
                    selectedMod.CurrentConfiguration[currentItemInfo.ID] = currentItemInfo.Value;
                }
            }
        }

        if (ModLoader.Instance.LoadedModAssemblies != null)
        {
            if (ModLoader.Instance.LoadedModAssemblies.ContainsKey(selectedMod?.InternalName ?? string.Empty))
            {
                ModLoader.Instance.LoadedModAssemblies[selectedMod?.InternalName ?? string.Empty]
                    ?.UpdatedConfiguration(selectedMod?.CurrentConfiguration);
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
