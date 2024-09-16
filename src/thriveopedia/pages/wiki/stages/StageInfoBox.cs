using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Wiki-style info box for a stage.
/// </summary>
public partial class StageInfoBox : PanelContainer
{
    private GameWiki.Page page = null!;

#pragma warning disable CA2213
    [Export]
    private Label nameLabel = null!;

    [Export]
    private Label gameplayType = null!;

    [Export]
    private Label previousStage = null!;

    [Export]
    private Label nextStage = null!;

    [Export]
    private Label editors = null!;

    [Export]
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

    /// <summary>
    ///   Sets all text values in the table.
    /// </summary>
    private void UpdateValues()
    {
        nameLabel.Text = Page.Name;
        gameplayType.Text = Page.GetInfoBoxData("INFO_BOX_GAMEPLAY_TYPE");
        previousStage.Text = Page.GetInfoBoxData("INFO_BOX_PREVIOUS_STAGE");
        nextStage.Text = Page.GetInfoBoxData("INFO_BOX_NEXT_STAGE");
        editors.Text = Page.GetInfoBoxData("INFO_BOX_EDITORS");
        internalNameLabel.Text = Page.InternalName;
    }
}
