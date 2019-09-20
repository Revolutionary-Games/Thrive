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
