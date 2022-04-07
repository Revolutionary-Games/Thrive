using Godot;
using Newtonsoft.Json;

public class EarlyMulticellularEditorGUI : Control
{
    [JsonProperty]
    private EditorTab selectedEditorTab = EditorTab.Report;

    public override void _Ready()
    {
    }

    public override void _Process(float delta)
    {
    }

    private void SetEditorTab(EditorTab tab)
    {
        selectedEditorTab = tab;

        ApplyEditorTab();
    }

    private void ApplyEditorTab()
    {
        // TODO: implement
    }
}
