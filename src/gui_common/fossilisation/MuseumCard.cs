using Godot;

public class MuseumCard : Button
{
    [Export]
    public NodePath SpeciesNameLabelPath = null!;

    [Export]
    public NodePath SpeciesPreviewPath = null!;

    public MicrobeSpecies SavedSpecies = null!;

    private Label speciesNameLabel = null!;
    private SpeciesPreview speciesPreview = null!;

    [Signal]
    public delegate void OnSpeciesSelected(MuseumCard card);

    public override void _Ready()
    {
        base._Ready();

        speciesPreview = GetNode<SpeciesPreview>(SpeciesPreviewPath);
        speciesNameLabel = GetNode<Label>(SpeciesNameLabelPath);

        SavedSpecies = new MicrobeSpecies(1, "Primum", "Testum");
        GameWorld.SetInitialSpeciesProperties(SavedSpecies);
        SavedSpecies.Organelles.Add(new OrganelleTemplate(
            SimulationParameters.Instance.GetOrganelleType("cytoplasm"), new Hex(1, 0), 0));

        UpdateSpeciesPreview();
    }

    private void UpdateSpeciesPreview()
    {
        speciesPreview.PreviewSpecies = SavedSpecies;
        speciesNameLabel.Text = SavedSpecies.FormattedName;
    }

    private void OnPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(nameof(OnSpeciesSelected), this);
    }
}