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

                result += std::max(population, 0);
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
            for(const auto [currentPatch, population] :
                entry.newPopulationInPatches) {

                if(currentPatch == patch)
                    return population;
            }

            break;
        }
    }

    throw InvalidArgument("no population found for requested species");
}
// ------------------------------------ //
void
    RunResults::printSummary(
        const PatchMap::pointer& previousPopulations /*= nullptr*/) const
{
    LOG_INFO("Start of auto-evo results summary (entries: " +
             std::to_string(m_results.size()) + ")");

    LOG_WRITE(makeSummary(previousPopulations, false));

    LOG_INFO("End of results summary");
}

std::string
    patchNameResolveHelper(const PatchMap::pointer& patches, int32_t patchId)
{
    if(!patches)
        return std::to_string(patchId);

    const auto patch = patches->getPatch(patchId);

    if(!patch)
        return std::to_string(patchId) + " (invalid)";

    return patch->getName();
}

std::string
    RunResults::makeSummary(
        const PatchMap::pointer& previousPopulations /*= nullptr*/,
        bool playerReadable /*= false*/) const
{

    std::stringstream sstream;

    for(const auto& entry : m_results) {

        sstream << entry.species->getFormattedName(!playerReadable) << ":"
                << "\n";

        if(entry.mutatedProperties) {
            sstream << " has a mutation";

            if(!playerReadable)
                sstream << ", gene code: "
                        << entry.mutatedProperties->stringCode;

            sstream << "\n";
        }

        if(!entry.spreadPatches.empty()) {
            sstream << " spread to patches:\n";

            for(const auto [patch, population] : entry.spreadPatches) {
                if(playerReadable) {
                    sstream
                        << "  "
                        << patchNameResolveHelper(previousPopulations, patch)
                        << " population: " << population;
                } else {
                    sstream << "  " << patch << " pop: " << population;
                }
                sstream << "\n";
            }
        }

        sstream << " population in patches:\n";
        for(const auto [patch, population] : entry.newPopulationInPatches) {

            sstream << "  ";

            if(!playerReadable) {
                sstream << patch;
            }

            sstream << " "
                    << patchNameResolveHelper(previousPopulations, patch);

            Patch::pointer patchObj;

            if(previousPopulations) {
                patchObj = previousPopulations->getPatch(patch);
            }

            sstream << " population: " << population;

            if(patchObj) {

                sstream << " previous: "
                        << patchObj->getSpeciesPopulation(entry.species);
            }

            sstream << "\n";
        }

        if(playerReadable)
            sstream << "\n";
    }

    return sstream.str();
}
