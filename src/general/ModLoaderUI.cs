using System;
using Godot;

/// <summary>
///   Class managing the Mod Manager UI
/// </summary>
public class ModLoaderUI : Control
{
    [Export]
    public NodePath UnloadedItemListPath;

    [Export]
    public NodePath ModInfoContainerPath;

    [Export]
    public NodePath CompatibleVersionContainerPath;

    [Export]
    public NodePath ErrorInfoContainerPath;

    [Export]
    public NodePath ModInfoNamePath;

    [Export]
    public NodePath ModInfoAuthorPath;

    [Export]
    public NodePath ModInfoVersionPath;

    [Export]
    public NodePath ModInfoDescriptionPath;

    [Export]
    public NodePath PreviewImagePath;

    [Export]
    public NodePath CompatibleVersionLabelPath;

    [Export]
    public NodePath LoadedItemListPath;

    [Export]
    public NodePath ErrorItemListPath;

    [Export]
    public NodePath ErrorLabelPath;

    [Export]
    public NodePath AutoLoadedItemListPath;

    [Export]
    public NodePath ResetPopupPath;

    [Export]
    public NodePath ReloadReminderPopupPath;

    [Export]
    public NodePath ModCheckPopupPath;

    [Export]
    public NodePath LoadWarningPopupPath;

    [Export]
    public NodePath SafeModeButtonPath;

    [Export]
    public NodePath InfoPopupPath;

    // The array is used for getting all of the ItemList
    private ItemList[] modItemLists;

    // Labels For The Mod Info Box
    private Label modInfoName;
    private Label modInfoAuthor;
    private Label modInfoVersion;
    private RichTextLabel modInfoDescription;
    private TextureRect modInfoPreviewImage;

    private Label errorLabel;
    private Label compatibleVersionLabel;

    private ConfirmationDialog resetPopup;
    private AcceptDialog reloadReminderPopup;
    private AcceptDialog modCheckPopup;
    private AcceptDialog infoPopup;
    private ConfirmationDialog loadWarningPopup;
    private CheckBox safeModeButton;

    private MarginContainer modInfoContainer;
    private MarginContainer errorInfoContainer;
    private BoxContainer compatibleVersionContainer;

    private ModLoader loader = new ModLoader();
    private ModInfo currentSelectedMod;

    [Signal]
    public delegate void OnModLoaderClosed();

    private enum ItemLists
    {
        UnloadedItemList,
        LoadedItemList,
        AutoloadedItemlist,
        ErrorItemList,
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        modInfoContainer = GetNode<MarginContainer>(ModInfoContainerPath);
        errorInfoContainer = GetNode<MarginContainer>(ErrorInfoContainerPath);
        compatibleVersionContainer = GetNode<BoxContainer>(CompatibleVersionContainerPath);

        // Temporary variables to hold all the ItemList to put it in the modItemLists
        ItemList unloadedItemList;
        ItemList loadedItemList;
        ItemList errorItemList;
        ItemList autoLoadedItemList;

        // Temporary variables are used to not make the array initialization too long
        unloadedItemList = GetNode<ItemList>(UnloadedItemListPath);
        loadedItemList = GetNode<ItemList>(LoadedItemListPath);
        errorItemList = GetNode<ItemList>(ErrorItemListPath);
        autoLoadedItemList = GetNode<ItemList>(AutoLoadedItemListPath);

        modItemLists = new ItemList[] { unloadedItemList, loadedItemList, autoLoadedItemList, errorItemList };
        errorLabel = GetNode<Label>(ErrorLabelPath);
        compatibleVersionLabel = GetNode<Label>(CompatibleVersionLabelPath);
        safeModeButton = GetNode<CheckBox>(SafeModeButtonPath);

        modInfoName = GetNode<Label>(ModInfoNamePath);
        modInfoAuthor = GetNode<Label>(ModInfoAuthorPath);
        modInfoVersion = GetNode<Label>(ModInfoVersionPath);
        modInfoDescription = GetNode<RichTextLabel>(ModInfoDescriptionPath);
        modInfoPreviewImage = GetNode<TextureRect>(PreviewImagePath);

        resetPopup = GetNode<ConfirmationDialog>(ResetPopupPath);
        reloadReminderPopup = GetNode<AcceptDialog>(ReloadReminderPopupPath);
        modCheckPopup = GetNode<AcceptDialog>(ModCheckPopupPath);
        infoPopup = GetNode<AcceptDialog>(InfoPopupPath);
        loadWarningPopup = GetNode<ConfirmationDialog>(LoadWarningPopupPath);

        loadWarningPopup.GetOk().Text = "Yes";
        loadWarningPopup.GetCancel().Text = "No";
        ReloadModLists();
    }

    private void OnModSelected(int itemIndex, int selectedItemList)
    {
        ModInfo tempModInfo = (ModInfo)modItemLists[selectedItemList].GetItemMetadata(itemIndex);
        currentSelectedMod = tempModInfo;

        for (int i = 0; i < modItemLists.Length; ++i)
        {
            if (i != selectedItemList)
            {
                modItemLists[i].UnselectAll();
            }
        }

        modInfoContainer.Visible = true;

        if (tempModInfo != null)
        {
            if (tempModInfo.DisplayName != null)
            {
                modInfoName.Text = tempModInfo.DisplayName;
            }
            else
            {
                modInfoName.Text = tempModInfo.Name;
            }

            modInfoAuthor.Text = tempModInfo.Author;
            modInfoVersion.Text = tempModInfo.Version;
            modInfoDescription.BbcodeText = tempModInfo.Description;

            if (tempModInfo.PreviewImage != null)
            {
                modInfoPreviewImage.Visible = true;
                modInfoPreviewImage.Texture = tempModInfo.PreviewImage;
            }
            else
            {
                modInfoPreviewImage.Visible = false;
            }

            // Checks if there is a Compatible Version and then display it
            compatibleVersionLabel.Text = string.Empty;
            if (tempModInfo.CompatibleVersion != null)
            {
                compatibleVersionContainer.Visible = true;
                foreach (string currentVersion in tempModInfo.CompatibleVersion)
                {
                    compatibleVersionLabel.Text += "* " + currentVersion + "\n";
                }
            }
            else
            {
                compatibleVersionContainer.Visible = false;
            }

            // Checks if the ErrorItemList was selected and if so display the error
            if (modItemLists[(int)ItemLists.ErrorItemList].GetSelectedItems().Length > 0)
            {
                errorInfoContainer.Visible = true;

                switch (tempModInfo.Status)
                {
                     case (int)ModLoader.ModStatus.ModFileCanNotBeFound:
                        errorLabel.Text = "The mod failed to load due to the path not being able to be found!";
                        break;
                     case (int)ModLoader.ModStatus.FailedModLoading:
                        errorLabel.Text = "The mod failed to load due to some unknown reasons!";
                        break;
                     case (int)ModLoader.ModStatus.ModAlreadyBeenLoaded:
                        errorLabel.Text = "The mod failed to load due to it already being loaded!";
                        break;
                     default:
                     case (int)ModLoader.ModStatus.ModLoadedSuccessfully:
                        errorLabel.Text = "The mod was loaded successfully!";
                        break;
                }
            }
            else
            {
                errorInfoContainer.Visible = false;
            }
        }
    }

    private void OnBackPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        modInfoContainer.Visible = false;
        for (int i = 0; i < modItemLists.Length; ++i)
        {
            modItemLists[i].UnselectAll();
        }

        EmitSignal(nameof(OnModLoaderClosed));
    }

    private void OnResetPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        resetPopup.PopupCenteredShrink();
    }

    private void OnMoveToLoadPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        var unloadedItemList = modItemLists[(int)ItemLists.UnloadedItemList];

        // Checks if anything was actually selected
        if (unloadedItemList.GetSelectedItems().Length <= 0)
        {
            return;
        }

        // Get the selected mod
        var selectedItem = unloadedItemList.GetSelectedItems()[0];

        ModInfo currentModInfo = (ModInfo)unloadedItemList.GetItemMetadata(selectedItem);
        AddModToItemList((int)ItemLists.LoadedItemList, modItemLists[(int)ItemLists.LoadedItemList].GetItemCount(), currentModInfo);

        unloadedItemList.RemoveItem(selectedItem);
    }

    private void OnMoveToUnloadPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        var loadedItemList = modItemLists[(int)ItemLists.LoadedItemList];

        // Checks if anything was actually selected
        if (loadedItemList.GetSelectedItems().Length <= 0)
        {
            return;
        }

        // Get the selected mod
        var selectedItem = loadedItemList.GetSelectedItems()[0];

        ModInfo currentModInfo = (ModInfo)loadedItemList.GetItemMetadata(selectedItem);
        AddModToItemList((int)ItemLists.UnloadedItemList, modItemLists[(int)ItemLists.UnloadedItemList].GetItemCount(), currentModInfo);

        loadedItemList.RemoveItem(selectedItem);
    }

    /// <summary>
    ///   Adds a mod to one of the ItemList in modItemLists
    /// </summary>
    private void AddModToItemList(int itemListIndex, int newItemIndex, ModInfo currentModInfo)
    {
        var tempItemList = modItemLists[itemListIndex];

        if (currentModInfo.DisplayName != null)
        {
            tempItemList.AddItem(currentModInfo.DisplayName);
        }
        else
        {
            tempItemList.AddItem(currentModInfo.Name);
        }

        tempItemList.SetItemMetadata(newItemIndex, currentModInfo);

        // Checks if a there is a icon image and if so set it
        if (currentModInfo.IconImage != null)
        {
            tempItemList.SetItemIcon(newItemIndex, currentModInfo.IconImage);
        }

        // Checks if a there is a stated compatible version on it
        if (currentModInfo.CompatibleVersion != null)
        {
            var isCompatible = false;
            foreach (string currentVersion in currentModInfo.CompatibleVersion)
            {
                if (currentVersion == Constants.Version)
                {
                    isCompatible = true;
                    currentModInfo.IsCompatibleVersion = 1;
                }
            }

            if (!isCompatible)
            {
                tempItemList.SetItemCustomFgColor(newItemIndex, new Color(1, 1, 0));
                tempItemList.SetItemTooltip(newItemIndex, "This mod might not be compatible with this version of Thrive.");
                currentModInfo.IsCompatibleVersion = -1;
            }
        }

        // Checks if the mod has a stated Incompatible version with it
        if (currentModInfo.IncompatibleVersion != null)
        {
            foreach (string currentVersion in currentModInfo.IncompatibleVersion)
            {
                if (currentVersion == Constants.Version)
                {
                    tempItemList.SetItemCustomFgColor(newItemIndex, new Color(1, 0, 0));
                    tempItemList.SetItemTooltip(newItemIndex, "This mod is not compatible with this version of Thrive.");
                    currentModInfo.IsCompatibleVersion = -2;
                    break;
                }
            }
        }
    }

    private void OnMoveUpPressed()
    {
        ItemList chosenList;

        if (modItemLists[(int)ItemLists.LoadedItemList].GetSelectedItems().Length > 0)
        {
            chosenList = modItemLists[(int)ItemLists.LoadedItemList];
        }
        else if (modItemLists[(int)ItemLists.UnloadedItemList].GetSelectedItems().Length > 0)
        {
            chosenList = modItemLists[(int)ItemLists.UnloadedItemList];
        }
        else
        {
            return;
        }

        MoveItem(chosenList, true, chosenList.GetSelectedItems()[0], 1);
    }

    private void OnMoveDownPressed()
    {
        if (modItemLists[(int)ItemLists.LoadedItemList].GetSelectedItems().Length > 0)
        {
            MoveItem(modItemLists[(int)ItemLists.LoadedItemList], false, modItemLists[(int)ItemLists.LoadedItemList].GetSelectedItems()[0], 1);
        }
        else if (modItemLists[(int)ItemLists.UnloadedItemList].GetSelectedItems().Length > 0)
        {
            MoveItem(modItemLists[(int)ItemLists.UnloadedItemList], false, modItemLists[(int)ItemLists.UnloadedItemList].GetSelectedItems()[0], 1);
        }
    }

    private void OnCheckPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        var loadedItemList = modItemLists[(int)ItemLists.LoadedItemList];

        if (loadedItemList.GetItemCount() <= 0)
        {
            return;
        }

        var checkResult = loader.IsValidModList(loadedItemList);
        var resultText = string.Empty;

        if (checkResult[0] < 0)
        {
            resultText = "The mod list contains an errors: \n\n" + CheckResultToString(checkResult, loadedItemList);
            resultText += "\n\n Once you fix that error try checking again to find more errors.";
        }
        else if (checkResult[0] > 0)
        {
            resultText = "The mod list has no errors and is valid.";
        }

        modCheckPopup.DialogText = resultText;
        modCheckPopup.PopupCenteredShrink();
    }

    private string CheckResultToString(int[] checkResult, ItemList list)
    {
        var result = string.Empty;

        // The mod that is causing the error
        var offendingMod = new ModInfo();

        // The reason why the mod is causing an error
        var otherMod = new ModInfo();

        if (checkResult.Length > 1)
        {
            offendingMod = (ModInfo)list.GetItemMetadata(checkResult[1]);
        }

        switch (checkResult[0])
        {
            default:
            case (int)ModLoader.CheckErrorStatus.EmptyList:
            case (int)ModLoader.CheckErrorStatus.Valid:
                result = "The mod list has no errors and is valid.";
                break;
            case (int)ModLoader.CheckErrorStatus.IncompatibleVersion:
                result += "The '" + offendingMod.Name + "' mod is incompatible with this version of Thrive.";
                break;
            case (int)ModLoader.CheckErrorStatus.DependencyNotFound:
                result += "The '" + offendingMod.Name + "' mod is dependent on the '" + offendingMod.Dependencies[checkResult[2]] + "' mod.\n";
                result += "Add that mod to the mod loader to fix this error.";
                break;
            case (int)ModLoader.CheckErrorStatus.InvalidDependencyOrder:
                otherMod = (ModInfo)list.GetItemMetadata(checkResult[2]);
                result += "The '" + offendingMod.Name + "' mod is dependent on the '" + otherMod.Name + "' mod.\n";
                result += "Load the '" + offendingMod.Name + "' mod after the '" + otherMod.Name + "' mod to fix this error.";
                break;
            case (int)ModLoader.CheckErrorStatus.IncompatibleMod:
                otherMod = (ModInfo)list.GetItemMetadata(checkResult[2]);
                result += "The '" + offendingMod.Name + "' mod is incompatible with the '" + otherMod.Name + "' mod.\n";
                result += "Remove the '" + otherMod.Name + "' mod to fix this error.";
                break;
            case (int)ModLoader.CheckErrorStatus.InvalidLoadOrderBefore:
                otherMod = (ModInfo)list.GetItemMetadata(checkResult[2]);
                result += "The '" + offendingMod.Name + "' mod needs to be loaded before the '" + otherMod.Name + "' mod.\n";
                result += "Load the '" + offendingMod.Name + "' mod before the '" + otherMod.Name + "' to fix this error.";
                break;
            case (int)ModLoader.CheckErrorStatus.InvalidLoadOrderAfter:
                otherMod = (ModInfo)list.GetItemMetadata(checkResult[2]);
                result += "The '" + offendingMod.Name + "' mod needs to be loaded after the '" + otherMod.Name + "' mod.\n";
                result += "Load the '" + offendingMod.Name + "' mod after the '" + otherMod.Name + "' to fix this error.";
                break;
        }

        return result;
    }

    private void OnDependencyPressed()
    {
        infoPopup.WindowTitle = "Dependencies";
        var infoText = string.Empty;
        GUICommon.Instance.PlayButtonPressSound();

        if (currentSelectedMod.Dependencies != null)
        {
            foreach (string currentDependency in currentSelectedMod.Dependencies)
            {
                infoText += "* " + currentDependency + "\n";
            }
        }
        else
        {
            infoText += "This mod have no Dependencies";
        }

        infoPopup.DialogText = infoText;
        infoPopup.PopupCenteredShrink();
    }

    private void OnIncompatiblePressed()
    {
        infoPopup.WindowTitle = "Incompatible With";
        var infoText = string.Empty;
        GUICommon.Instance.PlayButtonPressSound();

        if (currentSelectedMod.IncompatibleMods != null)
        {
            foreach (string currentIncompatibleMod in currentSelectedMod.IncompatibleMods)
            {
                infoText += "* " + currentIncompatibleMod + "\n";
            }
        }
        else
        {
            infoText += "This mod is not incompatible With any other mod.";
        }

        infoPopup.DialogText = infoText;
        infoPopup.PopupCenteredShrink();
    }

    private void OnLoadOrderPressed()
    {
        infoPopup.WindowTitle = "Load Order";
        var infoText = string.Empty;
        GUICommon.Instance.PlayButtonPressSound();

        if (currentSelectedMod.LoadBefore != null || currentSelectedMod.LoadAfter != null)
        {
            if (currentSelectedMod.LoadAfter != null)
            {
                infoText += "This mod needs to be loaded after the following mods:\n";
                foreach (string currentLoadAfterMod in currentSelectedMod.LoadAfter)
                {
                    infoText += "* " + currentLoadAfterMod + "\n";
                }
            }

            if (currentSelectedMod.LoadBefore != null && currentSelectedMod.LoadAfter != null)
            {
                infoText += "\n";
            }

            if (currentSelectedMod.LoadBefore != null)
            {
                infoText += "This mod needs to be loaded before the following mods:\n";
                foreach (string currentLoadBeforeMod in currentSelectedMod.LoadBefore)
                {
                    infoText += "* " + currentLoadBeforeMod + "\n";
                }
            }
        }
        else
        {
            infoText += "This mod have no specified load order.";
        }

        infoPopup.DialogText = infoText;
        infoPopup.PopupCenteredShrink();
    }

    /// <summary>
    ///   Handles the movement of the ItemList by any amount
    /// </summary>
    private void MoveItem(ItemList list, bool moveUp, int currentIndex, int amount)
    {
        GUICommon.Instance.PlayButtonPressSound();
        if (moveUp)
        {
            if (currentIndex == 0 || currentIndex - amount < 0)
            {
                return;
            }

            list.MoveItem(currentIndex, currentIndex - amount);
        }
        else
        {
            if (currentIndex == list.GetItemCount() - amount)
            {
                return;
            }

            list.MoveItem(currentIndex, currentIndex + amount);
        }
    }

    private void OnRefreshPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        for (int i = 0; i < modItemLists.Length; ++i)
        {
            modItemLists[i].Clear();
        }

        ReloadModLists();
    }

    /// <summary>
    ///   This is the method that actually reset the game
    /// </summary>
    private void ResetGame()
    {
        GUICommon.Instance.PlayButtonPressSound();
        loader.ResetGame();

        modItemLists[(int)ItemLists.UnloadedItemList].Clear();
        modItemLists[(int)ItemLists.LoadedItemList].Clear();
        ReloadModLists();

        reloadReminderPopup.PopupCenteredShrink();
    }

    /// <summary>
    ///   This get the mods from the mod directory and updates the UnloadedItemList
    /// </summary>
    private void ReloadModLists(bool unloadedModList = true, bool autoLoadedModList = true, bool errorModList = true)
    {
        if (unloadedModList)
        {
            var unloadedItemList = modItemLists[(int)ItemLists.UnloadedItemList];
            int index = 0;
            foreach (ModInfo currentModInfo in loader.LoadModList(true, false))
            {
                AddModToItemList((int)ItemLists.UnloadedItemList, index, currentModInfo);
                index++;
            }
        }

        if (autoLoadedModList)
        {
            var autoLoadedItemList = modItemLists[(int)ItemLists.AutoloadedItemlist];
            int index = 0;

            foreach (ModInfo currentModInfo in loader.LoadModList(false, true))
            {
                AddModToItemList((int)ItemLists.AutoloadedItemlist, index, currentModInfo);

                index++;
            }
        }

        if (errorModList)
        {
            var errorItemList = modItemLists[(int)ItemLists.ErrorItemList];
            int index = 0;

            foreach (ModInfo currentModInfo in ModLoader.FailedToLoadMods)
            {
                AddModToItemList((int)ItemLists.ErrorItemList, index, currentModInfo);
                index++;
            }
        }
    }

    private void OnLoadPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        var loadedItemList = modItemLists[(int)ItemLists.LoadedItemList];

        if (loadedItemList.GetItemCount() <= 0)
        {
            return;
        }

        var checkResult = loader.IsValidModList(loadedItemList);
        if ((checkResult[0] > 0 && safeModeButton.Pressed) || !safeModeButton.Pressed)
        {
            LoadAllMods();
        }
        else if (checkResult[0] < 0 && safeModeButton.Pressed)
        {
            var warningText = "The mods you want to load might cause errors.\n\n" + CheckResultToString(checkResult, loadedItemList);
            warningText += "\n\n Are you sure you want to load these mods?";
            loadWarningPopup.DialogText = warningText;
            loadWarningPopup.PopupCenteredShrink();
        }
    }

    private void LoadAllMods()
    {
        var loadedItemList = modItemLists[(int)ItemLists.LoadedItemList];

        loader.LoadModFromList(loadedItemList, true, true, true);
        loader.SaveAutoLoadedModsList();
        GD.Print("All mods loaded");
        SceneManager.Instance.ReturnToMenu();
    }
}
