using System.Collections.Generic;
using System.Reflection;
using Godot;
using Newtonsoft.Json;

public class ModLoader : Reference
{
    /// <summary>
    ///   Mods that are loaded in by the auto mod loader
    ///   so the player don't have to reload them every time they start the game
    /// </summary>
    public static List<ModInfo> AutoLoadedMods { get; set; } = new List<ModInfo>();

    /// <summary>
    ///   Mods that have already been loaded including autoloaded mods too
    /// </summary>
    public static List<ModInfo> LoadedMods { get; private set; } = new List<ModInfo>();

    /// <summary>
    ///   Mods that have failed to load
    /// </summary>
    public static List<ModInfo> FailedToLoadMods { get; set; } = new List<ModInfo>();

    /// <summary>
    ///   This fetches the mods from the mod directory and then returns it
    /// </summary>
    public List<ModInfo> LoadModList(bool ignoreAutoload = true)
    {
        FileHelpers.MakeSureDirectoryExists(Constants.MOD_FOLDER);
        var modFolder = new Directory();

        modFolder.Open(Constants.MOD_FOLDER);
        modFolder.ListDirBegin(true, true);

        List<ModInfo> modList = new List<ModInfo>();
        var currentMod = modFolder.GetNext();

        while (true)
        {
            if (string.IsNullOrEmpty(currentMod))
            {
                break;
            }

            var currentModInfo = GetModInfo(PathUtils.Join(Constants.MOD_FOLDER, currentMod));

            // Checks if the retrieval of ModInfo was valid
            if (currentModInfo.Name == null)
            {
                continue;
            }

            // Checks if it should ignore autoloaded mods
            if (currentModInfo.AutoLoad && ignoreAutoload)
            {
                continue;
            }

            modList.Add(currentModInfo);
            currentMod = modFolder.GetNext();
        }

        return modList;
    }

    /// <summary>
    ///   This get a ModInfo from a directory and then returns it
    /// </summary>
    public ModInfo GetModInfo(string location)
    {
        var file = new File();
        if (!file.FileExists(location + "/mod_info.json"))
        {
            return new ModInfo();
        }

        var currentModInfo =
            JsonConvert.DeserializeObject<ModInfo>(ReadJSONFile(location + "/mod_info.json"));

        currentModInfo.Location = location;
        if (file.FileExists(location + "/icon.png"))
        {
            var iconTexture = new ImageTexture();
            var iconImage = new Image();
            iconImage.Load(location + "/icon.png");
            iconTexture.CreateFromImage(iconImage);
            currentModInfo.IconImage = iconTexture;
        }

        if (file.FileExists(location + "/preview.png"))
        {
            var previewTexture = new ImageTexture();
            var previewImage = new Image();
            previewImage.Load(location + "/preview.png");
            previewTexture.CreateFromImage(previewImage);
            currentModInfo.PreviewImage = previewTexture;
        }

        return currentModInfo;
    }

    private static string ReadJSONFile(string path)
    {
        using (var file = new File())
        {
            file.Open(path, File.ModeFlags.Read);
            var result = file.GetAsText();

            // This might be completely unnecessary
            file.Close();

            return result;
        }
    }
}
