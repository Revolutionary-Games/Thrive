using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Godot;
using Newtonsoft.Json;
using Directory = System.IO.Directory;
using File = System.IO.File;

public class ModLoader : Control
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

    private ItemList unloadedItemList;
    private ItemList loadedItemList;

    // Labels For The Mod Info Box
    private Label modInfoName;
    private Label modInfoAuthor;
    private Label modInfoVersion;
    private Label modInfoDescription;

    private ConfirmationDialog confirmationPopup;

    private List<ModInfo> modList = new List<ModInfo>();
    private List<ModInfo> loadedModList = new List<ModInfo>();

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

        DirectoryInfo modFolder = Directory.CreateDirectory($"{Directory.GetCurrentDirectory()}\\mods");
        foreach (DirectoryInfo currentMod in modFolder.EnumerateDirectories())
        {
            if (!File.Exists(currentMod.FullName + "/mod_info.json"))
            {
                continue;
            }

            var currentModInfo =
                JsonConvert.DeserializeObject<ModInfo>(ReadJSONFile(currentMod.FullName + "/mod_info.json"));

            if (currentModInfo.AutoLoad)
            {
                continue;
            }

            currentModInfo.Location = currentMod.FullName;
            modList.Add(currentModInfo);
            unloadedItemList.AddItem(currentModInfo.Name);
        }
    }

    // Copied From The PauseMenu.cs
    private static string ReadJSONFile(string path)
    {
        using (var file = new Godot.File())
        {
            file.Open(path, Godot.File.ModeFlags.Read);
            var result = file.GetAsText();

            // This might be completely unnecessary
            file.Close();

            return result;
        }
    }

    private void OnModSelected(int index, bool unloadedSelected)
    {
        ModInfo tempModInfo;
        if (unloadedSelected)
        {
            tempModInfo = modList[index];
            loadedItemList.UnselectAll();
        }
        else
        {
            tempModInfo = loadedModList[index];
            unloadedItemList.UnselectAll();
        }

        modInfoName.Text = tempModInfo.Name;
        modInfoAuthor.Text = tempModInfo.Author;
        modInfoVersion.Text = tempModInfo.Version;
        modInfoDescription.Text = tempModInfo.Description;
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
        loadedModList.Add(modList[selectedItem]);
        loadedItemList.AddItem(modList[selectedItem].Name);
        modList.RemoveAt(selectedItem);
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
        modList.Add(loadedModList[selectedItem]);
        unloadedItemList.AddItem(loadedModList[selectedItem].Name);
        loadedModList.RemoveAt(selectedItem);
        loadedItemList.RemoveItem(selectedItem);
    }

    private void OnRefreshPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        modList.Clear();
        loadedModList.Clear();
        unloadedItemList.Clear();
        loadedItemList.Clear();
        DirectoryInfo modFolder = Directory.CreateDirectory($"{Directory.GetCurrentDirectory()}\\mods");
        foreach (DirectoryInfo currentMod in modFolder.EnumerateDirectories())
        {
            if (!File.Exists(currentMod.FullName + "/mod_info.json"))
            {
                continue;
            }

            var currentModInfo =
                JsonConvert.DeserializeObject<ModInfo>(ReadJSONFile(currentMod.FullName + "/mod_info.json"));

            if (currentModInfo.AutoLoad)
            {
                continue;
            }

            currentModInfo.Location = currentMod.FullName;
            modList.Add(currentModInfo);
            unloadedItemList.AddItem(currentModInfo.Name);
        }
    }

    private void OnLoadPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        if (loadedModList.Count <= 0)
        {
            return;
        }

        foreach (ModInfo currentMod in loadedModList)
        {
            if (string.IsNullOrEmpty(currentMod.Dll))
            {
                if (File.Exists(currentMod.Location + "/" + currentMod.Dll))
                {
                    Assembly.LoadFile(currentMod.Location + "/" + currentMod.Dll);
                }
            }

            if (!File.Exists(currentMod.Location + "/mod.pck"))
            {
                GD.Print("Fail to find mod file: " + currentMod.Name);
                continue;
            }

            if (ProjectSettings.LoadResourcePack(currentMod.Location + "/mod.pck", true))
            {
                GD.Print("Loaded mod: " + currentMod.Name);
            }
            else
            {
                GD.Print("Failed to load mod: " + currentMod.Name);
            }
        }

        GD.Print("All mods loaded");
        SceneManager.Instance.ReturnToMenu();
    }

    private void OnBackPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(nameof(OnModLoaderClosed));
    }

    private void OnResetPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        confirmationPopup.PopupCentered();
    }

    private void ResetGame()
    {
        GUICommon.Instance.PlayButtonPressSound();
        if (!File.Exists(Directory.GetCurrentDirectory() + "/Thrive.pck"))
        {
            GD.Print("Fail to find Thrive");
            return;
        }

        if (ProjectSettings.LoadResourcePack(Directory.GetCurrentDirectory() + "/Thrive.pck", true))
        {
            GD.Print("Reset successful");
        }
        else
        {
            GD.Print("Reset failed");
        }

        SceneManager.Instance.ReturnToMenu();
    }

    private void OnMoveUpPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        if (loadedItemList.GetSelectedItems().Length > 0)
        {
            var selectedItemIndex = loadedItemList.GetSelectedItems()[0];
            if (selectedItemIndex == 0)
            {
                return;
            }

            loadedItemList.MoveItem(selectedItemIndex, selectedItemIndex - 1);
            selectedItemIndex = loadedItemList.GetSelectedItems()[0];
            MoveItem(loadedModList, selectedItemIndex + 1, selectedItemIndex);
        }
        else if (unloadedItemList.GetSelectedItems().Length > 0)
        {
            var selectedItemIndex = unloadedItemList.GetSelectedItems()[0];

            if (unloadedItemList.GetSelectedItems()[0] == 0)
            {
                return;
            }

            unloadedItemList.MoveItem(selectedItemIndex, selectedItemIndex - 1);
            selectedItemIndex = unloadedItemList.GetSelectedItems()[0];
            MoveItem(modList, selectedItemIndex + 1, selectedItemIndex);
        }
    }

    private void OnMoveDownPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        if (loadedItemList.GetSelectedItems().Length > 0)
        {
            var selectedItemIndex = loadedItemList.GetSelectedItems()[0];
            if (selectedItemIndex == loadedItemList.GetItemCount() - 1)
            {
                return;
            }

            loadedItemList.MoveItem(selectedItemIndex, selectedItemIndex + 1);
            selectedItemIndex = loadedItemList.GetSelectedItems()[0];
            MoveItem(loadedModList, selectedItemIndex, selectedItemIndex - 1);
        }
        else if (unloadedItemList.GetSelectedItems().Length > 0)
        {
            var selectedItemIndex = unloadedItemList.GetSelectedItems()[0];
            if (selectedItemIndex == unloadedItemList.GetItemCount() - 1)
            {
                return;
            }

            unloadedItemList.MoveItem(selectedItemIndex, selectedItemIndex + 1);
            selectedItemIndex = unloadedItemList.GetSelectedItems()[0];
            MoveItem(modList, selectedItemIndex, selectedItemIndex - 1);
        }
    }

    private void MoveItem(List<ModInfo> list, bool moveUp, int currentIndex)
    {
        var oldIndex = currentIndex + (moveUp ? 1 : 0);
        var newIndex = currentIndex + (moveUp ? 0 : -1);
        var item = list[oldIndex];
        list.RemoveAt(oldIndex);

        if (newIndex > oldIndex)
        {
            newIndex--;
        }

        list.Insert(newIndex, item);
    }
}
