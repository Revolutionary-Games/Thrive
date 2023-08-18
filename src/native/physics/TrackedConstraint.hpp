#pragma once

#include "Jolt/Core/Reference.h"

#include "core/Logger.hpp"

namespace JPH
{
class Constraint;
} // namespace JPH

namespace Thrive::Physics
{

/// \brief Tracks an existing constraint. This is needed as the physics engine doesn't track the existing constraints
/// itself
class TrackedConstraint : public RefCounted<TrackedConstraint>
{
    friend class PhysicalWorld;

public:
#ifdef USE_OBJECT_POOLS
    /// \brief Constraint between a single body and the world
    TrackedConstraint(
        const JPH::Ref<JPH::Constraint>& constraint, const Ref<PhysicsBody>& body1, ReleaseCallback deleteCallback);

    /// \brief Constraint between two bodies
    TrackedConstraint(const JPH::Ref<JPH::Constraint>& constraint, const Ref<PhysicsBody>& body1,
        const Ref<PhysicsBody>& body2, ReleaseCallback deleteCallback);
#else
    /// \brief Constraint between a single body and the world
    TrackedConstraint(const JPH::Ref<JPH::Constraint>& constraint, const Ref<PhysicsBody>& body1);

    /// \brief Constraint between two bodies
    TrackedConstraint(
        const JPH::Ref<JPH::Constraint>& constraint, const Ref<PhysicsBody>& body1, const Ref<PhysicsBody>& body2);
#endif

    ~TrackedConstraint() override;

    [[nodiscard]] bool IsCreatedInWorld() const noexcept
    {
        return createdInWorld != nullptr;
    }

    [[nodiscard]] bool IsAttachedToBodies() const noexcept
    {
        return attachedToBodies;
    }

    [[nodiscard]] const inline JPH::Ref<JPH::Constraint>& GetConstraint() const noexcept
    {
        return constraintInstance;
    }

protected:
    inline void OnRegisteredToWorld(PhysicalWorld& world)
    {
        createdInWorld = &world;
    }

    inline void OnRemoveFromWorld(PhysicalWorld& world)
    {
        if (createdInWorld != &world)
        {
            LOG_ERROR("Constraint tried to be removed from world it is not in");
            return;
        }

        createdInWorld = nullptr;
    }

    inline void OnDestroyByWorld(PhysicalWorld& world)
    {
        if (createdInWorld != &world)
        {
            LOG_ERROR("Constraint tried to be destroyed by world it is not in");
            return;
        }

        OnRemoveFromWorld(world);

        // Make sure destructor doesn't run while detaching
        AddRef();

        // To make this be released, tell both of the bodies that this is no longer wanted to exist to free up
        // references
        DetachFromBodies();

        // This should get deleted now
        Release();
    }

    // TODO: method to delete the constraint from the bodies

private:
    void DetachFromBodies();

private:
    const Ref<PhysicsBody> firstBody;
    const Ref<PhysicsBody> optionalSecondBody;
    const JPH::Ref<JPH::Constraint> constraintInstance;

    PhysicalWorld* createdInWorld = nullptr;

    bool attachedToBodies = true;
};

} // namespace Thrive::Physics
