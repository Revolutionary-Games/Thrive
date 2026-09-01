// ------------------------------------ //
#include "TaskSystem.hpp"

#include "Jolt/Physics/PhysicsSettings.h"

#include "Logger.hpp"
#include "Time.hpp"

#ifdef _WIN32
#define WIN32_LEAN_AND_MEAN
#include <windows.h>

// Windows.h needs to be before these

#include <debugapi.h>
#include <processthreadsapi.h>

#else
#include <pthread.h>
#endif

// ------------------------------------ //
namespace Thrive
{
constexpr int MAIN_THREAD = 42;
static thread_local int MainThreadIdentifier = 0;

static std::atomic<int> ThreadIdentifierNumber{0};

std::string GenerateThreadName(int id)
{
    return "TNative_" + std::to_string(id);
}

#ifdef _WIN32

// Thread rename trick on Windows
constexpr DWORD MS_VC_EXCEPTION = 0x406D1388;

#pragma pack(push, 8)

typedef struct tagTHREADNAME_INFO
{
    DWORD dwType; // Must be 0x1000.
    LPCSTR szName; // Pointer to name (in user addr space).
    DWORD dwThreadID; // Thread ID (-1=caller thread).
    DWORD dwFlags; // Reserved for future use, must be zero.
} THREADNAME_INFO;

#pragma pack(pop)

void SetThreadNameImpl(DWORD threadId, const std::string& name)
{
    // Do this trick as shown on MSDN
    THREADNAME_INFO info;
    info.dwType = 0x1000;
    // Set the name //
    info.szName = name.c_str();
    info.dwThreadID = threadId;
    info.dwFlags = 0;

    // TODO: implement when cross compiled
#ifdef _MSC_VER

    __try
    {
        RaiseException(MS_VC_EXCEPTION, 0, sizeof(info) / sizeof(ULONG_PTR), reinterpret_cast<ULONG_PTR*>(&info));
    }
    __except (EXCEPTION_EXECUTE_HANDLER)
    {
    }
#else
    UNUSED(MS_VC_EXCEPTION);
#endif
}

void SetThreadName(int id, std::thread& thread)
{
    // Skip this if there is no debugger (as this uses an exception invoke way to perform the operation)
    if (!IsDebuggerPresent())
    {
        return;
    }

    const auto name = GenerateThreadName(id);

    const auto threadId = GetThreadId(thread.native_handle());

    // This new API is not available when cross compiled
#ifdef _MSC_VER
    // TODO: test and enable this
    // SetThreadDescriptionA(threadId, name.c_str());
#endif

    SetThreadNameImpl(threadId, name);
}

void SetThreadNameCurrent(int id)
{
    // Variant not needed
    UNUSED(id);
}

#elif __APPLE__

void SetThreadName(int id, std::thread& thread)
{
    UNUSED(id);
    UNUSED(thread);

    // Apple doesn't take in the thread handle here, so need to be set by the thread itself
}

void SetThreadNameCurrent(int id)
{
    pthread_setname_np(GenerateThreadName(id).c_str());
}

#else
// Assume standard pthreads on Linux or another UNIX type

void SetThreadName(int id, std::thread& thread)
{
    pthread_setname_np(thread.native_handle(), GenerateThreadName(id).c_str());
}

void SetThreadNameCurrent(int id)
{
    // Variant not needed
    UNUSED(id);
}

#endif

TaskSystem::QueuedTask::QueuedTask(SimpleCallable callable)
{
    Type = TaskType::Simple;
    Simple = callable;
}

#pragma clang diagnostic push
#pragma ide diagnostic ignored "cppcoreguidelines-pro-type-member-init"

#ifdef USE_LOCK_FREE_QUEUE
TaskSystem::QueuedTask::QueuedTask() : Type(TaskType::Cleared)
{
}
#endif

TaskSystem::QueuedTask::QueuedTask(std::function<void()> callable) : Type(TaskType::StdFunction)
{
    new (&Function) std::function<void()>(std::move(callable));
}

/*TaskSystem::QueuedTask::QueuedTask(std::function<void()>&& callable)
{
    Type = TaskType::StdFunction;
    new (&Function) std::function<void()>(std::move(callable));
}*/

TaskSystem::QueuedTask::QueuedTask(QuitSentinel quit) : Type(TaskType::Quit)
{
    UNUSED(quit);
}

TaskSystem::QueuedTask::QueuedTask(QueuedTask&& other) noexcept : Type(other.Type)
{
    MoveDataFromOther(std::move(other));
}

#pragma clang diagnostic pop

void TaskSystem::QueuedTask::Invoke() const
{
    switch (Type)
    {
        case TaskType::Cleared:
            return;
        case TaskType::Quit:
            LOG_ERROR("Can't execute quit command");
            break;
        case TaskType::Simple:
            Simple();
            break;
        case TaskType::StdFunction:
            Function();
            break;
    }
}

TaskSystem::QueuedTask& TaskSystem::QueuedTask::operator=(QueuedTask&& other) noexcept
{
    if (other.Type != Type)
    {
        ReleaseCurrentData();
        Type = other.Type;
    }

    MoveDataFromOther(std::move(other));

    return *this;
}

void TaskSystem::QueuedTask::ReleaseCurrentData()
{
    switch (Type)
    {
        case TaskType::StdFunction:
            Function.~function<void()>();
            break;
        default:
            break;
    }
}

void TaskSystem::QueuedTask::MoveDataFromOther(QueuedTask&& other)
{
#ifndef NDEBUG
    if (Type != other.Type)
    {
        LOG_ERROR("QueuedTask types must match before copying data");
        std::abort();
    }
#endif

    switch (other.Type)
    {
        case TaskType::Cleared:
        case TaskType::Quit:
            break;
        case TaskType::Simple:
            Simple = other.Simple;
            break;
        case TaskType::StdFunction:
            new (&Function) std::function<void()>(std::move(other.Function));
            break;
    }
}

TaskSystem::TaskSystem() :
#ifdef USE_LOCK_FREE_QUEUE
    // Must have enough queue size to not deadlock when running with 32 threads (untested if this works with more than
    // 32 threads, but hopefully this does)
    taskQueue(JPH::cMaxPhysicsJobs),
#endif
    queueLock(queueMutex)
{
    // Mark main thread
    MainThreadIdentifier = MAIN_THREAD;

#ifdef USE_OBJECT_POOLS
    jobPool.Init(JPH::cMaxPhysicsJobs, JPH::cMaxPhysicsJobs);
#endif

    Init(JPH::cMaxPhysicsBarriers);

    queueLock.unlock();

    // Start at least one thread initially
    SetThreads(1);
}

TaskSystem::~TaskSystem()
{
    Shutdown();
    std::atomic_thread_fence(std::memory_order::seq_cst);
}

void TaskSystem::Shutdown()
{
    runThreads = false;

    // End all threads and wait for them
    while (threadCount > 0)
    {
        EndTaskThread();
    }

    try
    {
        for (auto& thread : taskThreads)
        {
            thread.join();
        }

        taskThreads.clear();
    }
    catch (const std::exception& e)
    {
        LOG_ERROR(std::string("Failed to join a task thread: ") + e.what());
    }

    // Jolt's default thread pool executes jobs that are still queued after all
    // workers have stopped. Do this only for final shutdown; during a resize,
    // queued jobs must remain available to the newly started workers.
    {
#ifdef USE_LOCK_FREE_QUEUE
        Job* job;
        while (jobQueue.try_dequeue(job))
        {
            job->Execute();
            job->Release();
        }
#else
        std::unique_lock<std::mutex> lock(queueMutex);
        while (!jobQueue.empty())
        {
            Job* job = jobQueue.front();
            jobQueue.pop();
            lock.unlock();
            job->Execute();
            job->Release();
            lock.lock();
        }
#endif
    }

#ifdef USE_LOCK_FREE_QUEUE
    // Empty out the queue
    for (int i = 0; i < 5; ++i)
    {
        QueuedTask task;
        while (taskQueue.try_dequeue(task))
        {
        }

        Job* job;
        while (jobQueue.try_dequeue(job))
            job->Release();
    }
#endif
}

bool TaskSystem::IsOnMainThread()
{
    return MainThreadIdentifier == MAIN_THREAD;
}

void TaskSystem::AssertIsMainThread()
{
    if (IsOnMainThread()) [[likely]]
    {
        return;
    }

    LOG_ERROR("Operation that should have been on the main thread is not ran on the main thread");
    DEBUG_BREAK;
    std::abort();
}

// ------------------------------------ //
#ifdef USE_LOCK_FREE_QUEUE

void TaskSystem::TryEnqueueTask(QueuedTask&& task)
{
    int retryCount = 0;

    // The move should only take effect after the move succeeds
#pragma clang diagnostic push
#pragma ide diagnostic ignored "bugprone-use-after-move"

    // Retry the move until there is room in the queue
    while (!taskQueue.try_enqueue(std::move(task)))
    {
        ++retryCount;

        if (retryCount > 2)
        {
            // Start sleeping the thread if it has taken a lot of time
            if (retryCount > 100)
            {
                if (retryCount > 1000)
                {
                    LOG_ERROR("Task system stuck trying to add new jobs to the queue");
                }

                std::this_thread::sleep_for(MicrosecondDuration(900));
            }
            else if (retryCount > 65)
            {
                std::this_thread::yield();
            }
            else
            {
                HYPER_THREAD_YIELD;
            }
        }
    }

#pragma clang diagnostic pop
}
#endif

// ------------------------------------ //

void TaskSystem::QueueTask(QueuedTask&& task)
{
#ifdef USE_LOCK_FREE_QUEUE
    TryEnqueueTask(std::move(task));
#else
    queueLock.lock();

    taskQueue.emplace(std::move(task));

    queueLock.unlock();
#endif

    queueNotify.Release();
}

void TaskSystem::QueueTaskFromBackgroundThread(QueuedTask&& task)
{
#ifdef USE_LOCK_FREE_QUEUE
    TryEnqueueTask(std::move(task));
#else
    std::lock_guard<std::mutex> lock(queueMutex);

    taskQueue.emplace(std::move(task));
#endif

    queueNotify.Release();
}

// ------------------------------------ //
TaskSystem::JobHandle TaskSystem::CreateJob(
    const char* inName, JPH::ColorArg inColor, const JobFunction& inJobFunction, uint32_t inNumDependencies)
{
    Job* job;

#ifdef USE_OBJECT_POOLS
    uint32_t index;
    do
    {
        index = jobPool.ConstructObject(inName, inColor, this, inJobFunction, inNumDependencies);
        if (index == JPH::FixedSizeFreeList<Job>::cInvalidObjectIndex)
            std::this_thread::yield();
    } while (index == JPH::FixedSizeFreeList<Job>::cInvalidObjectIndex);

    job = &jobPool.Get(index);

#else
    job = new Job(inName, inColor, this, inJobFunction, inNumDependencies);
#endif

    JobHandle handle(job);

    if (inNumDependencies == 0)
        QueueJob(job);

    return handle;
}

void TaskSystem::FreeJob(Job* inJob)
{
#ifdef USE_OBJECT_POOLS
    jobPool.DestructObject(inJob);
#else
    delete inJob;
#endif
}

void TaskSystem::QueueJob(Job* inJob)
{
    inJob->AddRef();

#ifdef USE_LOCK_FREE_QUEUE
    jobQueue.enqueue(inJob);
#else
    std::lock_guard<std::mutex> lock(queueMutex);
    jobQueue.emplace(inJob);
#endif

    queueNotify.Release();
}

void TaskSystem::QueueJobs(Job** inJobs, uint32_t inNumJobs)
{
#ifdef USE_LOCK_FREE_QUEUE
    for (size_t i = 0; i < inNumJobs; ++i)
        inJobs[i]->AddRef();

    jobQueue.enqueue_bulk(inJobs, inNumJobs);
#else
    std::lock_guard<std::mutex> lock(queueMutex);

    for (size_t i = 0; i < inNumJobs; ++i)
    {
        inJobs[i]->AddRef();
        jobQueue.emplace(inJobs[i]);
    }
#endif

    queueNotify.Release(std::min(inNumJobs, static_cast<uint32_t>(targetThreadCount)));
}

// ------------------------------------ //
void TaskSystem::SetThreads(int count) noexcept
{
    AssertIsMainThread();

    if (count < 1)
    {
        LOG_ERROR("Thread count can't be less than 1");
        count = 1;
    }

    if (!runThreads)
    {
        LOG_ERROR("Task executor has already been shutdown");
        return;
    }

    targetThreadCount = count;

    // Quit sentinels are consumed by whichever worker reaches them first, so
    // they cannot be used to stop a particular subset of taskThreads. If the
    // pool is shrinking, stop and join every worker before starting the new
    // pool. This guarantees that every std::thread object being removed has
    // actually exited.
    if (targetThreadCount < threadCount)
    {
        while (threadCount > 0)
            EndTaskThread();

        for (auto& thread : taskThreads)
            thread.join();

        taskThreads.clear();
    }

    // Start new threads, either adding the requested workers or rebuilding the
    // pool after a reduction.
    while (targetThreadCount > threadCount)
    {
        StartTaskThread();
    }
}

// ------------------------------------ //
void TaskSystem::StartTaskThread()
{
    const auto threadId = ThreadIdentifierNumber.fetch_add(1);

    auto thread = std::thread(&TaskSystem::RunTaskThread, this, threadId);

    SetThreadName(threadId, thread);

    taskThreads.push_back(std::move(thread));

    ++threadCount;
}

void TaskSystem::EndTaskThread()
{
#ifdef USE_LOCK_FREE_QUEUE
    TryEnqueueTask(QueuedTask(QuitSentinel()));
#else
    std::lock_guard<std::mutex> lock(queueMutex);
    taskQueue.emplace(QuitSentinel());
#endif

    queueNotify.Release();

    --threadCount;
}

// ------------------------------------ //
void TaskSystem::RunTaskThread(int id)
{
    SetThreadNameCurrent(id);

    while (runThreads)
    {
        queueNotify.Acquire();

        // Process all currently available tasks. The semaphore ensures that a
        // notification cannot be lost while using the lock-free queue.
#ifdef USE_LOCK_FREE_QUEUE
        Job* job;
        while (jobQueue.try_dequeue(job))
        {
            job->Execute();
            job->Release();
        }

        QueuedTask task;
        while (taskQueue.try_dequeue(task))
#else
        std::unique_lock<std::mutex> lock(queueMutex);
        while (!jobQueue.empty() || !taskQueue.empty())
#endif
        {
#ifndef USE_LOCK_FREE_QUEUE
            if (!jobQueue.empty())
            {
                Job* job = jobQueue.front();
                jobQueue.pop();
                lock.unlock();
                job->Execute();
                job->Release();
                lock.lock();
                continue;
            }
#endif
            {
#ifndef USE_LOCK_FREE_QUEUE
                const auto task = std::move(taskQueue.front());
                taskQueue.pop();

                // Unlock while running the task.
                lock.unlock();
#endif
                if (task.Type == TaskType::Quit)
                    return;

                try
                {
                    task.Invoke();
                }
                catch (const std::exception& e)
                {
                    LOG_ERROR(std::string("Background task exception: ") + e.what());
                    throw;
                }
            }

#ifndef USE_LOCK_FREE_QUEUE
            // The task must be destroyed before reacquiring the queue lock.
            lock.lock();
#endif
        }

#ifndef USE_LOCK_FREE_QUEUE
        lock.unlock();
#endif
    }
}

} // namespace Thrive
