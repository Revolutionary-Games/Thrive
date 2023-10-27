#pragma once

#include <atomic>

#include "boost/intrusive_ptr.hpp"

#include "Include.h"

#include "NonCopyable.hpp"
#include "Reference.hpp"

#ifdef USE_OBJECT_POOLS
#include "boost/pool/singleton_pool.hpp"

#include "Reference.hpp"
#endif

namespace Thrive
{

/// \brief Base for all reference counted objects (can't be used directly)
///
/// We use intrusive pointers for our objects to allow the C interop interface to also work
class RefCountedBase : NonCopyable
{
    // Protected to avoid anyone using this class directly
protected:
    inline THRIVE_NATIVE_API RefCountedBase() : refCount(0)
    {
        // The reference count is started at one so that it is easy to create an instance of this class and just then
        // put this in an intrusive_ptr
    }

    inline THRIVE_NATIVE_API virtual ~RefCountedBase() noexcept = default;

public:
    /// \brief Adds one to the reference count of this object
    FORCE_INLINE void AddRef() const
    {
        intrusive_ptr_add_ref(this);
    }

protected:
    friend void intrusive_ptr_add_ref(const RefCountedBase* obj)
    {
        // TODO: have someone really knowledgeable determine what's the right memory order here and in release
        // https://en.cppreference.com/w/cpp/atomic/memory_order

        // Jolt also uses this approach, so it should be good
        obj->refCount.fetch_add(1, std::memory_order_relaxed);

        // obj->refCount.fetch_add(1, std::memory_order_release);
    }

protected:
    mutable std::atomic_int_fast32_t refCount;
};

/// \brief RefCounted variant that uses a custom release method to delete the object (used for pooled objects)
template<class AllocatedPtrT>
class RefCountedWithRelease : public RefCountedBase
{
public:
    using ReleaseCallback = void (*)(const AllocatedPtrT* ptr);

    // Protected to avoid anyone using this class directly
protected:
    explicit inline THRIVE_NATIVE_API RefCountedWithRelease(ReleaseCallback deleteCallback) :
        customDelete(deleteCallback)
    {
    }

public:
    /// \brief removes a reference and deletes the object with the custom callback if the reference count falls to 0
    FORCE_INLINE void Release() const
    {
        // This cast to derived type is needed here to make the call work. This should be fine hopefully if no one is
        // crazy enough to give the wrong class as template parameter to this class when inheriting from this.
        intrusive_ptr_release(static_cast<const AllocatedPtrT*>(this));
    }

protected:
    friend void intrusive_ptr_release(const AllocatedPtrT* obj)
    {
        // if (obj->refCount.fetch_sub(1, std::memory_order_acq_rel) == 1)
        // Jolt also uses this
        if (obj->refCount.fetch_sub(1, std::memory_order_release) == 1)
        {
            std::atomic_thread_fence(std::memory_order_acquire);

            obj->customDelete(obj);
        }
    }

private:
    ReleaseCallback customDelete;
};

/// \brief RefCounted variant that doesn't have a custom release method for slightly more performance in cases that
/// don't need the extra complexity
class RefCountedBasic : public RefCountedBase
{
public:
    /// \brief removes a reference and deletes the object if reference count reaches zero
    FORCE_INLINE void Release() const
    {
        intrusive_ptr_release(this);
    }

protected:
    friend void intrusive_ptr_release(const RefCountedBasic* obj)
    {
        // if (obj->refCount.fetch_sub(1, std::memory_order_acq_rel) == 1)
        // Jolt also uses this
        if (obj->refCount.fetch_sub(1, std::memory_order_release) == 1)
        {
            std::atomic_thread_fence(std::memory_order_acquire);
            delete obj;
        }
    }
};

#ifdef USE_OBJECT_POOLS

// Releasing global pool using helpers
template<class ObjectT, int PoolAllocSize>
inline void ReleaseWithGlobalPool(const ObjectT* obj)
{
    // Pool handles just bytes meaning we need to handle destroying the object
    obj->~ObjectT();

    // Cast the pointer back to void and remove the const qualifier to get the original pointer result of the pool
    // malloc method back in order to free the object for reuse
    boost::singleton_pool<ObjectT, PoolAllocSize>::free(const_cast<void*>(static_cast<const void*>(obj)));
}

template<class ObjectT>
FORCE_INLINE inline void ReleaseWithGlobalPool(const ObjectT* obj)
{
    ReleaseWithGlobalPool<ObjectT, sizeof(ObjectT)>(obj);
}

// Raw pointer object allocations from their global pool, note the non-raw variants should be preferred in user code
// for safety. These raw variants have to manually be wrapped in an intrusive_ptr or called AddRef on
template<class ObjectT, int PoolAllocSize, class ArgT>
inline ObjectT* ConstructFromGlobalPoolRaw(ArgT&& arg)
{
    // Pool handles only bytes so use a placement new to construct the instance
    void* ptr = boost::singleton_pool<ObjectT, PoolAllocSize>::malloc();

    return new (ptr) ObjectT(std::forward<ArgT>(arg), &ReleaseWithGlobalPool<ObjectT, PoolAllocSize>);
}

template<class ObjectT, class ArgT>
FORCE_INLINE inline ObjectT* ConstructFromGlobalPoolRaw(ArgT&& arg)
{
    return ConstructFromGlobalPoolRaw<ObjectT, sizeof(ObjectT), ArgT>(std::forward<ArgT>(arg));
}

template<class ObjectT, int PoolAllocSize, class... ArgT>
inline ObjectT* ConstructFromGlobalPoolRaw(ArgT&&... arg)
{
    void* ptr = boost::singleton_pool<ObjectT, PoolAllocSize>::malloc();

    return new (ptr) ObjectT(std::forward<ArgT>(arg)..., &ReleaseWithGlobalPool<ObjectT, PoolAllocSize>);
}

template<class ObjectT, class... ArgT>
FORCE_INLINE inline ObjectT* ConstructFromGlobalPoolRaw(ArgT&&... arg)
{
    return ConstructFromGlobalPoolRaw<ObjectT, sizeof(ObjectT), ArgT...>(std::forward<ArgT>(arg)...);
}

// Constructing objects from their global pool and returning them as safe Ref (intrusive_ptr) that holds the object
template<class ObjectT, int PoolAllocSize, class ArgT>
inline Ref<ObjectT> ConstructFromGlobalPool(ArgT&& arg)
{
    return Ref<ObjectT>(
        ConstructFromGlobalPoolRaw<ObjectT, PoolAllocSize, ArgT>(std::forward<ArgT>(arg)));
}

template<class ObjectT, class ArgT>
FORCE_INLINE inline Ref<ObjectT> ConstructFromGlobalPool(ArgT&& arg)
{
    return ConstructFromGlobalPool<ObjectT, sizeof(ObjectT), ArgT>(std::forward<ArgT>(arg));
}

template<class ObjectT, int PoolAllocSize, class... ArgT>
inline Ref<ObjectT> ConstructFromGlobalPool(ArgT&&... arg)
{
    return Ref<ObjectT>(
        ConstructFromGlobalPoolRaw<ObjectT, PoolAllocSize, ArgT...>(std::forward<ArgT>(arg)...));
}

template<class ObjectT, class... ArgT>
FORCE_INLINE inline Ref<ObjectT> ConstructFromGlobalPool(ArgT&&... arg)
{
    return ConstructFromGlobalPool<ObjectT, sizeof(ObjectT), ArgT...>(std::forward<ArgT>(arg)...);
}

/// When using pooling the reference counted type to use needs a callback to deallocate from the right pool
template<class AllocatedPtrT>
using RefCounted = RefCountedWithRelease<AllocatedPtrT>;
#else
/// Plain reference counted type. This takes in a template argument to be syntax compatible with the pooled variant
template<class AllocatedPtrT>
using RefCounted = RefCountedBasic;
#endif

} // namespace Thrive
