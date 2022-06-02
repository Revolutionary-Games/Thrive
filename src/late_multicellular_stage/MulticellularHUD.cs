using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Manages the multicellular HUD scene
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public class MulticellularHUD : StageHUDBase<MulticellularStage>
{
    protected override string? UnPauseHelpText => null;

    protected override void ReadPlayerHitpoints(out float hp, out float maxHP)
    {
        throw new System.NotImplementedException();
    }

    protected override CompoundBag? GetPlayerUsefulCompounds()
    {
        throw new System.NotImplementedException();
    }

    protected override void CalculatePlayerReproductionProgress(out Dictionary<Compound, float> gatheredCompounds,
        out Dictionary<Compound, float> totalNeededCompounds)
    {
        throw new System.NotImplementedException();
    }

    protected override ICompoundStorage GetPlayerColonyOrPlayerStorage()
    {
        throw new System.NotImplementedException();
    }

    protected override ProcessStatistics? GetPlayerProcessStatistics()
    {
        return stage!.Player!.ProcessStatistics;
    }

    protected override IEnumerable<(bool Player, Species Species)> GetHoveredSpecies()
    {
        throw new System.NotImplementedException();
    }

    protected override IReadOnlyDictionary<Compound, float> GetHoveredCompounds()
    {
        throw new System.NotImplementedException();
    }

    protected override string GetMouseHoverCoordinateText()
    {
        throw new System.NotImplementedException();
    }

    protected override void UpdateAbilitiesHotBar()
    {
    }

    private void OnBecomeAwarePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        // TODO: aware stage not done yet
        ToolTipManager.Instance.ShowPopup(TranslationServer.Translate("TO_BE_IMPLEMENTED"), 2.5f);
    }

    private void OnAwakenPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        // TODO: awakening stage not done yet
        ToolTipManager.Instance.ShowPopup(TranslationServer.Translate("TO_BE_IMPLEMENTED"), 2.5f);
    }
}
