using Godot;

public class MuseumCard : Button
{
    [Export]
    public NodePath SpeciesNameLabelPath = null!;

    [Export]
    public NodePath SpeciesPreviewPath = null!;

    public Species SavedSpecies = null!;

    private Label speciesNameLabel = null!;
    private SpeciesPreview speciesPreview = null!;

    [Signal]
    public delegate void OnSpeciesSelected(MuseumCard card);

    public override void _Ready()
    {
        base._Ready();

        speciesPreview = GetNode<SpeciesPreview>(SpeciesPreviewPath);
        speciesNameLabel = GetNode<Label>(SpeciesNameLabelPath);

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

    private void OnMouseEnter()
    {
        GUICommon.Instance.Tween.InterpolateProperty(speciesPreview, "modulate", null, Colors.Gray, 0.5f);
        GUICommon.Instance.Tween.Start();
    }

    private void OnMouseExit()
    {
        GUICommon.Instance.Tween.InterpolateProperty(speciesPreview, "modulate", null, Colors.White, 0.5f);
        GUICommon.Instance.Tween.Start();
    }
}
