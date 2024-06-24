#pragma once

#include <cstring>
#include <memory>
#include <vector>

#include "Jolt/Core/Reference.h"
#include "Jolt/Jolt.h"
#include "Jolt/Physics/Body/Body.h"
#include "Jolt/Physics/Body/BodyID.h"

#include "Include.h"
#include "core/ForwardDefinitions.hpp"

#include "core/RefCounted.hpp"

#include "PhysicsCollision.hpp"

// This needs to be included to allow one collision recording method to be inline
#include "PhysicalWorld.hpp"

#ifndef LOCK_FREE_COLLISION_RECORDING
#include "core/Mutex.hpp"
#endif

#ifdef USE_SMALL_VECTOR_POOLS
#include "boost/pool/pool_alloc.hpp"
#endif

namespace JPH
{
class Shape;
} // namespace JPH

namespace Thrive::Physics
{

class PhysicalWorld;
class BodyControlState;

// Flags to put in the physics user data field as a stuffed pointer, max count is UNUSED_POINTER_BITS
constexpr uint64_t PHYSICS_BODY_COLLISION_FILTER_FLAG = 0x1;
constexpr uint64_t PHYSICS_BODY_RECORDING_FLAG = 0x2;
constexpr uint64_t PHYSICS_BODY_DISABLE_COLLISION_FLAG = 0x4;

// Combined flag values
constexpr uint64_t PHYSICS_BODY_SPECIAL_COLLISION_FLAG =
    PHYSICS_BODY_COLLISION_FILTER_FLAG | PHYSICS_BODY_DISABLE_COLLISION_FLAG;

static_assert(STUFFED_POINTER_DATA_MASK ==
    (PHYSICS_BODY_COLLISION_FILTER_FLAG | PHYSICS_BODY_RECORDING_FLAG | PHYSICS_BODY_DISABLE_COLLISION_FLAG));

#ifdef USE_SMALL_VECTOR_POOLS
using IgnoredCollisionList = std::vector<JPH::BodyID, boost::pool_allocator<JPH::BodyID>>;
#else
using IgnoredCollisionList = std::vector<JPH::BodyID>;
#endif

/// \brief Our physics body wrapper that has extra data
class alignas(STUFFED_POINTER_ALIGNMENT) PhysicsBody : public RefCounted<PhysicsBody>
{
    friend PhysicalWorld;
    friend BodyActivationListener;
    friend TrackedConstraint;
    friend ContactListener;

    // Flags used only internally to track some extra state (these are not passed to the user pointer value)
    static constexpr uint64_t EXTRA_FLAG_FILTER_LIST = 32;
    static constexpr uint64_t EXTRA_FLAG_FILTER_CALLBACK = 64;

    // Ensure these extra flags don't leak into the other flag area
    static_assert((EXTRA_FLAG_FILTER_LIST & STUFFED_POINTER_DATA_MASK) == 0);
    static_assert((EXTRA_FLAG_FILTER_CALLBACK & STUFFED_POINTER_DATA_MASK) == 0);

protected:
#ifndef USE_OBJECT_POOLS
    PhysicsBody(JPH::Body* body, JPH::BodyID bodyId) noexcept;
#endif

public:
#ifdef USE_OBJECT_POOLS
    /// Even though this is public this should only be called by PhysicalWorld, so any other code should ask the world
    /// to make new bodies
    PhysicsBody(JPH::Body* body, JPH::BodyID bodyId, ReleaseCallback deleteCallback) noexcept;
#endif

    ~PhysicsBody() noexcept override;

    PhysicsBody(const PhysicsBody& other) = delete;
    PhysicsBody(PhysicsBody&& other) = delete;

    PhysicsBody& operator=(const PhysicsBody& other) = delete;
    PhysicsBody& operator=(PhysicsBody&& other) = delete;

    /// \brief Retrieves an instance of this class from a physics body user data
    [[nodiscard]] FORCE_INLINE static PhysicsBody* FromJoltBody(const JPH::Body* body) noexcept
    {
        return FromJoltBody(body->GetUserData());
    }

    [[nodiscard]] FORCE_INLINE static PhysicsBody* FromJoltBody(uint64_t bodyUserData) noexcept
    {
        bodyUserData &= STUFFED_POINTER_POINTER_MASK;

#ifdef NULL_HAS_UNUSUAL_REPRESENTATION
        if (bodyUserData == 0)
            return nullptr;
#endif

        return reinterpret_cast<PhysicsBody*>(bodyUserData);
    }

    // ------------------------------------ //
    // Recording
    void SetCollisionRecordingTarget(CollisionRecordListType target, int maxCount) noexcept;
    void ClearCollisionRecordingTarget() noexcept;

#ifdef LOCK_FREE_COLLISION_RECORDING
    inline const int32_t* GetRecordedCollisionTargetAddress() const noexcept
    {
        static_assert(
            sizeof(int32_t) == sizeof(activeRecordedCollisionCount), "atomic assumed same size as underlying type");

        return reinterpret_cast<const int32_t*>(&(this->activeRecordedCollisionCount));
    }
#else
    inline const int32_t* GetRecordedCollisionTargetAddress() const noexcept
    {
        return &(this->activeRecordedCollisionCount);
    }
#endif

    // ------------------------------------ //
    // Collision ignores

    bool AddCollisionIgnore(const PhysicsBody& ignoredBody, bool skipDuplicates) noexcept;
    bool RemoveCollisionIgnore(const PhysicsBody& noLongerIgnored) noexcept;

    void SetCollisionIgnores(PhysicsBody* const& ignoredBodies, int ignoreCount) noexcept;
    void SetSingleCollisionIgnore(const PhysicsBody& ignoredBody) noexcept;

    void ClearCollisionIgnores() noexcept;

    inline bool IsBodyIgnored(JPH::BodyID bodyId) const noexcept
    {
        for (const auto& ignored : ignoredCollisions)
        {
            if (ignored == bodyId)
                return true;
        }

        return false;
    }

    inline void SetCollisionFilter(CollisionFilterCallback callback) noexcept
    {
        if (callback == nullptr)
            LOG_ERROR("Collision filter callback set to null (use RemoveCollisionFilter to clear the value)");

        callbackBasedFilter = callback;
    }

    inline void RemoveCollisionFilter() noexcept
    {
        if (activeUserPointerFlags & EXTRA_FLAG_FILTER_CALLBACK)
            LOG_ERROR("Removing collision filter method while it is enabled");

        callbackBasedFilter = nullptr;
    }

    FORCE_INLINE CollisionFilterCallback GetCollisionFilter() const noexcept
    {
        return callbackBasedFilter;
    }

    // ------------------------------------ //
    // State flags

    [[nodiscard]] inline bool IsActive() const noexcept
    {
        return active;
    }

    [[nodiscard]] inline bool IsInWorld() const noexcept
    {
        return containedInWorld != nullptr;
    }

    [[nodiscard]] inline JPH::BodyID GetId() const
    {
        return id;
    }

    [[nodiscard]] const inline auto& GetConstraints() const noexcept
    {
        return constraintsThisIsPartOf;
    }

    [[nodiscard]] inline BodyControlState* GetBodyControlState() const noexcept
    {
        return bodyControlStateIfActive.get();
    }

    // ------------------------------------ //
    // User pointer flags

    inline bool MarkCollisionFilterEnabled() noexcept
    {
        const auto old = activeUserPointerFlags;

        // This and the following set flag methods are a two-step flag, i.e. we have two fields that control one of
        // the primary fields
        activeUserPointerFlags |= EXTRA_FLAG_FILTER_LIST;

        if (old == activeUserPointerFlags)
            return false;

        activeUserPointerFlags |= PHYSICS_BODY_COLLISION_FILTER_FLAG;
        return true;
    }

    inline bool MarkCollisionFilterDisabled() noexcept
    {
        const auto old = activeUserPointerFlags;

        activeUserPointerFlags &= ~EXTRA_FLAG_FILTER_LIST;

        if (old == activeUserPointerFlags)
            return false;

        // Keep the main flag on if the other flag controlling this is still on
        if (activeUserPointerFlags & EXTRA_FLAG_FILTER_CALLBACK)
            return true;

        activeUserPointerFlags &= ~PHYSICS_BODY_COLLISION_FILTER_FLAG;
        return true;
    }

    inline bool MarkCollisionFilterCallbackUsed() noexcept
    {
        if (callbackBasedFilter == nullptr)
        {
            LOG_ERROR("Trying to mark collision filter as enabled when the filter pointer is null");
            return false;
        }

        const auto old = activeUserPointerFlags;

        activeUserPointerFlags |= EXTRA_FLAG_FILTER_CALLBACK;

        if (old == activeUserPointerFlags)
            return false;

        activeUserPointerFlags |= PHYSICS_BODY_COLLISION_FILTER_FLAG;
        return true;
    }

    inline bool MarkCollisionFilterCallbackDisabled() noexcept
    {
        const auto old = activeUserPointerFlags;

        activeUserPointerFlags &= ~EXTRA_FLAG_FILTER_CALLBACK;

        if (old == activeUserPointerFlags)
            return false;

        // Keep the main flag on if the other flag controlling this is still on
        if (activeUserPointerFlags & EXTRA_FLAG_FILTER_LIST)
            return true;

        activeUserPointerFlags &= ~PHYSICS_BODY_COLLISION_FILTER_FLAG;

        return true;
    }

    inline bool MarkCollisionRecordingEnabled() noexcept
    {
        if (collisionRecordingTarget == nullptr || maxCollisionsToRecord < 1)
            LOG_ERROR("Enabling collision record flag without target");

        const auto old = activeUserPointerFlags;

        activeUserPointerFlags |= PHYSICS_BODY_RECORDING_FLAG;

        return old != activeUserPointerFlags;
    }

    inline bool MarkCollisionRecordingDisabled() noexcept
    {
        const auto old = activeUserPointerFlags;

        activeUserPointerFlags &= ~PHYSICS_BODY_RECORDING_FLAG;

        return old != activeUserPointerFlags;
    }

    /// \brief Just a simple way to store this one bool separately in this class, used by PhysicalWorld
    inline bool SetDisableAllCollisions(bool newValue) noexcept
    {
        if (allCollisionsDisabled == newValue)
            return false;

        allCollisionsDisabled = newValue;
        return true;
    }

    inline bool MarkCollisionDisableFlagEnabled() noexcept
    {
        const auto old = activeUserPointerFlags;

        activeUserPointerFlags |= PHYSICS_BODY_DISABLE_COLLISION_FLAG;

        return old != activeUserPointerFlags;
    }

    inline bool MarkCollisionDisableFlagDisabled() noexcept
    {
        const auto old = activeUserPointerFlags;

        activeUserPointerFlags &= ~PHYSICS_BODY_DISABLE_COLLISION_FLAG;

        return old != activeUserPointerFlags;
    }

    [[nodiscard]] inline uint64_t CalculateUserPointer() const noexcept
    {
#ifndef NDEBUG
        if (!CheckPointerAlignment())
            LOG_ERROR("Body data pointer alignment error");
#endif

        return reinterpret_cast<uint64_t>(this) |
            (static_cast<uint64_t>(activeUserPointerFlags) & STUFFED_POINTER_DATA_MASK);
    }

    inline bool CheckPointerAlignment() const noexcept
    {
        return (reinterpret_cast<uint64_t>(this) & STUFFED_POINTER_DATA_MASK) == 0;
    }

    // ------------------------------------ //
    // Collision callback user data (C# side provides this)

    [[nodiscard]] inline bool HasUserData() const noexcept
    {
        return userDataLength > 0;
    }

    [[nodiscard]] inline const std::array<char, PHYSICS_USER_DATA_SIZE>& GetUserData() const noexcept
    {
        return userData;
    }

    inline bool SetUserData(const char* data, int length) noexcept
    {
        static_assert(PHYSICS_USER_DATA_SIZE < std::numeric_limits<int>::max());

        // Fail if too much data given
        if (length > static_cast<int>(userData.size()))
        {
            LOG_ERROR("Physics body has too long user data");
            userDataLength = 0;
            return false;
        }

        // Data clearing
        if (data == nullptr)
        {
            userDataLength = 0;
            return true;
        }

        // New data is set
        std::memcpy(userData.data(), data, length);
        userDataLength = length;
        return true;
    }

    inline bool IsDetached() const noexcept
    {
        return detached;
    }

protected:
    bool EnableBodyControlIfNotAlready() noexcept;
    bool DisableBodyControl() noexcept;

    void MarkUsedInWorld(PhysicalWorld* containedInWorld) noexcept;
    void MarkRemovedFromWorld() noexcept;

    inline void MarkDetached() noexcept
    {
        detached = true;

        // Clear out any currently active collisions if any were recorded
        activeRecordedCollisionCount = 0;
    }

    inline bool IsInSpecificWorld(const PhysicalWorld* world) const noexcept
    {
        return containedInWorld == world;
    }

    void NotifyConstraintAdded(TrackedConstraint& constraint) noexcept;
    void NotifyConstraintRemoved(TrackedConstraint& constraint) noexcept;

    inline void NotifyActiveStatus(bool newActiveValue) noexcept
    {
        active = newActiveValue;
    }

#ifdef LOCK_FREE_COLLISION_RECORDING
    /// \brief Prepares a location to record a new collision on this body for this physics update
    /// \returns Pointer to write the data to, null if there was an overflow on the number of recorded collisions
    /// this frame and the data can't be recorded
    inline PhysicsCollision* GetNextCollisionRecordLocation(uint32_t stepIdentifier) noexcept
    {
        // TODO: could maybe trigger this only on the first recording of a collision
        HandleStepIdentifier(stepIdentifier);

        return GetNextRecordLocation();
    }

    inline PhysicsCollision* GetNextOrExistingCollisionRecordLocation(
        uint32_t stepIdentifier, PhysicsBody* otherBody, bool& usedExisting)
    {
        // TODO: could maybe trigger this only on the first recording of a collision
        HandleStepIdentifier(stepIdentifier);

        auto compareCount = activeRecordedCollisionCount.load(std::memory_order::acquire);

        if (compareCount > 0)
        {
            // Is this enough for the above TODO?
            std::atomic_thread_fence(std::memory_order::acq_rel);

            // Briefly the write may be out of range, so to guard against that we need this adjustment to what this
            // method may read
            if (compareCount > maxCollisionsToRecord) [[unlikely]]
                compareCount = maxCollisionsToRecord;

            for (int i = 0; i < compareCount; ++i)
            {
                auto* candidate = &collisionRecordingTarget[i];

                // Don't need to check first body as it is always us

                // This intends to compare the pointer data
#pragma clang diagnostic push
#pragma ide diagnostic ignored "bugprone-sizeof-expression"

#ifdef USE_ATOMIC_COLLISION_WRITE
                const std::atomic_ref<const PhysicsBody*> compareAtomic{candidate->SecondBody};
                if (compareAtomic.load(std::memory_order::acquire) == otherBody)
#else
                // Might be better to use a memory compare here as this might be better regarding the atomics
                // if (candidate->SecondBody == otherBody)
                if (std::memcmp(&candidate->SecondBody, &otherBody, sizeof(otherBody)) == 0)
#endif
                {
                    // Found a match
                    // TODO: is this needed (or does this not help either?)
                    // std::atomic_thread_fence(std::memory_order::acquire);
                    usedExisting = true;
                    return candidate;
                }

#pragma clang diagnostic pop
            }
        }

        usedExisting = false;
        return GetNextRecordLocation();
    }

private:
    inline void HandleStepIdentifier(uint32_t stepIdentifier) noexcept
    {
        auto originalStepValue = lastRecordedPhysicsStep.load(std::memory_order_acquire);
        if (stepIdentifier != originalStepValue)
        {
            // Atomic exchange here to ensure only one thread gets here
            if (lastRecordedPhysicsStep.compare_exchange_strong(
                    originalStepValue, stepIdentifier, std::memory_order_release, std::memory_order_relaxed))
            {
                // New step started, report that we have active collisions so that the world will clear our
                // activeRecordedCollisionCount before next physics step
                containedInWorld->ReportBodyWithActiveCollisions(*this);
            }
        }
    }

    inline PhysicsCollision* GetNextRecordLocation()
    {
        // Atomically acquire the array index to write to
        const auto indexToWriteTo = activeRecordedCollisionCount.fetch_add(1, std::memory_order::acq_rel);

        // Skip if too many collisions
        if (indexToWriteTo >= maxCollisionsToRecord) [[unlikely]]
        {
            // If we go over the number of allowed recorded collisions, we need to reset the memory back
            activeRecordedCollisionCount.fetch_sub(1, std::memory_order::acq_rel);

            // Previously this used atomic store on the variable, but that seemed to not work, though the bug was
            // probably in an if-condition instead, still it seems like this subtract approach always works

            return nullptr;
        }

#ifndef NDEBUG
        if (indexToWriteTo >= maxCollisionsToRecord) [[unlikely]]
            throw std::runtime_error("physics collision write index is too high");
#endif

        return &collisionRecordingTarget[indexToWriteTo];
    }

#else

    /// \brief Prepares a location to record a new collision on this body for this physics update
    /// \returns Pointer to write the data to, null if there was an overflow on the number of recorded collisions
    /// this frame and the data can't be recorded
    inline PhysicsCollision* GetNextCollisionRecordLocation(uint32_t stepIdentifier) noexcept
    {
        Lock lock(collisionRecordMutex);

        // TODO: could maybe trigger this only on the first recording of a collision
        HandleStepIdentifier(stepIdentifier);

        return GetNextRecordLocation();
    }

    inline PhysicsCollision* GetNextOrExistingCollisionRecordLocation(
        uint32_t stepIdentifier, PhysicsBody* otherBody, bool& usedExisting)
    {
        Lock lock(collisionRecordMutex);

        // TODO: could maybe trigger this only on the first recording of a collision
        HandleStepIdentifier(stepIdentifier);

        const auto compareCount = activeRecordedCollisionCount;

        for (int i = 0; i < compareCount; ++i)
        {
            auto* candidate = &collisionRecordingTarget[i];

            // Don't need to check first body as it is always us

            // Atomics can be used here as well becase even though there is a lock above, the lock is not held while
            // writing
            // This intends to compare the pointer data
#pragma clang diagnostic push
#pragma ide diagnostic ignored "bugprone-sizeof-expression"

#ifdef USE_ATOMIC_COLLISION_WRITE
            const std::atomic_ref<const PhysicsBody*> compareAtomic{candidate->SecondBody};
            if (compareAtomic.load(std::memory_order::acquire) == otherBody)
#else
            // Might be better to use a memory compare here as this might be better regarding the atomics
            // if (candidate->SecondBody == otherBody)
            if (std::memcmp(&candidate->SecondBody, &otherBody, sizeof(otherBody)) == 0)
#endif
            {
                // Found a match
                // TODO: is this needed (or does this not help either?)
                // std::atomic_thread_fence(std::memory_order::acquire);
                usedExisting = true;
                return candidate;
            }
        }

        usedExisting = false;
        return GetNextRecordLocation();
    }

private:
    inline void HandleStepIdentifier(uint32_t stepIdentifier) noexcept
    {
        if (stepIdentifier != lastRecordedPhysicsStep)
        {
            lastRecordedPhysicsStep = stepIdentifier;

            // New step started, report that we have active collisions so that the world will clear our
            // activeRecordedCollisionCount before next physics step
            containedInWorld->ReportBodyWithActiveCollisions(*this);
        }
    }

    inline PhysicsCollision* GetNextRecordLocation()
    {
        // Skip if too many collisions
        if (activeRecordedCollisionCount >= maxCollisionsToRecord) [[unlikely]]
            return nullptr;

        return &collisionRecordingTarget[activeRecordedCollisionCount++];
    }
#endif

protected:
    /// \brief Clears recorded collision data
    ///
    /// This is used by the PhysicalWorld to prepare collision recording for the next frame
    FORCE_INLINE void ClearRecordedData()
    {
        activeRecordedCollisionCount = 0;

        // TODO: could maybe switch the last step number to a simple bool flag to determine if we have registered our
        // selves already or not to be cleared of collisions on next update
    }

private:
    std::array<char, PHYSICS_USER_DATA_SIZE> userData;

    IgnoredCollisionList ignoredCollisions;

    /// This is memory not owned by us where recorded collisions are written to
    CollisionRecordListType collisionRecordingTarget = nullptr;

    std::vector<Ref<TrackedConstraint>> constraintsThisIsPartOf;

#ifndef LOCK_FREE_COLLISION_RECORDING
    Mutex collisionRecordMutex;
#endif

    const JPH::BodyID id;

    std::unique_ptr<BodyControlState> bodyControlStateIfActive;

    /// This is purely used to compare against world pointers to check that this is in a specific world. Do not call
    /// anything through this pointer as it is not guaranteed safe. The only exception is using this during a physics
    /// step in GetNextCollisionRecordLocation
    PhysicalWorld* containedInWorld = nullptr;

    CollisionFilterCallback callbackBasedFilter = nullptr;

    int userDataLength = 0;

    int maxCollisionsToRecord = 0;

#ifdef LOCK_FREE_COLLISION_RECORDING
    /// A pointer to this is passed out for users of the collision recording array
    std::atomic<int32_t> activeRecordedCollisionCount{0};

    /// Used to detect when a new batch of collisions begins and old ones should be cleared
    std::atomic<uint32_t> lastRecordedPhysicsStep{std::numeric_limits<uint32_t>::max()};
#else
    /// A pointer to this is passed out for users of the collision recording array
    int32_t activeRecordedCollisionCount = 0;

    /// Used to detect when a new batch of collisions begins and old ones should be cleared
    uint32_t lastRecordedPhysicsStep = -1;
#endif

    uint8_t activeUserPointerFlags = 0;

    bool detached = false;
    bool active = true;
    bool allCollisionsDisabled = false;
};

} // namespace Thrive::Physics
