#pragma once

#include <atomic>
#include <condition_variable>
#include <functional>
#include <queue>
#include <thread>
#include <vector>

#include "boost/pool/object_pool.hpp"
#include "Jolt/Core/JobSystemWithBarrier.h"

#include "Include.h"

namespace Thrive
{
/// \brief Handles multithreaded execution of native module code
class TaskSystem : public JPH::JobSystemWithBarrier,
                   public NonCopyable
{
public:
    using SimpleCallable = void (*)();
    using InstanceMethod = void (*)(void* instance);

    struct MethodAndInstance
    {
    public:
        MethodAndInstance(InstanceMethod method, void* instance) : Method(method), Instance(instance)
        {
        }

        InstanceMethod Method;
        void* Instance;
    };

private:
    enum class TaskType : uint8_t
    {
        Cleared = 0,
        Quit,
        Simple,
        Instance,
        StdFunction,
        JoltJob,
    };

    struct QueuedTask;

private:
    TaskSystem();
    ~TaskSystem() override;

public:
    static THRIVE_NATIVE_API TaskSystem& Get()
    {
        static TaskSystem system;

        return system;
    }

    [[nodiscard]] static bool IsOnMainThread();
    static void AssertIsMainThread();

    /// \brief Enqueues a new task. Can only be called from the main thread.
    void QueueTask(SimpleCallable callable);

    /// \brief Variant of queue that can be called from any thread
    void QueueTaskFromBackgroundThread(SimpleCallable callable);

    // Jolt task interface

    JobHandle CreateJob(const char* inName, JPH::ColorArg inColor, const JobFunction& inJobFunction,
        uint32_t inNumDependencies) override;

    void FreeJob(Job* inJob) override;

    void QueueJob(Job* inJob) override;

    void QueueJobs(Job** inJobs, uint inNumJobs) override;

    void SetThreads(int count) noexcept;

    [[nodiscard]] int GetThreads() const noexcept
    {
        return targetThreadCount;
    }

    /// \brief The number of physics tasks done in parallel
    ///
    /// TODO: determine if it would be good to limit the max physics threads (maybe 16?)
    [[nodiscard]] int GetMaxConcurrency() const override
    {
        return GetThreads();
    }

    /// \brief Shuts down all threads and doesn't allow starting more
    void Shutdown();

private:
    void StartTaskThread();
    void EndTaskThread();

    void RunTaskThread(int id);

private:
#ifdef USE_OBJECT_POOLS
    boost::object_pool<Job> jobPool;
#endif

    std::vector<std::thread> taskThreads;

    std::queue<QueuedTask> taskQueue;

    std::mutex queueMutex;
    std::condition_variable queueNotify;

    /// Lock used on the main thread to enqueue tasks
    std::unique_lock<std::mutex> queueLock;

    int targetThreadCount = 0;

    int threadCount = 0;

    std::atomic<bool> runThreads{true};
};

} // namespace Thrive
