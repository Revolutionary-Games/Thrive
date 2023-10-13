using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
///   Wiki-style info box for an organelle.
/// </summary>
public class OrganelleInfoBox : PanelContainer
{
    [Export]
    public NodePath? NamePath;

    [Export]
    public NodePath IconPath = null!;

    [Export]
    public NodePath ModelPath = null!;

    [Export]
    public NodePath CostLabelPath = null!;

    [Export]
    public NodePath RequiresNucleusLabelPath = null!;

    [Export]
    public NodePath ProcessesLabelPath = null!;

    [Export]
    public NodePath EnzymesLabelPath = null!;

    [Export]
    public NodePath MassLabelPath = null!;

    [Export]
    public NodePath SizeLabelPath = null!;

    [Export]
    public NodePath OsmoregulationCostLabelPath = null!;

    [Export]
    public NodePath StorageLabelPath = null!;

    [Export]
    public NodePath UniqueLabelPath = null!;

    [Export]
    public NodePath UpgradesLabelPath = null!;

    [Export]
    public NodePath InternalNameLabelPath = null!;

    private OrganelleDefinition? organelle;

#pragma warning disable CA2213
    private Label nameLabel = null!;
    private TextureRect icon = null!;
    private TextureRect? model;
    private Label costLabel = null!;
    private Label requiresNucleusLabel = null!;
    private Label processesLabel = null!;
    private Label enzymesLabel = null!;
    private Label massLabel = null!;
    private Label sizeLabel = null!;
    private Label osmoregulationCostLabel = null!;
    private Label storageLabel = null!;
    private Label uniqueLabel = null!;
    private Label upgradesLabel = null!;
    private Label internalNameLabel = null!;

    private Texture? modelTexture;
    private Texture? modelLoadingTexture;
#pragma warning restore CA2213

    private ImageTask? modelImageTask;
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

    public Texture? ModelTexture
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

        nameLabel = GetNode<Label>(NamePath);
        icon = GetNode<TextureRect>(IconPath);
        model = GetNode<TextureRect>(ModelPath);
        costLabel = GetNode<Label>(CostLabelPath);
        requiresNucleusLabel = GetNode<Label>(RequiresNucleusLabelPath);
        processesLabel = GetNode<Label>(ProcessesLabelPath);
        enzymesLabel = GetNode<Label>(EnzymesLabelPath);
        massLabel = GetNode<Label>(MassLabelPath);
        sizeLabel = GetNode<Label>(SizeLabelPath);
        osmoregulationCostLabel = GetNode<Label>(OsmoregulationCostLabelPath);
        storageLabel = GetNode<Label>(StorageLabelPath);
        uniqueLabel = GetNode<Label>(UniqueLabelPath);
        upgradesLabel = GetNode<Label>(UpgradesLabelPath);
        internalNameLabel = GetNode<Label>(InternalNameLabelPath);

        modelLoadingTexture = GD.Load<Texture>("res://assets/textures/gui/bevel/IconGenerating.png");
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (organelle == null || string.IsNullOrEmpty(organelle.DisplayScene) || finishedLoadingModelImage)
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

        modelImageTask = new ImageTask(
            new GalleryCardModel.ModelPreview(organelle.DisplayScene!, organelle.DisplaySceneModelPath ?? "."));

        PhotoStudio.Instance.SubmitTask(modelImageTask);

        ModelTexture = modelLoadingTexture;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (NamePath != null)
            {
                NamePath.Dispose();
                IconPath.Dispose();
                ModelPath.Dispose();
                CostLabelPath.Dispose();
                RequiresNucleusLabelPath.Dispose();
                ProcessesLabelPath.Dispose();
                EnzymesLabelPath.Dispose();
                MassLabelPath.Dispose();
                SizeLabelPath.Dispose();
                OsmoregulationCostLabelPath.Dispose();
                StorageLabelPath.Dispose();
                UniqueLabelPath.Dispose();
                UpgradesLabelPath.Dispose();
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
        if (organelle == null)
            return;

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

        var hasEnzymes = organelle.Enzymes != null && organelle.Enzymes.Count > 0;
        enzymesLabel.Modulate = hasEnzymes ? opaque : translucent;
        enzymesLabel.Text = hasEnzymes ?
            organelle.Enzymes!
                .Where(e => e.Value > 0)
                .Select(e => SimulationParameters.Instance.GetEnzyme(e.Key).Name)
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
        requiresNucleusLabel.Text = TranslationHelper.TranslateBoolean(organelle.RequiresNucleus);
        massLabel.Text = organelle.Mass.ToString(CultureInfo.CurrentCulture);
        sizeLabel.Text = organelle.HexCount.ToString(CultureInfo.CurrentCulture);
        osmoregulationCostLabel.Text = organelle.HexCount.ToString(CultureInfo.CurrentCulture);
        storageLabel.Text = (organelle.Components.Storage?.Capacity ?? 0).ToString(CultureInfo.CurrentCulture);
        uniqueLabel.Text = TranslationHelper.TranslateBoolean(organelle.Unique);
        internalNameLabel.Text = organelle.InternalName;
    }

    private void UpdateModelImage()
    {
        if (model == null || modelTexture == null)
            return;

        model.Visible = true;
        model.Texture = modelTexture;
    }
}
