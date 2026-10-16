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

    private void MicheTreeNodeSelected(int micheHash)
    {
        if (patchForMiches == null)
        {
            GD.PrintErr("Miche viewer not opened");
            return;
        }

        if (!miches.MicheByHash.TryGetValue(micheHash, out var micheData))
        {
            GD.PrintErr("Invalid hash passed into MicheTreeNodeSelected");
            return;
        }

        // NoOps are being used to hold species nodes
        if (micheData.Pressure.GetType() == typeof(NoOpPressure))
        {
            if (micheData.Occupant == null)
            {
                // No species selected so reset the display panel to not make the GUI as confusing
                speciesDetails.Visible = false;
                micheDetails.Visible = false;
                return;
            }

            speciesDetails.Visible = true;
            micheDetails.Visible = false;

            bool found = false;

            foreach (var species in patchForMiches.SpeciesInPatch.Keys)
            {
                if (species.ID == micheData.Occupant.ID)
                {
                    found = true;
                    speciesDetails.PreviewSpecies = species;
                    break;
                }
            }

            if (!found)
            {
                speciesDetails.PreviewSpecies = null;
                GD.PrintErr("Couldn't find species to display");
            }
        }
        else
        {
            speciesDetails.Visible = false;
            micheDetails.Visible = true;

            micheDetails.SetPreview(micheData, patchForMiches);
        }
    }
}
