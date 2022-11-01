using Godot;

/// <summary>
///   Card displaying a fossilised species in the Thriveopedia museum.
/// </summary>
public class MuseumCard : Button
{
    [Export]
    public NodePath SpeciesNameLabelPath = null!;

    [Export]
    public NodePath SpeciesPreviewPath = null!;

    private Label speciesNameLabel = null!;
    private SpeciesPreview speciesPreview = null!;

    private Species? savedSpecies;

    [Signal]
    public delegate void OnSpeciesSelected(MuseumCard card);

    /// <summary>
    ///   The fossilised species associated with this card.
    /// </summary>
    public Species? SavedSpecies
    {
        get => savedSpecies;
        set
        {
            savedSpecies = value;
            UpdateSpeciesPreview();
        }
    }

    public override void _Ready()
    {
        base._Ready();

        speciesPreview = GetNode<SpeciesPreview>(SpeciesPreviewPath);
        speciesNameLabel = GetNode<Label>(SpeciesNameLabelPath);

        UpdateSpeciesPreview();
    }

    private void UpdateSpeciesPreview()
    {
        if (speciesPreview != null && SavedSpecies != null)
        {
            speciesPreview.PreviewSpecies = SavedSpecies;
            speciesNameLabel.Text = SavedSpecies.FormattedName;
        }
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
