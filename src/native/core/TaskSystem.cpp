// ------------------------------------ //
#include "TaskSystem.hpp"

#include "Jolt/Physics/PhysicsSettings.h"

#include "Logger.hpp"
#include "Time.hpp"

#ifdef _WIN32
#define WIN32_LEAN_AND_MEAN
#include <debugapi.h>
#include <processthreadsapi.h>

#include "windows.h"
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

TaskSystem::QueuedTask::QueuedTask(Job* callable) : Type(TaskType::JoltJob)
{
    callable->AddRef();
    Jolt = callable;
}

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
        case TaskType::JoltJob:
            // TODO: handle the return value?
            Jolt->Execute();
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
        case TaskType::JoltJob:
            Jolt->Release();
            Jolt = nullptr;
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
        case TaskType::JoltJob:
            // Steal the job from the other one
            other.Type = TaskType::Cleared;
            Jolt = other.Jolt;
            break;
    }
}

TaskSystem::TaskSystem() :
#ifdef USE_LOCK_FREE_QUEUE
    // TODO: pick a reasonable queue size (right now assumed that all possible jobs are not queued at once)
    taskQueue(JPH::cMaxPhysicsJobs / 2),
#endif
    queueLock(queueMutex)
{
    // Mark main thread
    MainThreadIdentifier = MAIN_THREAD;

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

    // A duplicate notify compared to the EndTaskThread method but this feels better to ensure all threads are woken
    // up if they were waiting immediately on shutdown
    queueNotify.notify_all();

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

#ifdef USE_LOCK_FREE_QUEUE
    // Empty out the queue
    for (int i = 0; i < 5; ++i)
    {
        QueuedTask task;
        while (taskQueue.try_dequeue(task))
        {
        }
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
            else
            {
                std::this_thread::yield();
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

    queueNotify.notify_one();
}

void TaskSystem::QueueTaskFromBackgroundThread(QueuedTask&& task)
{
#ifdef USE_LOCK_FREE_QUEUE
    TryEnqueueTask(std::move(task));
#else
    std::lock_guard<std::mutex> lock(queueMutex);

    taskQueue.emplace(std::move(task));
#endif

    queueNotify.notify_one();
}

// ------------------------------------ //
TaskSystem::JobHandle TaskSystem::CreateJob(
    const char* inName, JPH::ColorArg inColor, const JobFunction& inJobFunction, uint32_t inNumDependencies)
{
    Job* job;

#ifdef USE_OBJECT_POOLS
    {
        std::lock_guard<std::mutex> lock(jobPoolMutex);
        job = jobPool.malloc();
    }

    ::new (job) Job(inName, inColor, this, inJobFunction, inNumDependencies);

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
    std::lock_guard<std::mutex> lock(jobPoolMutex);

    jobPool.destroy(inJob);
#else
    delete inJob;
#endif
}

void TaskSystem::QueueJob(Job* inJob)
{
#ifdef USE_LOCK_FREE_QUEUE
    TryEnqueueTask(QueuedTask(inJob));
#else
    std::lock_guard<std::mutex> lock(queueMutex);
    taskQueue.emplace(inJob);
#endif

    queueNotify.notify_one();
}

void TaskSystem::QueueJobs(Job** inJobs, uint32_t inNumJobs)
{
#ifdef USE_LOCK_FREE_QUEUE
    // TODO: should try_enqueue_bulk be used instead (at least when num jobs is over 2)?
    for (size_t i = 0; i < inNumJobs; ++i)
    {
        TryEnqueueTask(QueuedTask(inJobs[i]));
    }
#else
    std::lock_guard<std::mutex> lock(queueMutex);

    for (size_t i = 0; i < inNumJobs; ++i)
    {
        taskQueue.emplace(inJobs[i]);
    }
#endif

    if (inNumJobs > 4)
    {
        queueNotify.notify_all();
    }
    else if (inNumJobs > 3)
    {
        queueNotify.notify_one();
        queueNotify.notify_one();
    }
    else
    {
        queueNotify.notify_one();
    }
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

    queueLock.lock();

    targetThreadCount = count;

    // Start new threads
    while (targetThreadCount > threadCount)
    {
        StartTaskThread();
    }

    // Or stop threads when there are too many
    while (targetThreadCount < threadCount)
    {
        EndTaskThread();
    }

    queueLock.unlock();

    // TODO: where should this thread cleaning exist? (here it is not possible to know really which threads have exited)
    /*for (auto iter = taskThreads.begin(); iter != taskThreads.end(); )
    {
    }*/
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
    queueLock.lock();

    taskQueue.emplace(QuitSentinel());

    queueLock.unlock();
#endif

    queueNotify.notify_one();

    --threadCount;
}

// ------------------------------------ //
void TaskSystem::RunTaskThread(int id)
{
    const auto threadWait = MillisecondDuration(8);

    std::unique_lock<std::mutex> lock{queueMutex};

    SetThreadNameCurrent(id);

#ifdef USE_LOCK_FREE_QUEUE
    lock.unlock();
#endif

    while (runThreads)
    {
        bool processed = false;

#ifdef USE_LOCK_FREE_QUEUE
        lock.lock();
#endif

        queueNotify.wait_for(lock, threadWait);

#ifdef USE_LOCK_FREE_QUEUE
        lock.unlock();
#endif

        for (int i = 0; i < TASK_WAIT_LOOP_COUNT; ++i)
        {
            // Process tasks until empty before waiting again
#ifdef USE_LOCK_FREE_QUEUE
            // TODO: should this variable be in the outer scope?
            QueuedTask task;
            while (taskQueue.try_dequeue(task))
#else
            while (!taskQueue.empty())
#endif
            {
                {
#ifndef USE_LOCK_FREE_QUEUE
                    const auto task = std::move(taskQueue.front());

                    taskQueue.pop();

                    // Unlock while running the task
                    lock.unlock();
#endif

                    if (task.Type == TaskType::Quit)
                    {
                        return;
                    }

                    processed = true;

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
                // Extra scope used above to not lock this again until needed (task is destructed)
                lock.lock();
#endif
            }

#ifdef USE_LOCK_FREE_QUEUE
            /*if (!processed)
            {
                // Reduce looping speed while the queue is empty
                HYPER_THREAD_YIELD;
            }*/
#endif

            // If we woke up but didn't find any work, go back to sleep
            if (!processed)
            {
                break;
            }
        }
    }
}

} // namespace Thrive
