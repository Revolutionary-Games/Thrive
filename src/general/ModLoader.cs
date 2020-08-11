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

    private ItemList unloadedItemList;
    private ItemList loadedItemList;

    // Labels For The Mod Info Box
    private Label modInfoName;
    private Label modInfoAuthor;
    private Label modInfoVersion;
    private Label modInfoDescription;

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

        DirectoryInfo modFolder = Directory.CreateDirectory($"{Directory.GetCurrentDirectory()}\\mods");

        foreach (DirectoryInfo currentMod in modFolder.EnumerateDirectories())
        {
            if (!File.Exists(currentMod.FullName + "/mod_info.json"))
            {
                continue;
            }

            var currentModInfo =
                JsonConvert.DeserializeObject<ModInfo>(ReadJSONFile(currentMod.FullName + "/mod_info.json"));
            currentModInfo.Location = currentMod.FullName;
            modList.Add(currentModInfo);
            unloadedItemList.AddItem(currentModInfo.ModName);
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

    private void OnUnloadedModSelected(int index)
    {
        var tempModInfo = modList[index];
        modInfoName.Text = tempModInfo.ModName;
        modInfoAuthor.Text = "Author: " + tempModInfo.Author;
        modInfoVersion.Text = "Version: " + tempModInfo.Version;
        modInfoDescription.Text = tempModInfo.Description;
        modInfoDescription.Text = tempModInfo.Description;
        loadedItemList.UnselectAll();
    }

    private void OnLoadedModSelected(int index)
    {
        var tempModInfo = loadedModList[index];
        modInfoName.Text = tempModInfo.ModName;
        modInfoAuthor.Text = "Author: " + tempModInfo.Author;
        modInfoVersion.Text = "Version: " + tempModInfo.Version;
        modInfoDescription.Text = tempModInfo.Description;
        modInfoDescription.Text = tempModInfo.Description;
        unloadedItemList.UnselectAll();
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
        loadedItemList.AddItem(modList[selectedItem].ModName);
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
        unloadedItemList.AddItem(loadedModList[selectedItem].ModName);
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
            currentModInfo.Location = currentMod.FullName;
            modList.Add(currentModInfo);
            unloadedItemList.AddItem(currentModInfo.ModName);
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
                GD.Print("Fail to find mod file: " + currentMod.ModName);
                continue;
            }

            if (ProjectSettings.LoadResourcePack(currentMod.Location + "/mod.pck", true))
            {
                GD.Print("Loaded mod: " + currentMod.ModName);
            }
            else
            {
                GD.Print("Failed to load mod: " + currentMod.ModName);
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
}
