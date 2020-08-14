using System.IO;
using System.Reflection;
using Godot;
using Newtonsoft.Json;
using Directory = System.IO.Directory;
using File = System.IO.File;

public class AutoModLoader : Node
{
    private AutoModLoader()
    {
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
                    if (string.IsNullOrEmpty(currentModInfo.Dll))
                    {
                        if (File.Exists(currentMod.FullName + "/" + currentModInfo.Dll))
                        {
                            Assembly.LoadFile(currentMod.FullName + "/" + currentModInfo.Dll);
                        }
                    }

                    if (!File.Exists(currentMod.FullName + "/mod.pck"))
                    {
                        GD.Print("Fail to find mod file: " + currentModInfo.ModName);
                        continue;
                    }

                    if (ProjectSettings.LoadResourcePack(currentMod.FullName + "/mod.pck", true))
                    {
                        GD.Print("Loaded mod: " + currentModInfo.ModName);
                    }
                    else
                    {
                        GD.Print("Failed to load mod: " + currentModInfo.ModName);
                    }
            }
        }

        QueueFree();
   }

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
}
