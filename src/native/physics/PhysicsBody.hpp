#pragma once

#include "Jolt/Core/Reference.h"
#include "Jolt/Physics/Body/BodyID.h"

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

protected:
    PhysicsBody(JPH::Body* body, JPH::BodyID bodyId);

public:
    ~PhysicsBody();

    PhysicsBody(const PhysicsBody& other) = delete;
    PhysicsBody(PhysicsBody&& other) = delete;

    PhysicsBody& operator=(const PhysicsBody& other) = delete;
    PhysicsBody& operator=(PhysicsBody&& other) = delete;

    /// \brief Retrieves an instance of this class from a physics body user data
    static inline PhysicsBody* FromJoltBody(const JPH::Body* body);

    inline JPH::BodyID GetId() const
    {
        return id;
    }

protected:
    void MarkUsedInWorld();
    void MarkRemovedFromWorld();

private:
    const JPH::BodyID id;

    bool inWorld = false;

    // PhysicalWorld* owningWorld;
};

} // namespace Thrive::Physics
