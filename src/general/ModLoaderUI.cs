using System;
using System.Collections.Generic;
using System.Globalization;
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
    public NodePath OneShotButtonPath;

    [Export]
    public NodePath InfoPopupPath;

    [Export]
    public NodePath InfoButtonContainerPath;

    [Export]
    public NodePath LoadReminderPopupPath;

    [Export]
    public NodePath ConfigItemListPath;

    [Export]
    public NodePath ConfigContainerPath;

    [Export]
    public NodePath ConfigPanelContainerPath;

    [Export]
    public PackedScene ConfigItemScene;

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
    private ConfirmationDialog loadWarningPopup;
    private AcceptDialog reloadReminderPopup;
    private AcceptDialog modCheckPopup;
    private AcceptDialog infoPopup;
    private AcceptDialog loadReminderPopup;

    private CheckBox safeModeButton;
    private CheckBox oneShotButton;

    private MarginContainer modInfoContainer;
    private MarginContainer errorInfoContainer;
    private MarginContainer configPanelContainer;
    private BoxContainer compatibleVersionContainer;
    private BoxContainer infoButtonContainer;
    private BoxContainer configContainer;

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
        ConfigItemList,
    }

    public override void _Ready()
    {
        modInfoContainer = GetNode<MarginContainer>(ModInfoContainerPath);
        errorInfoContainer = GetNode<MarginContainer>(ErrorInfoContainerPath);
        configPanelContainer = GetNode<MarginContainer>(ConfigPanelContainerPath);
        compatibleVersionContainer = GetNode<BoxContainer>(CompatibleVersionContainerPath);
        infoButtonContainer = GetNode<BoxContainer>(InfoButtonContainerPath);
        configContainer = GetNode<BoxContainer>(ConfigContainerPath);

        // Temporary variables to hold all the ItemList to put it in the modItemLists
        ItemList unloadedItemList;
        ItemList loadedItemList;
        ItemList errorItemList;
        ItemList autoLoadedItemList;
        ItemList configItemList;

        // Temporary variables are used to not make the array initialization too long
        unloadedItemList = GetNode<ItemList>(UnloadedItemListPath);
        loadedItemList = GetNode<ItemList>(LoadedItemListPath);
        errorItemList = GetNode<ItemList>(ErrorItemListPath);
        autoLoadedItemList = GetNode<ItemList>(AutoLoadedItemListPath);
        configItemList = GetNode<ItemList>(ConfigItemListPath);

        modItemLists = new[] { unloadedItemList, loadedItemList, autoLoadedItemList, errorItemList, configItemList };
        errorLabel = GetNode<Label>(ErrorLabelPath);
        compatibleVersionLabel = GetNode<Label>(CompatibleVersionLabelPath);

        safeModeButton = GetNode<CheckBox>(SafeModeButtonPath);
        oneShotButton = GetNode<CheckBox>(OneShotButtonPath);

        modInfoName = GetNode<Label>(ModInfoNamePath);
        modInfoAuthor = GetNode<Label>(ModInfoAuthorPath);
        modInfoVersion = GetNode<Label>(ModInfoVersionPath);
        modInfoDescription = GetNode<RichTextLabel>(ModInfoDescriptionPath);
        modInfoPreviewImage = GetNode<TextureRect>(PreviewImagePath);

        resetPopup = GetNode<ConfirmationDialog>(ResetPopupPath);
        loadWarningPopup = GetNode<ConfirmationDialog>(LoadWarningPopupPath);
        reloadReminderPopup = GetNode<AcceptDialog>(ReloadReminderPopupPath);
        modCheckPopup = GetNode<AcceptDialog>(ModCheckPopupPath);
        infoPopup = GetNode<AcceptDialog>(InfoPopupPath);
        loadReminderPopup = GetNode<AcceptDialog>(LoadReminderPopupPath);

        loadWarningPopup.GetOk().Text = "Yes";
        loadWarningPopup.GetCancel().Text = "No";
        ReloadModLists();
    }

    private void OnModSelected(int itemIndex, int selectedItemList)
    {
        ModInfo tempModInfo = (ModInfo)modItemLists[selectedItemList].GetItemMetadata(itemIndex);
        currentSelectedMod = tempModInfo;

        // Unselects all the itemlist that are not being currently selected
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
            modInfoName.Text = tempModInfo.DisplayName ?? tempModInfo.Name;
            modInfoAuthor.Text = tempModInfo.Author;
            modInfoVersion.Text = tempModInfo.Version;
            modInfoDescription.BbcodeText = tempModInfo.Description;

            // Checks if there is a preview image and if so display it
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

            if (tempModInfo.Dependencies != null || tempModInfo.LoadBefore != null ||
                tempModInfo.IncompatibleMods != null || tempModInfo.LoadAfter != null)
            {
                infoButtonContainer.Visible = true;
            }
            else
            {
                infoButtonContainer.Visible = false;
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
                        errorLabel.Text = "The mod was loaded successfully!";
                        break;
                }
            }
            else
            {
                errorInfoContainer.Visible = false;
            }

            if (modItemLists[(int)ItemLists.ConfigItemList].GetSelectedItems().Length > 0)
            {
                configPanelContainer.Visible = true;
                if (configContainer.GetChildCount() > 0)
                {
                    configContainer.QueueFreeChildren();
                }

                VerifyConfigFileExist(tempModInfo);
                ConfigMenuSetup(tempModInfo.ConfigurationList, tempModInfo.Configuration);
            }
            else
            {
                configPanelContainer.Visible = false;
            }
        }
    }

    private void ConfigMenuSetup(ModConfigItemInfo[] modConfigList, Dictionary<string, object> modConfigDictionary)
    {
        foreach (var currentItemInfo in modConfigList)
        {
            var currentItem = ConfigItemScene.Instance() as HBoxContainer;
            var currentItemLabel = currentItem.GetChild(0) as Label;

            currentItemLabel.Text = (currentItemInfo.DisplayName ?? currentItemInfo.ID) + ":";
            currentItem.HintTooltip = currentItemInfo.Description ?? string.Empty;
            switch (currentItemInfo.Type.ToLower(CultureInfo.InvariantCulture))
            {
                case "int":
                case "integer":
                case "i":
                    var intNumberSpinner = new SpinBox();
                    intNumberSpinner.Rounded = true;
                    intNumberSpinner.MinValue = currentItemInfo.MinimumValue;
                    intNumberSpinner.Value = Convert.ToInt32(modConfigDictionary[currentItemInfo.ID] ?? default(int),
                        CultureInfo.InvariantCulture);
                    intNumberSpinner.MaxValue = currentItemInfo.MaximumValue;
                    currentItem.AddChild(intNumberSpinner);
                    break;
                case "float":
                case "f":
                    var floatNumberSpinner = new SpinBox();
                    floatNumberSpinner.Rounded = false;
                    floatNumberSpinner.Step = 0.1;
                    floatNumberSpinner.MinValue = currentItemInfo.MinimumValue;
                    floatNumberSpinner.Value = Convert.ToDouble(
                        modConfigDictionary[currentItemInfo.ID] ?? default(double),
                        CultureInfo.InvariantCulture);
                    floatNumberSpinner.MaxValue = currentItemInfo.MaximumValue;
                    currentItem.AddChild(floatNumberSpinner);
                    break;
                case "int range":
                case "integer range":
                case "ir":
                    var intNumberSlider = new HSlider();
                    intNumberSlider.Rounded = true;
                    intNumberSlider.MinValue = currentItemInfo.MinimumValue;
                    intNumberSlider.Value = Convert.ToInt32(modConfigDictionary[currentItemInfo.ID] ?? default(int),
                        CultureInfo.InvariantCulture);
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
                    floatNumberSlider.Value = Convert.ToDouble(
                        modConfigDictionary[currentItemInfo.ID] ?? default(double),
                        CultureInfo.InvariantCulture);
                    floatNumberSlider.MaxValue = currentItemInfo.MaximumValue;
                    floatNumberSlider.SizeFlagsHorizontal = 3;
                    currentItem.AddChild(floatNumberSlider);
                    break;
                case "bool":
                case "boolean":
                case "b":
                    var booleanCheckbutton = new CheckButton();
                    booleanCheckbutton.Pressed = (bool)(modConfigDictionary[currentItemInfo.ID] ?? default(bool));
                    currentItem.AddChild(booleanCheckbutton);
                    break;
                case "string":
                case "s":
                    var stringLineEdit = new LineEdit();
                    stringLineEdit.SizeFlagsHorizontal = 3;
                    stringLineEdit.Text = (string)(modConfigDictionary[currentItemInfo.ID] ?? default(string));
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

                    optionButton.Selected = Convert.ToInt32(modConfigDictionary[currentItemInfo.ID] ?? default(int),
                        CultureInfo.InvariantCulture);
                    currentItem.AddChild(optionButton);
                    break;
                case "color":
                case "colour":
                case "c":
                    var regularColorPickerButton = new ColorPickerButton();
                    regularColorPickerButton.EditAlpha = false;
                    regularColorPickerButton.Color =
                        new Color((string)modConfigDictionary[currentItemInfo.ID] ?? default(string));
                    regularColorPickerButton.Text = "Color";
                    currentItem.AddChild(regularColorPickerButton);
                    break;
                case "alphacolor":
                case "alphacolour":
                case "ac":
                    var colorAlphaPickerButton = new ColorPickerButton();
                    colorAlphaPickerButton.Color =
                        new Color((string)modConfigDictionary[currentItemInfo.ID] ?? default(string));
                    colorAlphaPickerButton.Text = "Color";
                    currentItem.AddChild(colorAlphaPickerButton);
                    break;
            }

            var currentItemNodeInfo = currentItem as ModConfigItemInfo;
            if (currentItemInfo.ID != null)
            {
                currentItemNodeInfo.Value = modConfigDictionary[currentItemInfo.ID];
            }

            currentItemNodeInfo.ID = currentItemInfo.ID;
            currentItemNodeInfo.DisplayName = currentItemInfo.DisplayName;
            currentItemNodeInfo.Description = currentItemInfo.Description;
            currentItemNodeInfo.MaximumValue = currentItemInfo.MaximumValue;
            currentItemNodeInfo.MinimumValue = currentItemInfo.MinimumValue;
            currentItemNodeInfo.Type = currentItemInfo.Type;
            currentItemNodeInfo.Options = currentItemInfo.Options;

            configContainer.AddChild(currentItem);
        }
    }

    private void OnBackPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        ResetLoaderUI();

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

    private void ResetLoaderUI()
    {
        modInfoContainer.Visible = false;
        configPanelContainer.Visible = false;
        errorInfoContainer.Visible = false;
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
        AddModToItemList((int)ItemLists.LoadedItemList, modItemLists[(int)ItemLists.LoadedItemList].GetItemCount(),
            currentModInfo);

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
        AddModToItemList((int)ItemLists.UnloadedItemList, modItemLists[(int)ItemLists.UnloadedItemList].GetItemCount(),
            currentModInfo);

        loadedItemList.RemoveItem(selectedItem);
    }

    /// <summary>
    ///   Adds a mod to one of the ItemList in modItemLists
    /// </summary>
    private void AddModToItemList(int itemListIndex, int newItemIndex, ModInfo currentModInfo)
    {
        var tempItemList = modItemLists[itemListIndex];

        tempItemList.AddItem(currentModInfo.DisplayName ?? currentModInfo.Name);
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
                tempItemList.SetItemTooltip(newItemIndex,
                    "This mod might not be compatible with this version of Thrive.");
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
                    tempItemList.SetItemTooltip(newItemIndex,
                        "This mod is not compatible with this version of Thrive.");
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
            MoveItem(modItemLists[(int)ItemLists.LoadedItemList], false,
                modItemLists[(int)ItemLists.LoadedItemList].GetSelectedItems()[0], 1);
        }
        else if (modItemLists[(int)ItemLists.UnloadedItemList].GetSelectedItems().Length > 0)
        {
            MoveItem(modItemLists[(int)ItemLists.UnloadedItemList], false,
                modItemLists[(int)ItemLists.UnloadedItemList].GetSelectedItems()[0], 1);
        }
    }

    private void OnCheckPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        var loadedItemList = modItemLists[(int)ItemLists.LoadedItemList];

        // Checks if there is anything that is going to be loaded first
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

    /// <summary>
    ///   Turns the result from a check into a string of the error and how to fix it
    /// </summary>
    private string CheckResultToString(int[] checkResult, ItemList list)
    {
        var result = string.Empty;

        // The mod that is causing the error
        var offendingMod = new ModInfo();

        // The reason why the mod is causing an error
        ModInfo otherMod;

        if (checkResult.Length > 1)
        {
            offendingMod = (ModInfo)list.GetItemMetadata(checkResult[1]);
        }

        switch (checkResult[0])
        {
            default:
                result = "The mod list has no errors and is valid.";
                break;
            case (int)ModLoader.CheckErrorStatus.IncompatibleVersion:
                result += "The '" + offendingMod.Name + "' mod is incompatible with this version of Thrive.";
                break;
            case (int)ModLoader.CheckErrorStatus.DependencyNotFound:
                result += "The '" + offendingMod.Name + "' mod is dependent on the '" +
                    offendingMod.Dependencies[checkResult[2]] + "' mod.\n";
                result += "Add that mod to the mod loader to fix this error.";
                break;
            case (int)ModLoader.CheckErrorStatus.InvalidDependencyOrder:
                otherMod = (ModInfo)list.GetItemMetadata(checkResult[2]);
                result += "The '" + offendingMod.Name + "' mod is dependent on the '" + otherMod.Name + "' mod.\n";
                result += "Load the '" + offendingMod.Name + "' mod after the '" + otherMod.Name +
                    "' mod to fix this error.";
                break;
            case (int)ModLoader.CheckErrorStatus.IncompatibleMod:
                otherMod = (ModInfo)list.GetItemMetadata(checkResult[2]);
                result += "The '" + offendingMod.Name + "' mod is incompatible with the '" + otherMod.Name + "' mod.\n";
                result += "Remove the '" + otherMod.Name + "' mod to fix this error.";
                break;
            case (int)ModLoader.CheckErrorStatus.InvalidLoadOrderBefore:
                otherMod = (ModInfo)list.GetItemMetadata(checkResult[2]);
                result += "The '" + offendingMod.Name + "' mod needs to be loaded before the '" + otherMod.Name +
                    "' mod.\n";
                result += "Load the '" + offendingMod.Name + "' mod before the '" + otherMod.Name +
                    "' to fix this error.";
                break;
            case (int)ModLoader.CheckErrorStatus.InvalidLoadOrderAfter:
                otherMod = (ModInfo)list.GetItemMetadata(checkResult[2]);
                result += "The '" + offendingMod.Name + "' mod needs to be loaded after the '" + otherMod.Name +
                    "' mod.\n";
                result += "Load the '" + offendingMod.Name + "' mod after the '" + otherMod.Name +
                    "' to fix this error.";
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

            // If there both 'load before' and 'load after' is going to display add a empty line between them
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
        ResetLoaderUI();

        for (int i = 0; i < modItemLists.Length; ++i)
        {
            modItemLists[i].Clear();
        }

        ReloadModLists();
    }

    private void VerifyConfigFileExist(ModInfo checkedModInfo)
    {
        if (checkedModInfo.ConfigurationList == null || checkedModInfo.ConfigurationList.Length < 1)
        {
            if (FileHelpers.Exists(checkedModInfo.Location + "/mod_config.json"))
            {
                checkedModInfo.ConfigurationList = loader.GetModConfigList(checkedModInfo);
            }
            else
            {
                GD.Print("Mod Missing Config File: " + checkedModInfo.Name);
                checkedModInfo.ConfigurationList = Array.Empty<ModConfigItemInfo>();
            }
        }
    }

    private void LoadReminderPopupConfirmed()
    {
        ResetLoaderUI();
        SceneManager.Instance.ReturnToMenu();
    }

    /// <summary>
    ///   This is the method that actually reset the game
    /// </summary>
    private void ResetGame()
    {
        GUICommon.Instance.PlayButtonPressSound();
        loader.ResetGame();

        for (int i = 0; i < modItemLists.Length; ++i)
        {
            modItemLists[i].Clear();
        }

        ResetLoaderUI();
        ReloadModLists();

        reloadReminderPopup.PopupCenteredShrink();
    }

    /// <summary>
    ///   This get the mods from the mod directory and updates the UnloadedItemList
    /// </summary>
    private void ReloadModLists(bool unloadedModList = true, bool autoLoadedModList = true, bool errorModList = true,
        bool configModList = true)
    {
        if (unloadedModList)
        {
            int index = 0;
            foreach (ModInfo currentModInfo in loader.LoadModList(true, false))
            {
                AddModToItemList((int)ItemLists.UnloadedItemList, index, currentModInfo);
                index++;
            }
        }

        if (autoLoadedModList)
        {
            int index = 0;

            foreach (ModInfo currentModInfo in loader.LoadModList(false, true, false))
            {
                AddModToItemList((int)ItemLists.AutoloadedItemlist, index, currentModInfo);

                index++;
            }
        }

        if (errorModList)
        {
            int index = 0;

            foreach (ModInfo currentModInfo in ModLoader.FailedToLoadMods)
            {
                AddModToItemList((int)ItemLists.ErrorItemList, index, currentModInfo);
                index++;
            }
        }

        if (configModList)
        {
            int index = 0;
            loader.LoadReloadedModsList();

            foreach (ModInfo currentModInfo in ModLoader.ReloadedMods)
            {
                if (currentModInfo.Configuration != null)
                {
                    AddModToItemList((int)ItemLists.ConfigItemList, index, currentModInfo);
                    index++;
                }
            }
        }
    }

    private void OnOpenModsFolderButtonPressed()
    {
        OS.ShellOpen(ProjectSettings.GlobalizePath(Constants.MOD_FOLDER));
    }

    private void OnClearConfigButtonPressed()
    {
        foreach (var currentItem in configContainer.GetChildren())
        {
            var currentItemInfo = currentItem as ModConfigItemInfo;
            currentItemInfo?.UpdateUI();
        }
    }

    private void OnResetConfigButtonPressed()
    {
        var configItemArray = configContainer.GetChildren();

        for (int i = 0; i < configItemArray.Count; i++)
        {
            var currentItemInfo = configItemArray[i] as ModConfigItemInfo;

            if (currentSelectedMod != null && currentItemInfo != null)
            {
                VerifyConfigFileExist(currentSelectedMod);
                currentItemInfo.Value = currentSelectedMod.ConfigurationList[i].Value;
                if (currentItemInfo.ID != null)
                {
                    currentSelectedMod.Configuration[currentItemInfo.ID] = currentItemInfo.Value;
                }

                currentItemInfo.UpdateUI();
            }
        }

        if (currentSelectedMod != null)
        {
            loader.SaveReloadedModsList();
        }
    }

    private void OnApplyButtonPressed()
    {
        var configItemArray = configContainer.GetChildren();
        for (int i = 0; i < configItemArray.Count; i++)
        {
            var currentItemInfo = configItemArray[i] as ModConfigItemInfo;
            if (currentItemInfo != null)
            {
                currentItemInfo.UpdateInternalValue();

                if (currentSelectedMod != null && currentItemInfo.ID != null)
                {
                    currentSelectedMod.Configuration[currentItemInfo.ID] = currentItemInfo.Value;
                }
            }
        }

        loader?.SaveReloadedModsList();
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
            var warningText = "The mods you want to load might cause errors.\n\n" +
                CheckResultToString(checkResult, loadedItemList);
            warningText += "\n\n Are you sure you want to load these mods?";
            loadWarningPopup.DialogText = warningText;
            loadWarningPopup.PopupCenteredShrink();
        }
    }

    private void LoadAllMods()
    {
        var loadedItemList = modItemLists[(int)ItemLists.LoadedItemList];

        loader.LoadModFromList(loadedItemList, true, !oneShotButton.Pressed, true);
        if (!oneShotButton.Pressed)
        {
            loader.SaveReloadedModsList();
        }

        GD.Print("All mods loaded");
        if (ModLoader.StartupMods.Count > 0)
        {
            loadReminderPopup.PopupCenteredShrink();
        }
        else
        {
            ResetLoaderUI();
            SceneManager.Instance.ReturnToMenu();
        }
    }
}
