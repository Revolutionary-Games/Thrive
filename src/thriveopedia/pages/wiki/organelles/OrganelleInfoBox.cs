using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
///   Wiki-style info box for an organelle.
/// </summary>
public partial class OrganelleInfoBox : PanelContainer
{
    private OrganelleDefinition? organelle;

#pragma warning disable CA2213
    [Export]
    private Label nameLabel = null!;
    [Export]
    private TextureRect icon = null!;
    [Export]
    private TextureRect? model;
    [Export]
    private Label costLabel = null!;
    [Export]
    private TextureRect requiresNucleusIcon = null!;
    [Export]
    private Label processesLabel = null!;
    [Export]
    private Label enzymesLabel = null!;
    [Export]
    private Label massLabel = null!;
    [Export]
    private Label sizeLabel = null!;
    [Export]
    private Label osmoregulationCostLabel = null!;
    [Export]
    private Label storageLabel = null!;
    [Export]
    private TextureRect uniqueIcon = null!;
    [Export]
    private Label upgradesLabel = null!;
    [Export]
    private Label internalNameLabel = null!;

    private Texture2D? modelTexture;
    private Texture2D? modelLoadingTexture;
#pragma warning restore CA2213

    private IImageTask? modelImageTask;
    private bool finishedLoadingModelImage;

    /// <summary>
    ///   The organelle this info box displays data for.
    /// </summary>
    public OrganelleDefinition? Organelle
    {
        get => organelle;
        set
        {
            organelle = value;
            UpdateValues();
            UpdateModelImage();
        }
    }

    public Texture2D? ModelTexture
    {
        get => modelTexture;
        set
        {
            modelTexture = value;
            UpdateModelImage();
        }
    }

    public override void _Ready()
    {
        base._Ready();

        modelLoadingTexture = GD.Load<Texture2D>("res://assets/textures/gui/bevel/IconGenerating.png");

        UpdateModelImage();
        UpdateValues();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (organelle == null || finishedLoadingModelImage)
            return;

        if (!organelle.TryGetGraphicsScene(null, out var sceneWithModelInfo))
            return;

        if (modelImageTask != null)
        {
            if (modelImageTask.Finished)
            {
                ModelTexture = modelImageTask.FinalImage;
                finishedLoadingModelImage = true;
            }

            return;
        }

        var hash = GalleryCardModel.ModelPreview.HashForPath(sceneWithModelInfo.LoadedScene.ResourcePath);

        modelImageTask = PhotoStudio.Instance.TryGetFromCache(hash) ?? PhotoStudio.Instance.GenerateImage(
            new GalleryCardModel.ModelPreview(sceneWithModelInfo.LoadedScene.ResourcePath,
                sceneWithModelInfo.ModelPath));
        ModelTexture = modelLoadingTexture;
    }

    /// <summary>
    ///   Sets all textures and text values in the table.
    /// </summary>
    private void UpdateValues()
    {
        if (organelle == null)
            return;

        icon.Texture = GD.Load<Texture2D>(organelle.IconPath);

        var opaque = new Color(1, 1, 1, 1);
        var translucent = new Color(1, 1, 1, 0.25f);

        var hasProcesses = organelle.Processes != null && organelle.Processes.Count > 0;
        processesLabel.Modulate = hasProcesses ? opaque : translucent;
        processesLabel.Text = hasProcesses ?
            organelle.Processes!.Keys
                .Select(p => SimulationParameters.Instance.GetBioProcess(p).Name)
                .Aggregate((a, b) => a + "\n" + b) :
            Localization.Translate("NONE");

        var hasEnzymes = organelle.Enzymes.Count > 0;
        enzymesLabel.Modulate = hasEnzymes ? opaque : translucent;
        enzymesLabel.Text = hasEnzymes ?
            organelle.Enzymes
                .Where(e => e.Value > 0)
                .Select(e => e.Key.Name)
                .Aggregate((a, b) => a + "\n" + b) :
            Localization.Translate("NONE");

        var hasUpgrades = organelle.AvailableUpgrades.Count > 0;
        upgradesLabel.Modulate = hasUpgrades ? opaque : translucent;
        upgradesLabel.Text = hasUpgrades ?
            organelle.AvailableUpgrades
                .Where(u => u.Key != "none")
                .Select(u => u.Value.Name)
                .Aggregate((a, b) => a + "\n" + b) :
            Localization.Translate("NONE");

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

    private void UpdateModelImage()
    {
        if (model == null || modelTexture == null)
            return;

        model.Visible = true;
        model.Texture = modelTexture;
    }
}
