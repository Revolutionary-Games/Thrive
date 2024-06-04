using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Wiki-style info box for a stage.
/// </summary>
public partial class StageInfoBox : PanelContainer
{
    [Export]
    public NodePath? NameLabelPath;

    [Export]
    public NodePath GameplayTypePath = null!;

    [Export]
    public NodePath PreviousStagePath = null!;

    [Export]
    public NodePath NextStagePath = null!;

    [Export]
    public NodePath EditorsPath = null!;

    [Export]
    public NodePath InternalNameLabelPath = null!;

    private GameWiki.Page page = null!;

#pragma warning disable CA2213
    private Label nameLabel = null!;
    private Label gameplayType = null!;
    private Label previousStage = null!;
    private Label nextStage = null!;
    private Label editors = null!;
    private Label internalNameLabel = null!;
#pragma warning restore CA2213

    public GameWiki.Page Page
    {
        get => page;
        set
        {
            page = value;
            UpdateValues();
        }
    }

    public override void _Ready()
    {
        base._Ready();

        nameLabel = GetNode<Label>(NameLabelPath);
        gameplayType = GetNode<Label>(GameplayTypePath);
        previousStage = GetNode<Label>(PreviousStagePath);
        nextStage = GetNode<Label>(NextStagePath);
        editors = GetNode<Label>(EditorsPath);
        internalNameLabel = GetNode<Label>(InternalNameLabelPath);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (NameLabelPath != null)
            {
                NameLabelPath.Dispose();
                GameplayTypePath.Dispose();
                PreviousStagePath.Dispose();
                NextStagePath.Dispose();
                EditorsPath.Dispose();
                InternalNameLabelPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    /// <summary>
    ///   Sets all text values in the table.
    /// </summary>
    private void UpdateValues()
    {
        Dictionary<string, string> infoboxData = Page.InfoboxData.ToDictionary(f => f.InfoboxKey, f => f.InfoboxValue);

        nameLabel.Text = Page.Name;
        gameplayType.Text = infoboxData["INFO_BOX_GAMEPLAY_TYPE"];
        previousStage.Text = infoboxData["INFO_BOX_PREVIOUS_STAGE"];
        nextStage.Text = infoboxData["INFO_BOX_NEXT_STAGE"];
        editors.Text = infoboxData["INFO_BOX_EDITORS"];
        internalNameLabel.Text = Page.InternalName;
    }
}
