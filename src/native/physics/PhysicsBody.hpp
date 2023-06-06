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

    [[nodiscard]] bool IsActive() const noexcept
    {
        return active;
    }

    [[nodiscard]] inline JPH::BodyID GetId() const
    {
        return id;
    }

protected:
    void MarkUsedInWorld() noexcept;
    void MarkRemovedFromWorld() noexcept;

    inline void NotifyActiveStatus(bool newActiveValue) noexcept
    {
        active = newActiveValue;
    }

private:
    const JPH::BodyID id;

    bool inWorld = false;
    bool active = true;

    // PhysicalWorld* owningWorld;
};

} // namespace Thrive::Physics
