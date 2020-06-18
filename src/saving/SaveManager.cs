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
        throw new System.NotImplementedException();
    }

    public static void RemoveExcessQuickSaves()
    {
        // throw new System.NotImplementedException();
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
