//! \brief Calculates the energy balance for a cell with the given organelles
//!
//! Used by the editor to collect info to send to the GUI
#include "calculate_effectiveness.as"

void calculateEnergyBalanceWithOrganelles(const array<PlacedOrganelle@>@ organelles,
    int patchId = -1)
{
    auto world = GetThriveGame().getCellStage();
    auto map = world.GetPatchManager().getCurrentMap();

    assert(map !is null, "no current patch map detected");

    Patch@ patch;

    if(patchId == -1){
        @patch = map.getCurrentPatch();
    } else {
        @patch = map.getPatch(patchId);
    }

    if(patch is null){
        LOG_ERROR("calculateEnergyBalanceWithOrganelles: could not find patch: " + patchId);
        return;
    }

    array<const OrganelleTemplate@> organelleTemplates;

    for(uint i = 0; i < organelles.length(); ++i){
        organelleTemplates.insertLast(organelles[i].organelle);
    }

    const string result = world.GetProcessSystem().computeEnergyBalance(
        organelleTemplates, patch);

    // LOG_WRITE("Energy balance data: \n" + result);

    GenericEvent@ event = GenericEvent("MicrobeEditorEnergyBalanceUpdated");
    NamedVars@ vars = event.GetNamedVars();
    vars.AddValue(ScriptSafeVariableBlock("data", result));
    GetEngine().GetEventHandler().CallEvent(event);
}
