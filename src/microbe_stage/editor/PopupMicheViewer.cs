using AutoEvo;
using Godot;

/// <summary>
///   Shows miches for a patch in a popup (for use in editors to inspect things)
/// </summary>
public partial class PopupMicheViewer : Control
{
#pragma warning disable CA2213
    [Export]
    private CustomWindow michePopup = null!;

    [Export]
    private MicheTree miches = null!;

    [Export]
    private SpeciesDetailsPanel speciesDetails = null!;

    [Export]
    private MicheDetailsPanel micheDetails = null!;
#pragma warning restore CA2213

    public override void _Ready()
    {
        base._Ready();

        // This is hidden in the editor to be out of the way so, show this by default
        Visible = true;
    }

    public void ShowMiches(string patchName, Miche michesToShow)
    {
        miches.SetMiche(michesToShow);
        speciesDetails.PreviewSpecies = null;
        micheDetails.ClearPreview();

        michePopup.WindowTitle = Localization.Translate("MICHES_FOR_PATCH").FormatSafe(patchName);
        michePopup.Show();
    }
}
