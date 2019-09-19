#pragma once

#include "run_parameters.h"

#include <Common/ThreadSafe.h>

#include <condition_variable>
#include <thread>

namespace thrive {

//! \brief auto-evo functionality
//!
//! \todo Decide if using a single background thread here (or variable amount),
//! OR using the Leviathan::ThreadingManager tasks to run the auto evo is
//! better.
class AutoEvo : public Leviathan::ThreadSafe {
public:
    AutoEvo();
    ~AutoEvo();

    //! \brief Queues a run to be executed
    void
        beginRun(const std::shared_ptr<RunParameters>& run);

    //! \brief Aborts the current simulations
    //! \note This should only be called if the player quits the game or exits
    //! to the menu
    void
        abortSimulations();

    //! \returns True if auto-evo is currently running.
    //! \note While auto-evo is running the patch conditions or species
    //! properties MAY NOT be changed! Also note that this is not immediate, it
    //! takes a bit of time for a run to go into running status.
    bool
        simulationInProgress() const;

    auto
        getQueueSize() const
    {
        GUARD_LOCK();
        return queuedRuns.size();
    }

    //! \returns a string describing the status of the simulation
    //!
    //! For example "21% done. 21/100 steps. 1 operation in queue."
    virtual std::string
        getStatusString() const;

private:
    void
        _runBackgroundThread();

private:
    std::atomic<bool> running = {false};

    //! When true background thread should stop
    std::atomic<bool> stopThread = {false};

    std::condition_variable notifyBackgroundThread;

    std::vector<std::shared_ptr<RunParameters>> queuedRuns;
    //! Only access this on the background thread
    std::shared_ptr<RunParameters> currentlyRunning;

    std::thread autoEvoThread;
};


} // namespace thrive
