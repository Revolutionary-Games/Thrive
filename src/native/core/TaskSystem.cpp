// ------------------------------------ //
#include "TaskSystem.hpp"

#include "Jolt/Physics/PhysicsSettings.h"

#include "Logger.hpp"
#include "Time.hpp"

#ifdef _WIN32
#include <debugapi.h>
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
    return "TNatTask_" + std::to_string(id);
}

#ifdef _WIN32

// Thread rename trick on Windows
const DWORD MS_VC_EXCEPTION = 0x406D1388;

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

    __try
    {
        RaiseException(MS_VC_EXCEPTION, 0, sizeof(info) / sizeof(ULONG_PTR), reinterpret_cast<ULONG_PTR*>(&info));
    }
    __except (EXCEPTION_EXECUTE_HANDLER)
    {
    }
}

void SetThreadName(int id, std::thread& thread)
{
    // Skip this if there is no debugger (as this uses an exception invoke way to perform the operation)
    if (!IsDebuggerPresent())
        return;

    const auto nativehandle = GetThreadId(thread.native_handle());

    SetThreadNameImpl(nativehandle, name);
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

struct QuitSentinel
{
};

struct TaskSystem::QueuedTask
{
public:
    explicit QueuedTask(SimpleCallable callable)
    {
        Type = TaskType::Simple;
        Simple = callable;
    }

#pragma clang diagnostic push
#pragma ide diagnostic ignored "cppcoreguidelines-pro-type-member-init"

    explicit QueuedTask(MethodAndInstance callable)
    {
        Type = TaskType::Instance;
        Instance = callable;
    }

    explicit QueuedTask(std::function<void()> callable)
    {
        Type = TaskType::StdFunction;
        new (&Function) std::function<void()>(std::move(callable));
    }

    explicit QueuedTask(std::function<void()>&& callable)
    {
        Type = TaskType::StdFunction;
        new (&Function) std::function<void()>(std::move(callable));
    }

    explicit QueuedTask(Job* callable)
    {
        Type = TaskType::JoltJob;
        callable->AddRef();
        Jolt = callable;
    }

    explicit QueuedTask(QuitSentinel quit)
    {
        UNUSED(quit);
        Type = TaskType::Quit;
    }

#pragma clang diagnostic pop

    QueuedTask(const QueuedTask& other) = delete;

    QueuedTask(QueuedTask&& other) noexcept
    {
        Type = other.Type;

        switch (other.Type)
        {
            case TaskType::Cleared:
            case TaskType::Quit:
                break;
            case TaskType::Simple:
                Simple = other.Simple;
                break;
            case TaskType::Instance:
                Instance = other.Instance;
                break;
            case TaskType::StdFunction:
                new (&Function) std::function<void()>(std::move(other.Function));
                break;
            case TaskType::JoltJob:
                other.Type = TaskType::Cleared;
                Jolt = other.Jolt;
                break;
        }
    }

    ~QueuedTask()
    {
        ReleaseCurrentData();
    }

    void Invoke() const
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
            case TaskType::Instance:
                Instance.Method(Instance.Instance);
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

    QueuedTask& operator=(QueuedTask&& other) noexcept
    {
        if (other.Type != Type)
        {
            ReleaseCurrentData();
            Type = other.Type;
        }

        switch (other.Type)
        {
            case TaskType::Cleared:
            case TaskType::Quit:
                break;
            case TaskType::Simple:
                Simple = other.Simple;
                break;
            case TaskType::Instance:
                Instance = other.Instance;
                break;
            case TaskType::JoltJob:
                other.Type = TaskType::Cleared;
                Jolt = other.Jolt;
                break;
            case TaskType::StdFunction:
                Function = std::move(other.Function);
                break;
        }

        return *this;
    }

    QueuedTask& operator=(const QueuedTask& other) = delete;

    union
    {
        SimpleCallable Simple;

        MethodAndInstance Instance;

        std::function<void()> Function;

        Job* Jolt;
    };

    TaskType Type;

private:
    /// Properly releases the union members that require releasing
    void ReleaseCurrentData()
    {
        switch (Type)
        {
            case TaskType::StdFunction:
                Function.~function<void()>();
            case TaskType::JoltJob:
                Jolt->Release();
                Jolt = nullptr;
                break;
            default:
                break;
        }
    }
};

TaskSystem::TaskSystem() : queueLock(queueMutex)
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
}

void TaskSystem::Shutdown()
{
    runThreads = false;

    // End all threads and wait for them
    while (threadCount > 0)
    {
        EndTaskThread();
    }

    for (auto& thread : taskThreads)
    {
        thread.join();
    }
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
    std::abort();
}

// ------------------------------------ //
void TaskSystem::QueueTask(TaskSystem::SimpleCallable callable)
{
    queueLock.lock();

    taskQueue.emplace(callable);

    queueLock.unlock();

    queueNotify.notify_one();
}

void TaskSystem::QueueTaskFromBackgroundThread(TaskSystem::SimpleCallable callable)
{
    std::unique_lock<std::mutex> lock(queueMutex);

    taskQueue.emplace(callable);

    queueNotify.notify_one();
}

// ------------------------------------ //
TaskSystem::JobHandle TaskSystem::CreateJob(
    const char* inName, JPH::ColorArg inColor, const JobFunction& inJobFunction, uint32_t inNumDependencies)
{
#ifdef USE_OBJECT_POOLS
    auto job = jobPool.malloc();

    ::new (job) Job(inName, inColor, this, inJobFunction, inNumDependencies);

#else
    auto job = new Job(inName, inColor, this, inJobFunction, inNumDependencies);
#endif

    JobHandle handle(job);

    if (inNumDependencies == 0)
        QueueJob(job);

    return handle;
}

void TaskSystem::FreeJob(Job* inJob)
{
#ifdef USE_OBJECT_POOLS
    jobPool.destroy(inJob);
#else
    delete inJob;
#endif
}

void TaskSystem::QueueJob(Job* inJob)
{
    std::unique_lock<std::mutex> lock(queueMutex);
    taskQueue.emplace(inJob);

    queueNotify.notify_one();
}

void TaskSystem::QueueJobs(Job** inJobs, uint inNumJobs)
{
    std::unique_lock<std::mutex> lock(queueMutex);

    for (size_t i = 0; i < inNumJobs; ++i)
    {
        taskQueue.emplace(inJobs[i]);
    }

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
    queueLock.lock();

    taskQueue.emplace(QuitSentinel());

    queueLock.unlock();

    --threadCount;
}

// ------------------------------------ //
void TaskSystem::RunTaskThread(int id)
{
    const auto threadWait = MillisecondDuration(5);

    std::unique_lock<std::mutex> lock{queueMutex};

    SetThreadNameCurrent(id);

    while (runThreads)
    {
        queueNotify.wait_for(lock, threadWait);

        for (int i = 0; i < TASK_WAIT_LOOP_COUNT; ++i)
        {
            bool processed = false;

            // Process tasks until empty before waiting again
            while (!taskQueue.empty())
            {
                {
                    const auto task = std::move(taskQueue.front());

                    taskQueue.pop();

                    // Unlock while running the task
                    lock.unlock();
                    processed = true;

                    if (task.Type == TaskType::Quit)
                    {
                        break;
                    }

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

                // Extra scope used above to not lock this again until needed
                lock.lock();
            }

            if (!processed)
            {
                HYPER_THREAD_YIELD;
            }
        }
    }
}

} // namespace Thrive
