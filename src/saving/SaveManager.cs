/// <summary>
///   Class for managing the save files on disk
/// </summary>
public class SaveManager
{
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
        throw new System.NotImplementedException();
    }

    /// <summary>
    ///   Refreshes the list of saves this manager knows about
    /// </summary>
    public void RefreshSavesList()
    {
        throw new System.NotImplementedException();
    }
}
