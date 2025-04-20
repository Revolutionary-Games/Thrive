using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using Newtonsoft.Json;
using DirAccess = Godot.DirAccess;
using FileAccess = Godot.FileAccess;

/// <summary>
///   Provides GUI for managing enabled mods
/// </summary>
public partial class ModManager : Control
{
    private readonly List<FullModDetails> validMods = new();

    private List<FullModDetails>? notEnabledMods;
    private List<FullModDetails>? enabledMods;

#pragma warning disable CA2213
    [Export]
    private Button leftArrow = null!;
    [Export]
    private Button rightArrow = null!;

    [Export]
    private ItemList availableModsContainer = null!;
    [Export]
    private ItemList enabledModsContainer = null!;

    [Export]
    private Button openModInfoButton = null!;
    [Export]
    private Button openModUrlButton = null!;
    [Export]
    private Button disableAllModsButton = null!;
    [Export]
    private Label selectedModName = null!;
    [Export]
    private TextureRect selectedModIcon = null!;
    [Export]
    private Label selectedModAuthor = null!;
    [Export]
    private Label selectedModVersion = null!;
    [Export]
    private Label selectedModRecommendedThriveVersion = null!;
    [Export]
    private Label selectedModMinimumThriveVersion = null!;
    [Export]
    private Label selectedModDescription = null!;

    [Export]
    private Button applyChangesButton = null!;

    [Export]
    private CustomWindow unAppliedChangesWarning = null!;

    [Export]
    private CustomWindow modFullInfoPopup = null!;
    [Export]
    private Label fullInfoName = null!;
    [Export]
    private Label fullInfoInternalName = null!;
    [Export]
    private Label fullInfoAuthor = null!;
    [Export]
    private Label fullInfoVersion = null!;
    [Export]
    private Label fullInfoDescription = null!;
    [Export]
    private Label fullInfoLongDescription = null!;
    [Export]
    private Label fullInfoFromWorkshop = null!;
    [Export]
    private Label fullInfoIconFile = null!;
    [Export]
    private Label fullInfoInfoUrl = null!;
    [Export]
    private Label fullInfoLicense = null!;
    [Export]
    private Label fullInfoRecommendedThrive = null!;
    [Export]
    private Label fullInfoMinimumThrive = null!;
    [Export]
    private Label fullInfoMaximumThrive = null!;
    [Export]
    private Label fullInfoPckName = null!;
    [Export]
    private Label fullInfoModAssembly = null!;
    [Export]
    private Label fullInfoAssemblyModClass = null!;
    [Export]
    private Label fullInfoAutoHarmony = null!;

    [Export]
    private Button openWorkshopButton = null!;
    [Export]
    private Button modUploaderButton = null!;

    [Export]
    private NewModGUI newModGUI = null!;

    [Export]
    private ErrorDialog modCreateErrorDialog = null!;

    [Export]
    private ModUploader modUploader = null!;

    [Export]
    private ErrorDialog modErrorDialog = null!;

    [Export]
    private CustomWindow restartRequired = null!;
#pragma warning restore CA2213

    private FullModDetails? selectedMod;

    /// <summary>
    ///   Used to automatically refresh this object when it becomes visible after being invisible
    /// </summary>
    private bool wasVisible;

    [Signal]
    public delegate void OnClosedEventHandler();

    /// <summary>
    ///   Loads mod info from a folder
    /// </summary>
    /// <param name="folder">Folder to load from</param>
    /// <returns>The info object if the info was valid, null otherwise</returns>
    public static ModInfo? LoadModInfo(string folder)
    {
        var infoFile = Path.Combine(folder, Constants.MOD_INFO_FILE_NAME);

        using var file = FileAccess.Open(infoFile, FileAccess.ModeFlags.Read);

        if (file == null)
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
    public static Texture2D? LoadModIcon(FullModDetails mod)
    {
        if (string.IsNullOrEmpty(mod.Info.Icon))
            return null;

        try
        {
            var image = Image.LoadFromFile(Path.Combine(mod.Folder, mod.Info.Icon!));

            return ImageTexture.CreateFromImage(image);
        }
        catch (Exception e)
        {
            GD.PrintErr(
                $"Failed to load icon for mod {mod.InternalName} (from folder: {mod.Folder}) due to exception: ", e);
            return null;
        }
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
                    throw new ArgumentException(Localization.Translate("INVALID_ICON_PATH"));
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
                    throw new ArgumentException(Localization.Translate("INVALID_URL_SCHEME"));
                }

                GD.PrintErr("Disallowed URI scheme in: ", info.InfoUrl);
                return false;
            }
        }

        if (info.ModAssembly != null && info.AssemblyModClass == null && info.UseAutoHarmony != true)
        {
            if (throwOnError)
            {
                throw new ArgumentException(Localization.Translate("ASSEMBLY_CLASS_REQUIRED"));
            }

            GD.PrintErr("AssemblyModClass must be set if ModAssembly is set (and auto harmony is not used)");
            return false;
        }

        if (info.UseAutoHarmony == true && string.IsNullOrEmpty(info.ModAssembly))
        {
            if (throwOnError)
            {
                throw new ArgumentException(Localization.Translate("ASSEMBLY_REQUIRED_WITH_HARMONY"));
            }

            GD.PrintErr("ModAssembly must be set if UseAutoHarmony is true");
            return false;
        }

        return true;
    }

    public override void _Ready()
    {
        base._Ready();

        // These are hidden in the editor to make selecting UI elements there easier
        newModGUI.Visible = true;
        modUploader.Visible = true;

        UpdateSelectedModInfo();

        if (!SteamHandler.Instance.IsLoaded)
        {
            openWorkshopButton.Visible = false;
            modUploaderButton.Visible = false;
        }
    }

    public override void _Process(double delta)
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

    private static bool IsAllowedModPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        if (path.Contains("//") || path.Contains("..") || path.StartsWith("/", StringComparison.Ordinal))
            return false;

        return true;
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

            result.Add(new FullModDetails(name, modFolder, info));
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
    private static List<string> FindModFolders()
    {
        var result = new List<string>();

        foreach (var location in Constants.ModLocations)
        {
            using var currentDirectory = DirAccess.Open(location);

            if (currentDirectory == null)
                continue;

            if (currentDirectory.ListDirBegin() != Error.Ok)
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

        availableModsContainer.DeselectAll();
        enabledModsContainer.DeselectAll();

        UpdateOverallModButtons();
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
        return Settings.Instance.EnabledMods.Value.Contains(mod.InternalName) || enabledMods!.Contains(mod);
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

            if (notEnabledMods!.Contains(selectedMod))
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
            selectedModName.Text = Localization.Translate("NO_SELECTED_MOD");
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

    /// <summary>
    ///   Opens the user manageable mod folder. Creates it if it doesn't exist already
    /// </summary>
    private void OpenModsFolder()
    {
        var folder = Constants.ModLocations[Constants.ModLocations.Count - 1];

        GD.Print("Opening mod folder: ", folder);

        if (DirAccess.MakeDirRecursiveAbsolute(folder) != Error.Ok)
        {
            GD.PrintErr("Failed to create mods folder for viewing");
        }

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

        Texture2D? icon = null;

        foreach (var index in availableModsContainer.GetSelectedItems())
        {
            icon = availableModsContainer.GetItemIcon(index);
            availableModsContainer.RemoveItem(index);
        }

        enabledModsContainer.AddItem(selectedMod.InternalName, icon);

        notEnabledMods!.Remove(selectedMod);
        enabledMods!.Add(selectedMod);

        OnModChangedLists();
    }

    private void DisableModPressed()
    {
        if (selectedMod == null)
        {
            GD.PrintErr("No mod is selected");
            return;
        }

        Texture2D? icon = null;

        foreach (var index in enabledModsContainer.GetSelectedItems())
        {
            icon = enabledModsContainer.GetItemIcon(index);
            enabledModsContainer.RemoveItem(index);
        }

        availableModsContainer.AddItem(selectedMod.InternalName, icon);

        enabledMods!.Remove(selectedMod);
        notEnabledMods!.Add(selectedMod);

        OnModChangedLists();
    }

    private void DisableAllPressed()
    {
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

        while (enabledModsContainer.ItemCount > 0)
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
        enabledModsContainer.DeselectAll();
        availableModsContainer.DeselectAll();

        UpdateOverallModButtons();
    }

    private void UpdateOverallModButtons()
    {
        // TODO: once mod load order controlling is added, this use of HashSet needs to be removed to allow reorder
        // be applied. For reorder perhaps mod loader needs to first unload *all* mods so that it can then load
        // everything in the right order
        applyChangesButton.Disabled =
            Settings.Instance.EnabledMods.Value.ToHashSet()
                .SetEquals(enabledMods!.Select(m => m.InternalName));

        disableAllModsButton.Disabled = enabledMods!.Count < 1;
    }

    private void ApplyChanges()
    {
        if (notEnabledMods == null || enabledMods == null)
        {
            GD.PrintErr("Can't apply changes as the mod manager was not opened yet");
            return;
        }

        GD.Print("Applying changes to enabled mods");

        Settings.Instance.EnabledMods.Value = enabledMods.Select(m => m.InternalName).ToList();

        var modLoader = ModLoader.Instance;
        modLoader.LoadMods();

        var errors = modLoader.GetAndClearModErrors();

        if (errors.Count > 0)
        {
            modErrorDialog.ExceptionInfo = string.Join("\n", errors);
            modErrorDialog.PopupCenteredShrink();
        }

        if (modLoader.RequiresRestart)
        {
            restartRequired.PopupCenteredShrink();
        }

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
            enabledModsContainer.DeselectAll();
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
            availableModsContainer.DeselectAll();
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
            Localization.Translate("THIS_IS_WORKSHOP_MOD") :
            Localization.Translate("THIS_IS_LOCAL_MOD");
        fullInfoIconFile.Text = info.Icon;
        fullInfoInfoUrl.Text = info.InfoUrl == null ? string.Empty : info.InfoUrl.ToString();
        fullInfoLicense.Text = info.License;
        fullInfoRecommendedThrive.Text = info.RecommendedThriveVersion;
        fullInfoMinimumThrive.Text = info.MinimumThriveVersion;
        fullInfoMaximumThrive.Text = info.MaximumThriveVersion;
        fullInfoPckName.Text = info.PckToLoad;
        fullInfoModAssembly.Text = info.ModAssembly;
        fullInfoAssemblyModClass.Text = info.AssemblyModClass;
        fullInfoAutoHarmony.Text = info.UseAutoHarmony == true ?
            Localization.Translate("USES_FEATURE") :
            Localization.Translate("DOES_NOT_USE_FEATURE");

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
            NullValueHandling = NullValueHandling.Include,
        }).Serialize(new JsonTextWriter(serialized) { Indentation = 4 }, parsedData.Info);
        var modInfoText = serialized.ToString();

        GD.Print("Creating new mod at: ", parsedData.Folder);

        if (DirAccess.MakeDirRecursiveAbsolute(parsedData.Folder) != Error.Ok)
        {
            modCreateErrorDialog.ErrorMessage = Localization.Translate("ERROR_CREATING_FOLDER");
            modCreateErrorDialog.ExceptionInfo = null;
            modCreateErrorDialog.PopupCenteredShrink();
            return;
        }

        using var file = FileAccess.Open(Path.Combine(parsedData.Folder, Constants.MOD_INFO_FILE_NAME),
            FileAccess.ModeFlags.Write);
        if (file == null)
        {
            modCreateErrorDialog.ErrorMessage = Localization.Translate("ERROR_CREATING_INFO_FILE");
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
            EmitSignal(SignalName.OnClosed);
        }
        else
        {
            unAppliedChangesWarning.PopupCenteredShrink();
        }
    }

    private void ConfirmBackWithUnAppliedChanges()
    {
        EmitSignal(SignalName.OnClosed);
    }
}
