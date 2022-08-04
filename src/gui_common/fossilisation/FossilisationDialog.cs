using Godot;
using System.Linq;

public class FossilisationDialog : CustomDialog
{
    [Export]
    public NodePath NameEditPath = null!;

    [Export]
    public NodePath SpeciesPreviewPath = null!;

    [Export]
    public NodePath HexPreviewPath = null!;

    [Export]
    public NodePath SpeciesDetailsLabelPath = null!;

    public Species SelectedSpecies
    {
        get => selectedSpecies;
        set
        {
            selectedSpecies = value;

            UpdateSpeciesName(selectedSpecies.FormattedName);
            UpdateSpeciesPreview();
            UpdateSpeciesDetails();
        }
    }

    private LineEdit nameEdit = null!;
    private SpeciesPreview speciesPreview = null!;
    private CellHexesPreview hexesPreview = null!;
    private CustomRichTextLabel speciesDetailsLabel = null!;

    private Species selectedSpecies = null!;

    public override void _Ready()
    {
        base._Ready();

        nameEdit = GetNode<LineEdit>(NameEditPath);
        speciesPreview = GetNode<SpeciesPreview>(SpeciesPreviewPath);
        hexesPreview = GetNode<CellHexesPreview>(HexPreviewPath);
        speciesDetailsLabel = GetNode<CustomRichTextLabel>(SpeciesDetailsLabelPath);
    }

    private void UpdateSpeciesName(string name)
    {
        nameEdit.Text = name;
    }

    private void UpdateSpeciesPreview()
    {
        speciesPreview.PreviewSpecies = SelectedSpecies;
        hexesPreview.PreviewSpecies = (MicrobeSpecies)SelectedSpecies;
    }

    private void UpdateSpeciesDetails()
    {
        speciesDetailsLabel.ExtendedBbcode = TranslationServer.Translate("SPECIES_DETAIL_TEXT").FormatSafe(
            SelectedSpecies.FormattedNameBbCode, SelectedSpecies.ID, SelectedSpecies.Generation, SelectedSpecies.Population, SelectedSpecies.Colour.ToHtml(),
            string.Join("\n  ", SelectedSpecies.Behaviour.Select(b => b.Key + ": " + b.Value)));

        switch (SelectedSpecies)
        {
            case MicrobeSpecies microbeSpecies:
            {
                speciesDetailsLabel.ExtendedBbcode += "\n" +
                    TranslationServer.Translate("MICROBE_SPECIES_DETAIL_TEXT").FormatSafe(
                        microbeSpecies.MembraneType.Name, microbeSpecies.MembraneRigidity,
                        microbeSpecies.BaseSpeed, microbeSpecies.BaseRotationSpeed, microbeSpecies.BaseHexSize);
                break;
            }
        }
    }

    private void OnCancelPressed()
    {
        Hide();
    }

    private void OnFossilisePressed()
    {
        Hide();
    }
}