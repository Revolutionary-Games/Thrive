#pragma once

#include <atomic>

#include "boost/intrusive_ptr.hpp"

#include "Include.h"

#include "NonCopyable.hpp"
#include "Reference.hpp"

namespace Thrive
{

/// \brief Base for all reference counted objects
///
/// We use intrusive pointers for our objects to allow the C interop interface to also work
class RefCounted : NonCopyable
{
    // Protected to avoid anyone using this class directly
protected:
    inline THRIVE_NATIVE_API RefCounted() : refCount(0)
    {
        // The reference count is started at one so that it is easy to create an instance of this class and just then
        // put this in an intrusive_ptr
    }

    inline THRIVE_NATIVE_API ~RefCounted() = default;

public:
    /// \brief Adds one to the reference count of this object
    FORCE_INLINE void AddRef() const
    {
        intrusive_ptr_add_ref(this);
    }

    /// \brief removes a reference and deletes the object if reference count reaches zero
    FORCE_INLINE void Release() const
    {
        intrusive_ptr_release(this);
    }

protected:
    friend void intrusive_ptr_add_ref(const RefCounted* obj)
    {
        // TODO: have someone really knowledgeable determine what's the right memory order here and in release
        // obj->refCount.fetch_add(1, std::memory_order_relaxed);
        obj->refCount.fetch_add(1, std::memory_order_release);
    }

    friend void intrusive_ptr_release(const RefCounted* obj)
    {
        // if (obj->refCount.fetch_sub(1, std::memory_order_release) == 1)

        if (obj->refCount.fetch_sub(1, std::memory_order_acq_rel) == 1)
        {
            std::atomic_thread_fence(std::memory_order_acquire);
            delete obj;
        }
    }

private:
    mutable std::atomic_int_fast32_t refCount;
};

} // namespace Thrive
