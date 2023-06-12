// ------------------------------------ //
#include "ContactListener.hpp"

#include "Jolt/Physics/Body/Body.h"
#include "Jolt/Physics/Collision/CollideShape.h"

#include "DebugDrawForwarder.hpp"

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

#ifdef JPH_DEBUG_RENDERER
    if (debugDrawer != nullptr)
    {
        const auto contact_point = baseOffset + collisionResult.mContactPointOn1;

        if (result != JPH::ValidateResult::RejectContact &&
            result != JPH::ValidateResult::RejectAllContactsForThisBodyPair)
        {
            debugDrawer->DrawArrow(contact_point,
                contact_point - collisionResult.mPenetrationAxis.NormalizedOr(JPH::Vec3::sZero()), JPH::Color::sBlue,
                0.05f);
        }
        else
        {
            debugDrawer->DrawArrow(contact_point,
                contact_point - collisionResult.mPenetrationAxis.NormalizedOr(JPH::Vec3::sZero()), JPH::Color::sDarkRed,
                0.05f);
        }
    }
#endif

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

#ifdef JPH_DEBUG_RENDERER
    if (debugDrawer != nullptr)
    {
        debugDrawer->DrawWirePolygon(JPH::RMat44::sTranslation(manifold.mBaseOffset),
            manifold.mRelativeContactPointsOn1, JPH::Color::sGreen, 0.05f);
        debugDrawer->DrawWirePolygon(JPH::RMat44::sTranslation(manifold.mBaseOffset),
            manifold.mRelativeContactPointsOn2, JPH::Color::sGreen, 0.05f);
        debugDrawer->DrawArrow(manifold.GetWorldSpaceContactPointOn1(0),
            manifold.GetWorldSpaceContactPointOn1(0) + manifold.mWorldSpaceNormal, JPH::Color::sGreen, 0.05f);
    }
#endif
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

#ifdef JPH_DEBUG_RENDERER
    if (!drawOnlyNew && debugDrawer != nullptr)
    {
        debugDrawer->DrawWirePolygon(JPH::RMat44::sTranslation(manifold.mBaseOffset),
            manifold.mRelativeContactPointsOn1, JPH::Color::sYellow, 0.05f);
        debugDrawer->DrawWirePolygon(JPH::RMat44::sTranslation(manifold.mBaseOffset),
            manifold.mRelativeContactPointsOn2, JPH::Color::sYellow, 0.05f);
        debugDrawer->DrawArrow(manifold.GetWorldSpaceContactPointOn1(0),
            manifold.GetWorldSpaceContactPointOn1(0) + manifold.mWorldSpaceNormal, JPH::Color::sYellow, 0.05f);
    }
#endif
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

// ------------------------------------ //
#ifdef JPH_DEBUG_RENDERER
void ContactListener::DrawActiveContacts(JPH::DebugRenderer& debugRenderer)
{
    Lock lock(currentCollisionsMutex);
    for (const auto& collision : currentCollisions)
    {
        for (const auto offset : collision.second.second)
        {
            debugRenderer.DrawWireSphere(collision.second.first + offset, 0.05f, JPH::Color::sRed, 1);
        }
    }
}
#endif
} // namespace Thrive::Physics
