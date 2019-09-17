// ------------------------------------ //
#include "run_results.h"

#include "auto-evo_script_helpers.h"

using namespace thrive;
using namespace autoevo;
// ------------------------------------ //
RunResults*
    RunResults::factory()
{
    return new RunResults();
}
// ------------------------------------ //
void
    RunResults::addMutationResultForSpecies(const Species::pointer& species,
        const Species::pointer& mutated)
{
    for(auto& entry : m_results) {
        if(entry.species == species) {

            entry.mutatedProperties = mutated;
            return;
        }
    }

    SpeciesResult result{species};
    result.mutatedProperties = mutated;

    m_results.push_back(result);
}

void
    RunResults::addPopulationResultForSpecies(const Species::pointer& species,
        int32_t patch,
        int newPopulation)
{
    for(auto& entry : m_results) {
        if(entry.species == species) {

            entry.newPopulationInPatches[patch] = newPopulation;
            return;
        }
    }

    SpeciesResult result{species};
    result.newPopulationInPatches[patch] = newPopulation;

    m_results.push_back(result);
}
// ------------------------------------ //
void
    RunResults::applyResults(const PatchMap::pointer& map, bool skipMutations)
{
    for(const auto& entry : m_results) {

        if(!skipMutations && entry.mutatedProperties) {
            LOG_INFO("Applying mutation to species: " + entry.species->name);
            applySpeciesMutation(entry.species, entry.mutatedProperties);
        }

        for(const auto [patchId, population] : entry.newPopulationInPatches) {

            auto patch = map->getPatch(patchId);

            if(patch) {
                if(!patch->updateSpeciesPopulation(entry.species, population)) {
                    LOG_ERROR("RunResults failed to update population for a "
                              "species in a patch");
                }

            } else {
                LOG_ERROR("RunResults has a species population change in a "
                          "patch with invalid id: " +
                          std::to_string(patchId));
            }
        }

        if(!entry.spreadPatches.empty()) {
            LOG_ERROR("TODO: spreadPatches applying is not done");
        }
    }
}
// ------------------------------------ //
int
    RunResults::getGlobalPopulation(const Species::pointer& species) const
{
    for(const auto& entry : m_results) {
        if(entry.species == species) {

            int result = 0;

            for(const auto [patch, population] : entry.newPopulationInPatches) {

                result += std::min(population, 0);
            }

            return result;
        }
    }

    throw InvalidArgument("no population found for requested species");
}

int
    RunResults::getPopulationInPatch(const Species::pointer& species,
        int32_t patch) const
{
    for(const auto& entry : m_results) {
        if(entry.species == species) {

            int result = 0;

            // This is a bit silly way to find the data in the map, but this is
            // done this way because this is a copy of getGlobalPopulation
            for(const auto [currentPatch, population] :
                entry.newPopulationInPatches) {

                if(currentPatch == patch)
                    result += std::min(population, 0);
            }

            return result;
        }
    }

    throw InvalidArgument("no population found for requested species");
}
