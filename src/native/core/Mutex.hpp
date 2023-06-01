#pragma once

#include <mutex>
#include <shared_mutex>

// Jolt says the mutex is not fast for all platforms so here's some flexibility to allow redefining it

namespace Thrive
{
using Mutex = std::mutex;
using SharedMutex = std::shared_mutex;
using Lock = std::lock_guard<Mutex>;
using SharedLock = std::lock_guard<SharedMutex>;
} // namespace Thrive
