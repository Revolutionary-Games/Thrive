#pragma once

#include <atomic>

#include "NonCopyable.hpp"

namespace Thrive
{

/// \brief A simple spinlock implemented with an atomic flag
///
/// For extra info see: https://www.talkinghightech.com/en/implementing-a-spinlock-in-c/ and
/// https://rigtorp.se/spinlock/
class Spinlock final : NonCopyable
{
public:
    Spinlock() = default;

    void Lock() noexcept
    {
        while (true)
        {
            // Optimized approach by first trying to just get the flag once
            if (!lockedFlag.test_and_set(std::memory_order_acquire))
            {
                // Lock was acquired
                break;
            }

            // And if it fails falling back to a cheaper memory order to keep testing when the flag is unset and going
            // back to retrying the lock
            while (lockedFlag.test(std::memory_order_relaxed))
            {
                // Spin while waiting

                // Reduce hyperthreaded core usage to work more efficiently on modern systems
                HYPER_THREAD_YIELD;

                // TODO: create a hybrid lock variant that falls back to a condition variable after some time to let
                // the thread sleep
            }
        }
    }

    bool TryLock() noexcept
    {
        // This does a test first to optimize the case where TryLock is used in a while loop
        return !lockedFlag.test(std::memory_order_relaxed) && !lockedFlag.test_and_set(std::memory_order_acquire);
    }

    void Unlock() noexcept
    {
        lockedFlag.clear(std::memory_order_release);
    }

private:
    std::atomic_flag lockedFlag = ATOMIC_FLAG_INIT;
};

} // namespace Thrive
