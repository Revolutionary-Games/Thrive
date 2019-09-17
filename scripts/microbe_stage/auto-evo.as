// Script parts of auto-evo
// The C++ main side is in src/auto-evo/auto-evo.h
// These functions are coded as scripts to allow easier tweaking. Note: all of these are ran
// in a background thread and touching global variables is not allowed.

//! This takes the properties from the second parameter and applies
//! them to the first (that make sense to apply)
void applyMutatedSpeciesProperties(Species@ target, const Species@ mutatedProperties)
{
    LOG_INFO("TODO: applyMutatedSpeciesProperties");
}

// ------------------------------------ //
// Population simulations.

//! Simulates all patches in the map
RunResults@ simulatePatchMapPopulations(const PatchMap@ map,
    const SimulationConfiguration@ config)
{
    RunResults@ results = RunResults();

    auto@ patches = map.getPatches();

    for(uint i = 0; i < patches.length(); ++i){

        simulatePatchPopulations(patches[i], results, config);
    }

    return results;
}

//! Simulates a single patch
void simulatePatchPopulations(const Patch@ patch, RunResults@ results,
    const SimulationConfiguration@ config)
{
    array<const Species@> species;

    // Populate the species from the patch taking config into account
    for(uint i = 0; i < patch.getSpeciesCount(); ++i){
        const Species@ potentialSpecies = patch.getSpecies(i);

        bool exclude = false;

        for(uint a = 0; a < config.getExcludedSpeciesCount(); ++a){
            if(potentialSpecies is config.getExcludedSpecies(a)){

                exclude = true;
                break;
            }
        }

        if(!exclude)
            species.insertLast(potentialSpecies);
    }

    for(uint i = 0; i < config.getExtraSpeciesCount(); ++i){
        species.insertLast(config.getExtraSpecies(i));
    }

    // Prepare population numbers
    for(uint i = 0; i < species.length(); ++i){
        const int currentPopulation = patch.getSpeciesPopulation(species[i]);

        results.addPopulationResultForSpecies(species[i], patch.getId(),
            currentPopulation);
    }

    // Run steps
    for(int step = 0; step < config.steps; ++step){
        simulatePopulation(patch.getBiome(), patch.getId(), species, results);
    }
}

//! The heart of the simulation that handles the processed parameters and
//! calculates future populations.
//! If this function gets big it should be moved to a new file
void simulatePopulation(const Biome@ conditions, int32 patchIdentifier,
    const array<const Species@> &in species, RunResults@ results)
{
    // TODO: this is where the proper auto-evo algorithm goes
    for(uint i = 0; i < species.length(); ++i){
        const Species@ currentSpecies = species[i];
        const int currentPopulation = results.getPopulationInPatch(currentSpecies,
            patchIdentifier);

        results.addPopulationResultForSpecies(currentSpecies, patchIdentifier,
            currentPopulation + GetEngine().GetRandom().GetNumber(-50, 50));
    }
}
