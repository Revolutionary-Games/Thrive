using Godot;

/// <summary>
///   Display for an item in a build queue based on data from <see cref="IBuildQueueProgressItem"/>
/// </summary>
public partial class BuildQueueItemGUI : VBoxContainer
{
    [Export]
    public NodePath? ProgressBarPath;

    [Export]
    public NodePath NameLabelPath = null!;

#pragma warning disable CA2213
    private ProgressBar progressBar = null!;
    private Label nameLabel = null!;
#pragma warning restore CA2213

    private IBuildQueueProgressItem? progressItem;

    public override void _Ready()
    {
        progressBar = GetNode<ProgressBar>(ProgressBarPath);
        nameLabel = GetNode<Label>(NameLabelPath);

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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (ProgressBarPath != null)
            {
                ProgressBarPath.Dispose();
                NameLabelPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }
}
