using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Class for managing the save files on disk
/// </summary>
public class SaveManager
{
    public enum SaveOrder
    {
        LastModifiedFirst,
        FileSystem,
    }

    public static void RemoveExcessSaves()
    {
        RemoveExcessAutoSaves();
        RemoveExcessQuickSaves();
    }

    public static void RemoveExcessAutoSaves()
    {
        int maxAutoSaves = Settings.Instance.MaxAutoSaves;
        int autoSaveCount = 0;

        List<string> autoSaveNames = new List<string>();
        List<string> allSaveNames = CreateListOfSaves();

        allSaveNames.Reverse();

        foreach (var save in allSaveNames)
        {
            if (save.StartsWith("auto_save", StringComparison.CurrentCulture))
            {
                autoSaveNames.Add(save);
                ++autoSaveCount;
            }

            if (autoSaveCount >= maxAutoSaves && autoSaveNames.Count > 0)
            {
                SaveHelper.DeleteSave(autoSaveNames[0]);
                autoSaveNames.RemoveAt(0);
                --autoSaveCount;
            }
        }
    }

    public static void RemoveExcessQuickSaves()
    {
        int maxQuickSaves = Settings.Instance.MaxQuickSaves;
        int quickSaveCount = 0;

        List<string> quickSaveNames = new List<string>();
        List<string> allSaveNames = CreateListOfSaves();

        allSaveNames.Reverse();

        foreach (var save in allSaveNames)
        {
            if (save.StartsWith("quick_save", StringComparison.CurrentCulture))
            {
                quickSaveNames.Add(save);
                ++quickSaveCount;
            }

            if (quickSaveCount >= maxQuickSaves && quickSaveNames.Count > 0)
            {
                SaveHelper.DeleteSave(quickSaveNames[0]);
                quickSaveNames.RemoveAt(0);
                --quickSaveCount;
            }
        }
    }

    /// <summary>
    ///   Returns a list of all saves
    /// </summary>
    /// <returns>The list of save names</returns>
    public static List<string> CreateListOfSaves(SaveOrder order = SaveOrder.LastModifiedFirst)
    {
        var result = new List<string>();

        using (var directory = new Directory())
        {
            if (!directory.DirExists(Constants.SAVE_FOLDER))
                return result;

            directory.Open(Constants.SAVE_FOLDER);
            directory.ListDirBegin(true, true);

            while (true)
            {
                var filename = directory.GetNext();

                if (string.IsNullOrEmpty(filename))
                    break;

                if (!filename.EndsWith(Constants.SAVE_EXTENSION, StringComparison.Ordinal))
                    continue;

                result.Add(filename);
            }

            directory.ListDirEnd();
        }

        switch (order)
        {
            case SaveOrder.LastModifiedFirst:
            {
                using (var file = new File())
                {
                    result = result.OrderByDescending(item =>
                        file.GetModifiedTime(PathUtils.Join(Constants.SAVE_FOLDER, item))).ToList();
                }

                break;
            }

            default:
                break;
        }

        return result;
    }

    /// <summary>
    ///   Counts the total number of saves and how many bytes they take up
    /// </summary>
    public static (int count, long diskSpace) CountSaves()
    {
        int count = 0;
        long totalSize = 0;

        using (var file = new File())
        {
            foreach (var save in CreateListOfSaves())
            {
                file.Open(PathUtils.Join(Constants.SAVE_FOLDER, save), File.ModeFlags.Read);
                ++count;
                totalSize += file.GetLen();
            }
        }

        return (count, totalSize);
    }

    /// <summary>
    ///   Refreshes the list of saves this manager knows about
    /// </summary>
    public void RefreshSavesList()
    {
        throw new System.NotImplementedException();
    }
}
