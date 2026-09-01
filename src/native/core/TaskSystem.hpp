#pragma once

#include <atomic>
#include <functional>
#include <queue>
#include <thread>
#include <vector>

#include "Jolt/Core/FixedSizeFreeList.h"
#include "Jolt/Core/JobSystemWithBarrier.h"
#include "Jolt/Core/Semaphore.h"

#include "Include.h"

#include "concurrentqueue.h"

namespace Thrive
{
/// \brief Handles multithreaded execution of native module code
class TaskSystem final : public JPH::JobSystemWithBarrier,
                         public NonCopyable
{
public:
    JPH_OVERRIDE_NEW_DELETE;

    using SimpleCallable = void (*)();

private:
    enum class TaskType : uint8_t
    {
        Cleared = 0,
        Quit,
        Simple,
        StdFunction,
    };

    struct QuitSentinel
    {
    };

    struct QueuedTask
    {
    public:
    public:
#ifdef USE_LOCK_FREE_QUEUE
        /// \brief Creates an empty task, for use to dequeue items
        explicit QueuedTask();
#endif

        explicit QueuedTask(SimpleCallable callable);

        explicit QueuedTask(std::function<void()> callable);

        // explicit QueuedTask(std::function<void()>&& callable);

        explicit QueuedTask(QuitSentinel quit);

        QueuedTask(const QueuedTask& other) = delete;

        QueuedTask(QueuedTask&& other) noexcept;

        inline ~QueuedTask()
        {
            ReleaseCurrentData();

            // TODO: check this:
            // This allows objects of this type to be reused at the same address without requiring re-initialization
            // Type = TaskType::Cleared;
        }

        void Invoke() const;

        QueuedTask& operator=(QueuedTask&& other) noexcept;

        QueuedTask& operator=(const QueuedTask& other) = delete;

        union
        {
            SimpleCallable Simple;

            std::function<void()> Function;
        };

        TaskType Type;

    private:
        void ReleaseCurrentData();
        void MoveDataFromOther(QueuedTask&& other);
    };

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
    void QueueTask(SimpleCallable callable)
    {
        QueueTask(QueuedTask(callable));
    }

    void QueueTask(QueuedTask&& task);

    void QueueTask(std::function<void()> callable)
    {
        QueueTask(QueuedTask(std::move(callable)));
    }

    /// \brief Variant of queue that can be called from any thread
    void QueueTaskFromBackgroundThread(SimpleCallable callable)
    {
        QueueTaskFromBackgroundThread(QueuedTask(callable));
    }

    void QueueTaskFromBackgroundThread(QueuedTask&& task);

    void QueueTaskFromBackgroundThread(std::function<void()>&& callable)
    {
        QueueTaskFromBackgroundThread(QueuedTask(std::move(callable)));
    }

    // Jolt task interface

    virtual JobHandle CreateJob(const char* inName, JPH::ColorArg inColor, const JobFunction& inJobFunction,
        uint32_t inNumDependencies) override;

    void SetThreads(int count) noexcept;

    [[nodiscard]] int GetThreads() const noexcept
    {
        return targetThreadCount.load(std::memory_order_relaxed);
    }

    /// \brief The number of physics tasks done in parallel
    ///
    /// TODO: determine if it would be good to limit the max physics threads (maybe 16?)
    [[nodiscard]] virtual int GetMaxConcurrency() const override
    {
        // Jolt counts the thread waiting on a barrier as an additional worker.
        return targetThreadCount.load(std::memory_order_relaxed) + 1;
    }

    /// \brief Shuts down all threads and doesn't allow starting more
    void Shutdown();

protected:
    virtual void FreeJob(Job* inJob) override;

    virtual void QueueJob(Job* inJob) override;

    virtual void QueueJobs(Job** inJobs, uint32_t inNumJobs) override;

private:
#ifdef USE_LOCK_FREE_QUEUE
    FORCE_INLINE void TryEnqueueTask(QueuedTask&& task);
#endif

    void StartTaskThread();
    void EndTaskThread();

    void RunTaskThread(int id);

private:
#ifdef USE_OBJECT_POOLS
    JPH::FixedSizeFreeList<Job> jobPool;
#endif

    std::vector<std::thread> taskThreads;

    // || !defined(TASK_QUEUE_USES_POINTERS)
#if defined(USE_LOCK_FREE_QUEUE)

    // For efficiency, we have both general tasks and Jolt tasks
    moodycamel::ConcurrentQueue<QueuedTask> taskQueue;
    moodycamel::ConcurrentQueue<Job*> jobQueue;

#else
    std::queue<QueuedTask> taskQueue;
    std::queue<Job*> jobQueue;
#endif

    /// When USE_LOCK_FREE_QUEUE is defined this should not be locked to write to the queue
    std::mutex queueMutex;

    /// This now uses a Jolt semaphore to ensure as many threads are woken up as required
    JPH::Semaphore queueNotify;

    /// Lock used on the main thread to enqueue tasks
    std::unique_lock<std::mutex> queueLock;

    std::atomic<int> targetThreadCount{0};

    int threadCount = 0;

    std::atomic<bool> runThreads{true};
};

} // namespace Thrive
