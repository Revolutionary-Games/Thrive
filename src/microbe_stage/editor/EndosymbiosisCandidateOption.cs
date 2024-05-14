using System.Collections.Generic;
using Godot;

/// <summary>
///   Info on a single endosymbiosis candidate
/// </summary>
public partial class EndosymbiosisCandidateOption : VBoxContainer
{
#pragma warning disable CA2213
    [Export]
    private Container organelleChoicesContainer = null!;
#pragma warning restore CA2213

    [Signal]
    public delegate void OnOrganelleTypeSelectedEventHandler(string organelleType, int cost);

    public void UpdateChoices(List<(OrganelleDefinition Organelle, int Cost)> organelleChoices)
    {
        if (organelleChoices.Count < 1)
        {
            organelleChoicesContainer.AddChild(new Label
            {
                Text = Localization.Translate("ENDOSYMBIOSIS_NO_CANDIDATE_ORGANELLES"),
                HorizontalAlignment = HorizontalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                CustomMinimumSize = new Vector2(100, 0),
            });
            return;
        }

        throw new System.NotImplementedException();
    }
}
