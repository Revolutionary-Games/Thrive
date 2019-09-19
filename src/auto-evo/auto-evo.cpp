// ------------------------------------ //
#include "auto-evo.h"

using namespace thrive;
// ------------------------------------ //
AutoEvo::AutoEvo() :
    autoEvoThread(std::bind(&AutoEvo::_runBackgroundThread, this))
{}

AutoEvo::~AutoEvo()
{
    {
        GUARD_LOCK();
        stopThread = true;
    }

    abortSimulations();
    autoEvoThread.join();
}
// ------------------------------------ //
void
    AutoEvo::beginRun(const std::shared_ptr<RunParameters>& run)
{
    if(!run)
        throw InvalidArgument("empty run pointer");

    GUARD_LOCK();

    queuedRuns.push_back(run);

    notifyBackgroundThread.notify_one();
}

void
    AutoEvo::abortSimulations()
{
    GUARD_LOCK();

    for(auto& run : queuedRuns) {
        run->abort();
    }

    queuedRuns.clear();
    notifyBackgroundThread.notify_one();
}
// ------------------------------------ //
bool
    AutoEvo::simulationInProgress() const
{
    return running;
}
// ------------------------------------ //
std::string
    AutoEvo::getStatusString() const
{
    if(!simulationInProgress()) {
        return "Simulation finished.";
    }

    // I suppose there is no way around this lock (easily)
    GUARD_LOCK();

    std::string status = queuedRuns.front()->getStatusString();

    if(queuedRuns.size() > 1) {

        status += " " + std::to_string(queuedRuns.size() - 1) +
                  " operation(s) in queue.";
    }

    return status;
}
// ------------------------------------ //
void
    AutoEvo::_runBackgroundThread()
{
    GUARD_LOCK();

    while(!stopThread) {

        if(queuedRuns.empty()) {
            // Wait for work
            notifyBackgroundThread.wait(guard);
            continue;
        }

        LOG_INFO("Auto-evo beginning work on a run");
        const auto start = std::chrono::high_resolution_clock::now();

        running = true;

        currentlyRunning = queuedRuns.front();

        guard.unlock();

        currentlyRunning->onBeginExecuting();

        while(!stopThread) {
            try {
                if(currentlyRunning->step()) {
                    // Complete
                    break;
                }
            } catch(const Leviathan::Exception& e) {
                LOG_ERROR("Exception happened in auto-evo step: ");
                e.PrintToLog();
            }
        }

        if(currentlyRunning->inProgress() ||
            !currentlyRunning->wasSuccessful()) {
            LOG_INFO("Auto-evo run was aborted or it failed");
        }

        guard.lock();

        const auto pos =
            std::find(queuedRuns.begin(), queuedRuns.end(), currentlyRunning);
        if(pos != queuedRuns.end())
            queuedRuns.erase(pos);

        currentlyRunning.reset();

        const auto end = std::chrono::high_resolution_clock::now();
        std::chrono::duration<float> elapsed = end - start;
        LOG_INFO("Auto-evo finished working on a run. Elapsed time: " +
                 std::to_string(elapsed.count()) + "s");
    }
}
