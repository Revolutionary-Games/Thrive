using Godot;

/// <summary>
///   Display for an item in a build queue based on data from <see cref="IBuildQueueProgressItem"/>
/// </summary>
public partial class BuildQueueItemGUI : VBoxContainer
{
#pragma warning disable CA2213
    [Export]
    private ProgressBar progressBar = null!;
    [Export]
    private Label nameLabel = null!;
#pragma warning restore CA2213

    private IBuildQueueProgressItem? progressItem;

    public override void _Ready()
    {
        // TODO: a cancel button?
    }

    public override void _Process(double delta)
    {
        if (progressItem == null)
            return;

        progressBar.Value = progressItem.Progress;
    }

    public void Display(IBuildQueueProgressItem buildQueueItemData)
    {
        progressItem = buildQueueItemData;

        nameLabel.Text = progressItem.ItemName;
    }
}
