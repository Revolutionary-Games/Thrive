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

    private Patch? patchForMiches;

    public override void _Ready()
    {
        base._Ready();

        // This is hidden in the editor to be out of the way so, show this by default
        Visible = true;
    }

    public void ShowMiches(Patch patch, Miche michesToShow, WorldGenerationSettings worldSettings)
    {
        patchForMiches = patch;

        miches.SetMiche(michesToShow);
        micheDetails.WorldSettings = worldSettings;

        speciesDetails.PreviewSpecies = null;
        micheDetails.ClearPreview();

        micheDetails.Visible = false;
        speciesDetails.Visible = false;

        michePopup.WindowTitle = Localization.Translate("MICHES_FOR_PATCH").FormatSafe(patch.Name);
        michePopup.Show();
    }

    private void MicheTreeNodeSelected(MicheTreeNode node)
    {
        if (patchForMiches == null)
        {
            GD.PrintErr("Miche viewer not opened");
            return;
        }

        if (node.ShowsOccupant)
        {
            speciesDetails.PreviewSpecies = node.Miche.Occupant;
            speciesDetails.Visible = node.Miche.Occupant != null;
            micheDetails.Visible = false;
        }
        else
        {
            speciesDetails.Visible = false;
            micheDetails.Visible = true;

            micheDetails.SetPreview(node.Miche, patchForMiches);
        }
    }
}
