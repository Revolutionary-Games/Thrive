// Script parts of auto-evo
// The C++ main side is in src/auto-evo/auto-evo.h
// These functions are coded as scripts to allow easier tweaking. Note: all of these are ran
// in a background thread and touching global variables is not allowed.

// AI species migration is implemented in ai_migration.as

//! This takes the properties from the second parameter and applies
//! them to the first (that make sense to apply)
void applyMutatedSpeciesProperties(Species@ target, const Species@ mutatedProperties)
{
    @target.organelles = mutatedProperties.organelles;
    @target.avgCompoundAmounts = mutatedProperties.avgCompoundAmounts;

    target.colour = mutatedProperties.colour;
    target.isBacteria = mutatedProperties.isBacteria;
    target.speciesMembraneType = mutatedProperties.speciesMembraneType;

    // These don't mutate for a species
    // name;
    // genus;
    // epithet;

    target.stringCode = mutatedProperties.stringCode;

    // Behavior properties
    target.aggression = mutatedProperties.aggression;
    target.opportunism = mutatedProperties.opportunism;
    target.fear = mutatedProperties.fear;
    target.activity = mutatedProperties.activity;
    target.focus = mutatedProperties.focus;
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
    const auto patchId = patch.getId();

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
        const Species@ currentSpecies = species[i];

        int currentPopulation = patch.getSpeciesPopulation(currentSpecies);

        // If this is an extra species, this first takes the
        // population from extra species that match its index, if that
        // doesn't exist then the population number is used
        if(currentPopulation == 0){

            bool useGlobal = false;

            for(uint a = 0; a < config.getExtraSpeciesCount(); ++a){
                if(config.getExtraSpecies(a) is currentSpecies){

                    if(config.getExcludedSpeciesCount() > a){
                        currentPopulation = patch.getSpeciesPopulation(
                            config.getExcludedSpecies(a));
                    } else {
                        useGlobal = true;
                    }

                    break;
                }
            }

            if(useGlobal)
                currentPopulation = currentSpecies.population;
        }

        // Apply migrations
        // TODO: looping these here in scripts all the time might be slow
        // if we have many migrations
        for(uint64 migrationIndex = 0; migrationIndex < config.getMigrationsCount();
            ++migrationIndex){

            const auto migration = config.getMigration(migrationIndex);

            if(migration.getSpecies() is currentSpecies){
                if(migration.fromPatch == patchId){
                    currentPopulation -= migration.population;
                } else if(migration.toPatch == patchId){
                    currentPopulation += migration.population;
                }
            }
        }

        results.addPopulationResultForSpecies(currentSpecies, patchId,
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

    // Here's a temporary boost when there are few species and penalty
    // when there are many species
    const bool lowSpecies = species.length() <= AUTO_EVO_LOW_SPECIES_THRESHOLD;
    const bool highSpecies = species.length() >= AUTO_EVO_HIGH_SPECIES_THRESHOLD;

    for(uint i = 0; i < species.length(); ++i){
        const Species@ currentSpecies = species[i];
        const int currentPopulation = results.getPopulationInPatch(currentSpecies,
            patchIdentifier);
        int populationChange = GetEngine().GetRandom().GetNumber(
            -AUTO_EVO_RANDOM_POPULATION_CHANGE, AUTO_EVO_RANDOM_POPULATION_CHANGE);

        // LOG_WRITE("current: " + currentPopulation + " change: " + populationChange);

        if(lowSpecies){
            populationChange += AUTO_EVO_LOW_SPECIES_BOOST;

        } else if(highSpecies){
            populationChange -= AUTO_EVO_HIGH_SPECIES_PENALTY;
        }

        results.addPopulationResultForSpecies(currentSpecies, patchIdentifier,
            currentPopulation + populationChange);
    }
}
