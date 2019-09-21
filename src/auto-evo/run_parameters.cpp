// ------------------------------------ //
#include "run_parameters.h"

#include "common_steps.h"
#include "run_results.h"
#include "run_step.h"

#include <numeric>

using namespace thrive;
using namespace autoevo;
// ------------------------------------ //
RunParameters::RunParameters(const PatchMap::pointer& patchesToSimulate) :
    m_map(patchesToSimulate), m_results(RunResults::MakeShared<RunResults>())
{
    if(!m_map)
        throw InvalidArgument("null map give to RunParameters");
}

RunParameters::~RunParameters() {}
// ------------------------------------ //
void
    RunParameters::abort()
{
    // It seems that the stepping function can get the mutex much better than
    // this one
    m_inProgress = false;

    std::lock_guard<std::mutex> lock(m_stepMutex);
    m_inProgress = false;
    m_success = false;
}
// ------------------------------------ //
float
    RunParameters::getCompletionFraction() const
{
    if(m_totalSteps < 0)
        return 0;

    return static_cast<float>(m_completeSteps) / m_totalSteps;
}

std::string
    RunParameters::getStatusString() const
{
    if(!m_inProgress) {

        if(m_success) {
            return "Finished.";
        }

        return "Not running.";
    }

    if(m_totalSteps > 0) {

        return std::to_string(getCompletionFraction() * 100) + "% done. " +
               std::to_string(m_completeSteps) + "/" +
               std::to_string(m_totalSteps) + " steps.";
    } else {
        return "Starting";
    }
}
// ------------------------------------ //
void
    RunParameters::addExternalPopulationEffect(Species::pointer species,
        int amount,
        const std::string& eventType)
{
    std::lock_guard<std::mutex> lock(m_externalEffectsMutex);
    m_externalEffects.push_back(std::make_tuple(species, amount, eventType));
}

void
    RunParameters::applyExternalEffects()
{
    std::lock_guard<std::mutex> lock(m_externalEffectsMutex);

    if(m_externalEffects.empty())
        return;

    const auto currentPatch = m_map->getCurrentPatchId();

    // Effects are applied in the current patch
    for(const auto& [species, amount, eventType] : m_externalEffects) {
        // TODO: make a log for debugging here from the external effect
        // parameters
        try {
            int currentPop =
                m_results->getPopulationInPatch(species, currentPatch);

            m_results->addPopulationResultForSpecies(
                species, currentPatch, currentPop + amount);

        } catch(const Leviathan::InvalidArgument& e) {
            LOG_WARNING("External effect can't be applied: ");
            e.PrintToLog();
        }
    }

    m_results->applyResults(m_map, false);
}
// ------------------------------------ //
std::string
    RunParameters::makeSummaryOfExternalEffects() const
{
    std::stringstream sstream;

    for(const auto& [species, amount, eventType] : m_externalEffects) {
        sstream << species->getFormattedName() << " "
                << (amount >= 0 ? "gained" : "lost") << " " << amount
                << " population because of: " << eventType << "\n";
    }

    return sstream.str();
}
// ------------------------------------ //
bool
    RunParameters::step()
{
    std::lock_guard<std::mutex> lock(m_stepMutex);
    if(!m_inProgress) {
        // Aborted
        return true;
    }

    switch(m_state) {
    case RUN_STAGE::GATHERING_INFO: {
        LOG_INFO("Auto-evo run is gathering info");
        _gatherInfo();

        // +2 is for this step and the result apply step
        m_totalSteps = std::accumulate(m_runSteps.begin(), m_runSteps.end(), 0,
                           [](int total, const std::unique_ptr<RunStep>& item) {
                               return total + item->getTotalSteps();
                           }) +
                       2;

        LOG_INFO("Step count for simulation: " + std::to_string(m_totalSteps));
        ++m_completeSteps;
        m_state = RUN_STAGE::STEPPING;
        return false;
    }
    case RUN_STAGE::STEPPING: {

        if(m_runSteps.empty()) {
            // All steps complete
            m_state = RUN_STAGE::ENDED;
        } else {

            if(m_runSteps.front()->step(*m_results))
                m_runSteps.pop_front();

            ++m_completeSteps;
        }

        return false;
    }
    case RUN_STAGE::ENDED: {
        LOG_INFO("Auto-evo run is complete. Applying results");
        // NOTE: extinct species are not removed yet as they might be revived
        // through external effects.
        // Remember to call PatchMap::removeExtinctSpecies after applying the
        // external effects

        m_results->printSummary(m_map);

        // Store the summary text to store previous populations
        m_results->setStoredSummary(m_results->makeSummary(m_map, true));

        m_results->applyResults(m_map, true);

        m_success = true;
        m_inProgress = false;
        ++m_completeSteps;
        return true;
    }
    }

    LOG_FATAL("unreachable");
    return false;
}
// ------------------------------------ //
void
    RunParameters::_gatherInfo()
{
    LOG_INFO("Patch count: " + std::to_string(m_map->getPatches().size()));

    int totalSpecies = 0;

    for(const auto& [id, patch] : m_map->getPatches()) {

        for(const auto& species : patch->getSpecies()) {

            ++totalSpecies;

            // The player species doesn't get random mutations
            if(!species.species->isPlayerSpecies()) {

                m_runSteps.push_back(std::make_unique<FindBestMutation>(m_map,
                    species.species, m_mutationsPerSpecies, m_allowNoMutation));

            } else {
            }
        }
    }

    // The new populations don't depend on the mutations, this is so that when
    // the player edits their species the other species they are competing
    // against are the same (so we can show some performance predictions in the
    // editor and suggested changes)
    m_runSteps.push_back(std::make_unique<CalculatePopulation>(m_map));

    // Adjust auto-evo results for player species
    // NOTE: currently the population change is random so it is canceled out for
    // the player
    m_runSteps.push_back(
        std::make_unique<LambdaStep>([map = this->m_map](RunResults& result) {
            for(const auto& [id, patch] : map->getPatches()) {

                for(const auto& species : patch->getSpecies()) {
                    if(species.species->isPlayerSpecies()) {
                        result.addPopulationResultForSpecies(
                            species.species, id, species.population);
                    }
                }
            }
        }));


    LOG_INFO("Species count: " + std::to_string(totalSpecies));
}
// ------------------------------------ //
void
    RunParameters::onBeginExecuting()
{
    m_inProgress = true;
}
