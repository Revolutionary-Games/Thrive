using Godot;

/// <summary>
///   Partial class: Scene tree dumper
/// </summary>
public partial class DebugOverlays
{
    private const string SCENE_DUMP_FILE = "user://logs/scene_tree_dump.txt";

    public void DumpSceneTreeToFile(Node node)
    {
        using var file = FileAccess.Open(SCENE_DUMP_FILE, FileAccess.ModeFlags.Write);

        if (file == null)
        {
            GD.PrintErr("Cannot open file for writing scene tree");
            return;
        }

        DumpSceneTreeToFile(node, file, 0);

        file.Close();

        // TODO: maybe this should have a separate button?
#if DEBUG
        PrintOrphanedNodes();
#endif

        GD.Print("Scene tree dumped to \"", SCENE_DUMP_FILE, "\"");
    }

    private static void DumpSceneTreeToFile(Node node, FileAccess file, int indent)
    {
        file.StoreString($"{new string(' ', 2 * indent)}{node.GetType()}: {node.Name}\n");

        foreach (Node child in node.GetChildren())
            DumpSceneTreeToFile(child, file, indent + 1);
    }

    private static void PrintOrphanedNodes()
    {
        GD.Print("Orphaned nodes:");
        PrintOrphanNodes();
    }
}
