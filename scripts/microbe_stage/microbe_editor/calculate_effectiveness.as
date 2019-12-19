//! \brief Calculates the effectiveness of organelles in the current
//! or given patch and sends it to the GUI
void calculateOrganelleEffectivenessInPatch(int patchId = -1)
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
        LOG_ERROR("calculateOrganelleEffectivenessInPatch: could not find patch: " + patchId);
        return;
    }

    auto keys = getOrganelleNames();

    array<OrganelleTemplate@> organelles;

    for(uint i = 0; i < keys.length(); ++i){
        organelles.insertLast(getOrganelleDefinition(keys[i]));
    }

    const string result = world.GetProcessSystem().computeOrganelleProcessEfficiencies(
        organelles, patch);

    LOG_WRITE("OrganellePatchEfficiencyData: \n" + result);

    GenericEvent@ event = GenericEvent("OrganellePatchEfficiencyData");
    NamedVars@ vars = event.GetNamedVars();
    vars.AddValue(ScriptSafeVariableBlock("data", result));
    GetEngine().GetEventHandler().CallEvent(event);
}

