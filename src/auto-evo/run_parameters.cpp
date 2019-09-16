// ------------------------------------ //
#include "run_parameters.h"

using namespace thrive;
// ------------------------------------ //
RunParameters::RunParameters(const PatchMap::pointer& patchesToSimulate) :
    m_map(patchesToSimulate)
{}

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
    LOG_INFO("TODO: RunParameters::applyExternalEffects");
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
        // Takes 10 seconds
        m_totalSteps = 1000;
        m_state = RUN_STAGE::STEPPING;
        return false;
    }
    case RUN_STAGE::STEPPING: {

        std::this_thread::sleep_for(std::chrono::milliseconds(10));
        if(++m_completeSteps >= m_totalSteps) {

            m_state = RUN_STAGE::ENDED;
        }
        return false;
    }
    case RUN_STAGE::ENDED: {
        LOG_INFO("Auto-evo run is complete. Applying results");
        m_success = true;
        return true;
    }
    }

    LOG_FATAL("unreachable");
    return false;
}

void
    RunParameters::onBeginExecuting()
{
    m_inProgress = true;
}
