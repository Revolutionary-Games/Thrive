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
    public NodePath LoadedItemListPath;

    [Export]
    public NodePath ErrorItemListPath;

    [Export]
    public NodePath ErrorLabelPath;

    [Export]
    public NodePath AutoLoadedItemListPath;

    [Export]
    public NodePath ConfirmationPopupPath;

    [Export]
    public NodePath AcceptPopupPath;

    // The array is used for getting all of the ItemList
    private ItemList[] modItemLists;

    // Labels For The Mod Info Box
    private Label modInfoName;
    private Label modInfoAuthor;
    private Label modInfoVersion;
    private RichTextLabel modInfoDescription;
    private TextureRect modInfoPreviewImage;

    private Label errorLabel;

    private ConfirmationDialog confirmationPopup;
    private AcceptDialog acceptPopup;

    private MarginContainer modInfoContainer;
    private MarginContainer errorInfoContainer;

    private ModLoader loader = new ModLoader();

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

        modInfoName = GetNode<Label>(ModInfoNamePath);
        modInfoAuthor = GetNode<Label>(ModInfoAuthorPath);
        modInfoVersion = GetNode<Label>(ModInfoVersionPath);
        modInfoDescription = GetNode<RichTextLabel>(ModInfoDescriptionPath);
        modInfoPreviewImage = GetNode<TextureRect>(PreviewImagePath);

        confirmationPopup = GetNode<ConfirmationDialog>(ConfirmationPopupPath);
        acceptPopup = GetNode<AcceptDialog>(AcceptPopupPath);
        ReloadModLists();
    }

    private void OnModSelected(int itemIndex, int selectedItemList)
    {
        ModInfo tempModInfo = (ModInfo)modItemLists[selectedItemList].GetItemMetadata(itemIndex);
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
            modInfoName.Text = tempModInfo.Name;
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
        EmitSignal(nameof(OnModLoaderClosed));
    }

    private void OnResetPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        confirmationPopup.PopupCenteredShrink();
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

        tempItemList.AddItem(currentModInfo.Name);
        tempItemList.SetItemMetadata(newItemIndex, currentModInfo);

        // Checks if a there is a icon image and if so set it
        if (currentModInfo.IconImage != null)
        {
            tempItemList.SetItemIcon(newItemIndex, currentModInfo.IconImage);
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

        acceptPopup.PopupCenteredShrink();
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

        loader.LoadModFromList(loadedItemList, true, true, true);
        loader.SaveAutoLoadedModsList();
        GD.Print("All mods loaded");
        SceneManager.Instance.ReturnToMenu();
    }
}
