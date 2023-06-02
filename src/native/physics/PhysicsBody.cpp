// ------------------------------------ //
#include "PhysicsBody.hpp"

#include "Jolt/Physics/Body/Body.h"

#include "core/Logger.hpp"

// ------------------------------------ //
namespace Thrive::Physics
{

PhysicsBody::PhysicsBody(JPH::Body* body, JPH::BodyID bodyId) : id(bodyId)
{
    body->SetUserData(reinterpret_cast<uint64_t>(this));
}

PhysicsBody::~PhysicsBody()
{
    if (inWorld)
        LOG_ERROR("PhysicsBody deleted while it is still in the world, this is going to cause memory corruption!");
}

// ------------------------------------ //
PhysicsBody* PhysicsBody::FromJoltBody(const JPH::Body* body)
{
    const auto rawValue = body->GetUserData();

    if (rawValue == 0)
        return nullptr;

    return reinterpret_cast<PhysicsBody*>(rawValue);
}

// ------------------------------------ //
void PhysicsBody::MarkUsedInWorld()
{
    if (inWorld)
        LOG_ERROR("PhysicsBody marked used when already in use");

    inWorld = true;
}

void PhysicsBody::MarkRemovedFromWorld()
{
    if (!inWorld)
        LOG_ERROR("PhysicsBody marked removed from world when it wasn't used in the first place");

    inWorld = false;
}

} // namespace Thrive::Physics
