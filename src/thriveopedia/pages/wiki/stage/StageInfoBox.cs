using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
///   Wiki-style info box for a stage.
/// </summary>
public class StageInfoBox : PanelContainer
{
    [Export]
    public NodePath? NamePath;

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

#pragma warning disable CA2213
    private Label nameLabel = null!;
    private Label gameplayType = null!;
    private Label previousStage = null!;
    private Label nextStage = null!;
    private Label editors = null!;
    private Label internalNameLabel = null!;
#pragma warning restore CA2213

    public override void _Ready()
    {
        base._Ready();

        nameLabel = GetNode<Label>(NamePath);
        gameplayType = GetNode<Label>(GameplayTypePath);
        previousStage = GetNode<Label>(PreviousStagePath);
        nextStage = GetNode<Label>(NextStagePath);
        internalNameLabel = GetNode<Label>(InternalNameLabelPath);

        UpdateValues();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (NamePath != null)
            {
                NamePath.Dispose();
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
    ///   Sets all textures and text values in the table.
    /// </summary>
    private void UpdateValues()
    {
        icon.Texture = GD.Load<Texture>(organelle.IconPath);

        var opaque = new Color(1, 1, 1, 1);
        var translucent = new Color(1, 1, 1, 0.25f);

        var hasProcesses = organelle.Processes != null && organelle.Processes.Count > 0;
        processesLabel.Modulate = hasProcesses ? opaque : translucent;
        processesLabel.Text = hasProcesses ?
            organelle.Processes!.Keys
                .Select(p => SimulationParameters.Instance.GetBioProcess(p).Name)
                .Aggregate((a, b) => a + "\n" + b) :
            TranslationServer.Translate("NONE");

        var hasEnzymes = organelle.Enzymes.Count > 0;
        enzymesLabel.Modulate = hasEnzymes ? opaque : translucent;
        enzymesLabel.Text = hasEnzymes ?
            organelle.Enzymes
                .Where(e => e.Value > 0)
                .Select(e => e.Key.Name)
                .Aggregate((a, b) => a + "\n" + b) :
            TranslationServer.Translate("NONE");

        var hasUpgrades = organelle.AvailableUpgrades.Count > 0;
        upgradesLabel.Modulate = hasUpgrades ? opaque : translucent;
        upgradesLabel.Text = hasUpgrades ?
            organelle.AvailableUpgrades
                .Where(u => u.Key != "none")
                .Select(u => u.Value.Name)
                .Aggregate((a, b) => a + "\n" + b) :
            TranslationServer.Translate("NONE");

        nameLabel.Text = organelle.Name;
        costLabel.Text = organelle.MPCost.ToString(CultureInfo.CurrentCulture);

        // TODO: make this make more sense now that we only have physics density to use
        if (organelle.RelativeDensityVolume > 0)
        {
            massLabel.Text = (organelle.Density * organelle.RelativeDensityVolume).ToString(CultureInfo.CurrentCulture);
        }
        else
        {
            massLabel.Text = organelle.Density.ToString(CultureInfo.CurrentCulture);
        }

        sizeLabel.Text = organelle.HexCount.ToString(CultureInfo.CurrentCulture);
        osmoregulationCostLabel.Text = organelle.HexCount.ToString(CultureInfo.CurrentCulture);
        storageLabel.Text = (organelle.Components.Storage?.Capacity ?? 0).ToString(CultureInfo.CurrentCulture);
        internalNameLabel.Text = organelle.InternalName;

        requiresNucleusIcon.Texture = GUICommon.Instance.GetRequirementFulfillmentIcon(organelle.RequiresNucleus);
        uniqueIcon.Texture = GUICommon.Instance.GetRequirementFulfillmentIcon(organelle.Unique);
    }
}
