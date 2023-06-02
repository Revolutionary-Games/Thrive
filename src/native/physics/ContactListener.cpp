// ------------------------------------ //
#include "ContactListener.hpp"

#include "Jolt/Physics/Body/Body.h"

// ------------------------------------ //
namespace Thrive::Physics
{

JPH::ValidateResult ContactListener::OnContactValidate(const JPH::Body& body1, const JPH::Body& body2,
    JPH::RVec3Arg baseOffset, const JPH::CollideShapeResult& collisionResult)
{
    JPH::ValidateResult result;
    if (chainedListener != nullptr)
    {
        result = chainedListener->OnContactValidate(body1, body2, baseOffset, collisionResult);
    }
    else
    {
        result = JPH::ContactListener::OnContactValidate(body1, body2, baseOffset, collisionResult);
    }

    return result;
}

void ContactListener::OnContactAdded(const JPH::Body& body1, const JPH::Body& body2,
    const JPH::ContactManifold& manifold, JPH::ContactSettings& settings)
{
    // Note the bodies are sorted (at least the sample Jolt code has asserts to verify it, so we can probably safely
    // always assume that) `body1.GetID() < body2.GetID()`

    // Add the new collision
    {
        Lock lock(currentCollisionsMutex);
        JPH::SubShapeIDPair key(body1.GetID(), manifold.mSubShapeID1, body2.GetID(), manifold.mSubShapeID2);
        currentCollisions[key] = CollisionPair(manifold.mBaseOffset, manifold.mRelativeContactPointsOn1);
    }

    if (chainedListener != nullptr)
        chainedListener->OnContactAdded(body1, body2, manifold, settings);
}

void ContactListener::OnContactPersisted(const JPH::Body& body1, const JPH::Body& body2,
    const JPH::ContactManifold& manifold, JPH::ContactSettings& settings)
{
    // Update existing collision info
    {
        Lock lock(currentCollisionsMutex);

        JPH::SubShapeIDPair key(body1.GetID(), manifold.mSubShapeID1, body2.GetID(), manifold.mSubShapeID2);

        const auto iter = currentCollisions.find(key);
        if (iter != currentCollisions.end())
        {
            iter->second = CollisionPair(manifold.mBaseOffset, manifold.mRelativeContactPointsOn1);
        }
    }

    if (chainedListener != nullptr)
        chainedListener->OnContactPersisted(body1, body2, manifold, settings);
}

void ContactListener::OnContactRemoved(const JPH::SubShapeIDPair& subShapePair)
{
    // Remove the contact
    {
        Lock lock(currentCollisionsMutex);

        const auto iter = currentCollisions.find(subShapePair);
        if (iter != currentCollisions.end())
            currentCollisions.erase(iter);
    }

    if (chainedListener != nullptr)
        chainedListener->OnContactRemoved(subShapePair);
}

} // namespace Thrive::Physics
