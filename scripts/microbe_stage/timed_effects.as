// Timed effect functions that act on a world over time

//! Reduces the amount of glucose over time
//! \todo Decide if effects must work even when called once with huge elapsed time or not
void reduceGlucoseOverTime(GameWorld@ world, double elapsed, double totalElapsed)
{
    CellStageWorld@ casted = cast<CellStageWorld>(world);
    assert(casted !is null, "non-microbe world given");

    const auto compoundId = SimulationParameters::compoundRegistry().getTypeId("glucose");

    auto patches = casted.GetPatchManager().getCurrentMap().getPatches();

    for(uint i = 0; i < patches.length(); ++i){

        Patch@ patch = patches[i];

        BiomeCompoundData@ data = patch.getBiome().getCompound(compoundId);

        if(data !is null){
            data.density *= 0.8f;
        }
    }
}

//! Updates the CO2 and O2 levels based on the planets current global state.
void updatePatchGasses(GameWorld@ world, double elapsed, double totalElapsed)
{
    CellStageWorld@ casted = cast<CellStageWorld>(world);
    assert(casted !is null, "non-microbe world given");

    const auto compoundIdCarbonDioxide = SimulationParameters::compoundRegistry().getTypeId("carbondioxide");
    const auto compoundIdOxygen = SimulationParameters::compoundRegistry().getTypeId("oxygen");

    auto patches = casted.GetPatchManager().getCurrentMap().getPatches();

    for(uint i = 0; i < patches.length(); ++i){

        Patch@ patch = patches[i];

        BiomeCompoundData@ dataCarbonDioxide = patch.getBiome().getCompound(compoundIdCarbonDioxide);
        LOG_INFO("**** compoundIdCarbonDioxide" + dataCarbonDioxide.dissolved);

        BiomeCompoundData@ dataOxygen = patch.getBiome().getCompound(compoundIdOxygen);
        LOG_INFO("**** compoundIdOxygen" + dataOxygen.dissolved);
    }
}
