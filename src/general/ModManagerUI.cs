using Godot;

/// <summary>
///   Class managing the Mod Manager UI
/// </summary>
public class ModManagerUI : Control
{
    [Export]
    public NodePath UnloadedItemListPath;

    [Export]
    public NodePath ModInfoNamePath;

    [Export]
    public NodePath ModInfoAuthorPath;

    [Export]
    public NodePath ModInfoVersionPath;

    [Export]
    public NodePath ModInfoDescriptionPath;

    [Export]
    public NodePath LoadedItemListPath;

    [Export]
    public NodePath ConfirmationPopupPath;

    [Export]
    public NodePath AcceptPopupPath;

    private ItemList unloadedItemList;
    private ItemList loadedItemList;

    // Labels For The Mod Info Box
    private Label modInfoName;
    private Label modInfoAuthor;
    private Label modInfoVersion;
    private Label modInfoDescription;

    private ConfirmationDialog confirmationPopup;
    private AcceptDialog acceptPopup;

    private ModLoader loader = new ModLoader();

    [Signal]
    public delegate void OnModLoaderClosed();

    public override void _Ready()
    {
        unloadedItemList = GetNode<ItemList>(UnloadedItemListPath);
        loadedItemList = GetNode<ItemList>(LoadedItemListPath);

        modInfoName = GetNode<Label>(ModInfoNamePath);
        modInfoAuthor = GetNode<Label>(ModInfoAuthorPath);
        modInfoVersion = GetNode<Label>(ModInfoVersionPath);
        modInfoDescription = GetNode<Label>(ModInfoDescriptionPath);

        confirmationPopup = GetNode<ConfirmationDialog>(ConfirmationPopupPath);
        acceptPopup = GetNode<AcceptDialog>(AcceptPopupPath);

        ReloadUnloadedModList();
    }

    private void OnModSelected(int index, bool unloadedSelected)
    {
        ModInfo tempModInfo;
        if (unloadedSelected)
        {
            tempModInfo = (ModInfo)unloadedItemList.GetItemMetadata(index);
            loadedItemList.UnselectAll();
        }
        else
        {
            tempModInfo = (ModInfo)loadedItemList.GetItemMetadata(index);
            unloadedItemList.UnselectAll();
        }

        modInfoName.Text = tempModInfo.Name;
        modInfoAuthor.Text = tempModInfo.Author;
        modInfoVersion.Text = tempModInfo.Version;
        modInfoDescription.Text = tempModInfo.Description;
    }

    private void OnMoveToLoadPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        if (unloadedItemList.GetSelectedItems().Length <= 0)
        {
            return;
        }

        var selectedItem = unloadedItemList.GetSelectedItems()[0];
        ModInfo currentModInfo = (ModInfo)unloadedItemList.GetItemMetadata(selectedItem);

        loadedItemList.AddItem(currentModInfo.Name);
        loadedItemList.SetItemMetadata(loadedItemList.GetItemCount() - 1, currentModInfo);
        unloadedItemList.RemoveItem(selectedItem);
    }

    private void OnMoveToUnloadPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        if (loadedItemList.GetSelectedItems().Length <= 0)
        {
            return;
        }

        var selectedItem = loadedItemList.GetSelectedItems()[0];
        ModInfo currentModInfo = (ModInfo)loadedItemList.GetItemMetadata(selectedItem);

        unloadedItemList.AddItem(currentModInfo.Name);
        unloadedItemList.SetItemMetadata(unloadedItemList.GetItemCount() - 1, currentModInfo);
        loadedItemList.RemoveItem(selectedItem);
    }

    private void OnBackPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(nameof(OnModLoaderClosed));
    }

    private void OnMoveUpPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        if (loadedItemList.GetSelectedItems().Length > 0)
        {
            MoveItem(loadedItemList, true, loadedItemList.GetSelectedItems()[0]);
        }
        else if (unloadedItemList.GetSelectedItems().Length > 0)
        {
            MoveItem(unloadedItemList, true, unloadedItemList.GetSelectedItems()[0]);
        }
    }

    private void OnMoveDownPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        if (loadedItemList.GetSelectedItems().Length > 0)
        {
            MoveItem(loadedItemList, false, loadedItemList.GetSelectedItems()[0]);
        }
        else if (unloadedItemList.GetSelectedItems().Length > 0)
        {
            MoveItem(unloadedItemList, false, unloadedItemList.GetSelectedItems()[0]);
        }
    }

    private void MoveItem(ItemList list, bool moveUp, int currentIndex)
    {
        if (moveUp)
        {
            if (currentIndex == 0)
            {
                return;
            }

            list.MoveItem(currentIndex, currentIndex - 1);
        }
        else
        {
            if (currentIndex == list.GetItemCount() - 1)
            {
                return;
            }

            list.MoveItem(currentIndex, currentIndex + 1);
        }
    }

    private void OnResetPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        confirmationPopup.PopupCenteredMinsize();
    }

    /// <summary>
    ///   This is the method that actually reset the game
    /// </summary>
    private void ResetGame()
    {
        GUICommon.Instance.PlayButtonPressSound();
        loader.ResetGame();

        unloadedItemList.Clear();
        loadedItemList.Clear();
        ReloadUnloadedModList();

        acceptPopup.PopupCenteredMinsize();
    }

    private void OnRefreshPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        unloadedItemList.Clear();
        loadedItemList.Clear();

        ReloadUnloadedModList();
    }

    /// <summary>
    ///   This get the mods from the mod directory and updates the ItemList
    /// </summary>
    private void ReloadUnloadedModList()
    {
        int index = 0;
        foreach (ModInfo currentModInfo in loader.LoadModList(false))
        {
            unloadedItemList.AddItem(currentModInfo.Name);
            unloadedItemList.SetItemMetadata(index, currentModInfo);
            index++;
        }
    }

    private void OnLoadPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
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
