#pragma once

#include "Jolt/Core/Reference.h"
#include "Jolt/Physics/Body/BodyID.h"

#include "core/ForwardDefinitions.hpp"

namespace JPH
{
class BodyID;
class Body;
class Shape;
} // namespace JPH

namespace Thrive::Physics
{

class PhysicalWorld;

/// \brief Our physics body wrapper that has extra data
class PhysicsBody : public RefCounted
{
    friend PhysicalWorld;
    friend BodyActivationListener;
    friend TrackedConstraint;

protected:
    PhysicsBody(JPH::Body* body, JPH::BodyID bodyId) noexcept;

public:
    ~PhysicsBody() noexcept;

    PhysicsBody(const PhysicsBody& other) = delete;
    PhysicsBody(PhysicsBody&& other) = delete;

    PhysicsBody& operator=(const PhysicsBody& other) = delete;
    PhysicsBody& operator=(PhysicsBody&& other) = delete;

    /// \brief Retrieves an instance of this class from a physics body user data
    [[nodiscard]] static PhysicsBody* FromJoltBody(const JPH::Body* body) noexcept;

    [[nodiscard]] static PhysicsBody* FromJoltBody(uint64_t bodyUserData) noexcept;

    [[nodiscard]] inline bool IsActive() const noexcept
    {
        return active;
    }

    [[nodiscard]] inline bool IsInWorld() const noexcept
    {
        return inWorld;
    }

    [[nodiscard]] inline JPH::BodyID GetId() const
    {
        return id;
    }

    [[nodiscard]] inline const auto& GetConstraints() const noexcept{
        return constraintsThisIsPartOf;
    }

protected:
    void MarkUsedInWorld() noexcept;
    void MarkRemovedFromWorld() noexcept;

    void NotifyConstraintAdded(TrackedConstraint& constraint) noexcept;
    void NotifyConstraintRemoved(TrackedConstraint& constraint) noexcept;

    inline void NotifyActiveStatus(bool newActiveValue) noexcept
    {
        active = newActiveValue;
    }

private:
    const JPH::BodyID id;

    bool inWorld = false;
    bool active = true;

    std::vector<Ref<TrackedConstraint>> constraintsThisIsPartOf;

    // PhysicalWorld* owningWorld;
};

} // namespace Thrive::Physics
