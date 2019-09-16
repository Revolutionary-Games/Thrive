#pragma once

#include "microbe_stage/patch.h"

// TODO: make a common base class for the species classes for different stages.
#include "microbe_stage/species.h"

#include <mutex>

#include <atomic>

namespace thrive {

class AutoEvo;

//! \brief Parameters for an auto-evo run
//! \todo Decide if different types of RunParameters classes should be made for
//! the different stages or not.
class RunParameters {
    friend AutoEvo;

    enum class RUN_STAGE {
        //! On the first step(s) all the data is loaded (if there is a lot then
        //! it is split into multiple steps) and the total number of steps is
        //! calculated
        GATHERING_INFO,
        //! Steps are being executed
        STEPPING,
        //! All the steps are done and the result is written
        ENDED
    };

public:
    RunParameters(const PatchMap::pointer& patchesToSimulate);
    virtual ~RunParameters();

    //! \brief Stops this auto-evo run. Waits until the run won't read any
    //! external resources
    void
        abort();

    float
        getCompletionFraction() const;

    //! \returns True if auto-evo is currently running.
    //! \note While auto-evo is running the patch conditions or species
    //! properties (that this run uses) MAY NOT be changed!
    bool
        inProgress() const
    {
        return m_inProgress;
    }

    bool
        wasSuccessful() const
    {
        return m_success;
    }

    //! \returns a string describing the status of the simulation
    //!
    //! For example "21% done. 21/100 steps."
    virtual std::string
        getStatusString() const;

    //! \brief Applies things added by addExternalPopulationEffect
    //! \note This has to be called after thus run is finished
    void
        applyExternalEffects();

    //! \brief Adds an external population affecting event (player dying,
    //! reproduction, darwinian evo actions)
    //! \todo Maybe it would be better to replace eventType with an enum
    void
        addExternalPopulationEffect(Species::pointer species,
            int amount,
            const std::string& eventType);

protected:
    //! \brief Performs a single calculation step. This should be quite fast
    //! (5-20 milliseconds) in order to make aborting work fast.
    //! \note This should only be called by AutoEvo
    //! \return True when finished or aborted
    virtual bool
        step();

    virtual void
        onBeginExecuting();

protected:
    // These are atomic to not require locking in getStatusString
    std::atomic<RUN_STAGE> m_state = {RUN_STAGE::GATHERING_INFO};

    std::atomic<bool> m_inProgress = {false};
    std::atomic<bool> m_success = {false};

    //! -1 means not yet computed
    std::atomic<int> m_totalSteps = {-1};
    std::atomic<int> m_completeSteps = {0};

    PatchMap::pointer m_map;

    //! Locked while stepping or in abort
    std::mutex m_stepMutex;

    //! \todo These aren't handled at all
    std::vector<std::tuple<Species::pointer, int, std::string>>
        m_externalEffects;
    std::mutex m_externalEffectsMutex;
};

} // namespace thrive
