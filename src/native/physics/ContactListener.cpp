// ------------------------------------ //
#include "ContactListener.hpp"

#include "Jolt/Physics/Body/Body.h"
#include "Jolt/Physics/Collision/CollideShape.h"

#include "DebugDrawForwarder.hpp"
#include "PhysicsBody.hpp"

// ------------------------------------ //
namespace Thrive::Physics
{

inline void PrepareBasicCollisionInfo(PhysicsCollision& collision, const PhysicsBody* body1, const PhysicsBody* body2)
{
    collision.FirstBody = body1;
    collision.SecondBody = body2;

    if (body1->HasUserData()) [[likely]]
    {
        collision.FirstUserData = body1->GetUserData();
    }
    else
    {
        // In case there is no user data (in Thrive use there should always be when used from entities) do a safety
        // thing and fill in zeros
        std::memset(collision.FirstUserData.data(), 0, collision.FirstUserData.size());
    }

    if (body2->HasUserData()) [[likely]]
    {
        collision.SecondUserData = body2->GetUserData();
    }
    else
    {
        std::memset(collision.SecondUserData.data(), 0, collision.SecondUserData.size());
    }
}

inline void ClearUnknownDataForCollisionFilter(PhysicsCollision& collision)
{
    collision.FirstSubShapeData = COLLISION_UNKNOWN_SUB_SHAPE;
    collision.SecondSubShapeData = COLLISION_UNKNOWN_SUB_SHAPE;
    collision.PenetrationAmount = -1;
}

inline void PrepareCollisionInfoFromManifold(PhysicsCollision& collision, const PhysicsBody* body1,
    const PhysicsBody* body2, const JPH::ContactManifold& manifold, bool justStarted)
{
    PrepareBasicCollisionInfo(collision, body1, body2);

    collision.FirstSubShapeData = manifold.mSubShapeID1.GetValue();
    collision.SecondSubShapeData = manifold.mSubShapeID2.GetValue();

    collision.PenetrationAmount = manifold.mPenetrationDepth;

    collision.JustStarted = justStarted;
}

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

    // Body-specific filtering. Likely is used here as the base method always allows contact, and we don't use chained
    // listeners
    if (result == JPH::ValidateResult::AcceptAllContactsForThisBodyPair || result == JPH::ValidateResult::AcceptContact)
        [[likely]]
    {
        // PhysicsCollision struct not initialized to only initialize it when required
#pragma clang diagnostic push
#pragma ide diagnostic ignored "cppcoreguidelines-pro-type-member-init"

        PhysicsCollision collisionData;

#pragma clang diagnostic pop

        bool collisionDataFilled = false;

        const auto userData1 = body1.GetUserData();
        const auto userData2 = body2.GetUserData();

        const bool body1UsesFilter = userData1 & PHYSICS_BODY_SPECIAL_COLLISION_FLAG;
        const bool body2UsesFilter = userData2 & PHYSICS_BODY_SPECIAL_COLLISION_FLAG;

        if (body1UsesFilter || body2UsesFilter)
        {
            // Some special collision handling has to occur
            bool disallow = false;

            const auto body1Object = PhysicsBody::FromJoltBody(userData1);
            const auto body2Object = PhysicsBody::FromJoltBody(userData2);

            // Check all collision disable first
            if (userData1 & PHYSICS_BODY_DISABLE_COLLISION_FLAG || userData2 & PHYSICS_BODY_DISABLE_COLLISION_FLAG)
            {
                disallow = true;
            }
            else if (body1UsesFilter)
            {
                // Filter based on custom filter callback if defined
                const auto filter1 = body1Object->GetCollisionFilter();

                if (filter1)
                {
                    // Prepare collision data for the callback
                    PrepareBasicCollisionInfo(collisionData, body1Object, body2Object);
                    ClearUnknownDataForCollisionFilter(collisionData);
                    collisionData.JustStarted = true;
                    collisionDataFilled = true;

                    disallow = !filter1(collisionData);
                }

                // And then based on ignore list
                if (!disallow)
                    disallow = body1Object->IsBodyIgnored(body2.GetID());
            }

            if (!disallow)
            {
                // If first body allows collision, check the second one (all disable for second body was already
                // checked above)

                if (body2UsesFilter)
                {
                    // Filter based on custom filter callback if defined
                    const auto filter2 = body2Object->GetCollisionFilter();

                    if (filter2)
                    {
                        // Prepare collision data for the callback if not already
                        if (!collisionDataFilled)
                        {
                            PrepareBasicCollisionInfo(collisionData, body2Object, body2Object);
                            ClearUnknownDataForCollisionFilter(collisionData);
                            collisionData.JustStarted = true;
                        }

                        disallow = !filter2(collisionData);
                    }

                    // And then based on ignore list
                    if (!disallow)
                        disallow = body2Object->IsBodyIgnored(body2.GetID());
                }
            }

            if (disallow)
                result = JPH::ValidateResult::RejectAllContactsForThisBodyPair;
        }
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
    // Note the bodies are sorted (`body1.GetID() < body2.GetID()`)

    // Add the new collision
    {
        Lock lock(currentCollisionsMutex);
        JPH::SubShapeIDPair key(body1.GetID(), manifold.mSubShapeID1, body2.GetID(), manifold.mSubShapeID2);
        currentCollisions[key] = CollisionPair(manifold.mBaseOffset, manifold.mRelativeContactPointsOn1);
    }

    if (chainedListener != nullptr)
        chainedListener->OnContactAdded(body1, body2, manifold, settings);

#pragma clang diagnostic push
#pragma ide diagnostic ignored "cppcoreguidelines-pro-type-member-init"

    // PhysicsCollision struct not initialized to only initialize it when required
    // TODO: could we make a further optimization here by asking the PhysicsBody to provide a pointer directly to where
    // to write the collision data? This could save one memory copy per collision
    PhysicsCollision collisionData;
    bool collisionDataFilled = false;

#pragma clang diagnostic pop

    // TODO: should relative velocities be stored somehow here? The Jolt documentation mentions that can be used to
    // determine how hard the collision is

    // Recording collisions (we record the start as only on the next update does the persisted connection trigger)
    const auto userData1 = body1.GetUserData();
    const auto userData2 = body2.GetUserData();

    if (userData1 & PHYSICS_BODY_RECORDING_FLAG)
    {
        const auto body1Object = PhysicsBody::FromJoltBody(userData1);

        PrepareCollisionInfoFromManifold(
            collisionData, body1Object, PhysicsBody::FromJoltBody(userData2), manifold, true);
        collisionDataFilled = true;

        body1Object->RecordCollision(collisionData, physicsStep);
    }

    if (userData2 & PHYSICS_BODY_RECORDING_FLAG)
    {
        const auto body2Object = PhysicsBody::FromJoltBody(userData2);

        if (!collisionDataFilled)
        {
            PrepareCollisionInfoFromManifold(
                collisionData, PhysicsBody::FromJoltBody(userData1), body2Object, manifold, true);
        }

        body2Object->RecordCollision(collisionData, physicsStep);
    }

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

#pragma clang diagnostic push
#pragma ide diagnostic ignored "cppcoreguidelines-pro-type-member-init"

    // PhysicsCollision struct not initialized to only initialize it when required
    PhysicsCollision collisionData;
    bool collisionDataFilled = false;

#pragma clang diagnostic pop

    // Contact recording
    const auto userData1 = body1.GetUserData();
    const auto userData2 = body2.GetUserData();

    if (userData1 & PHYSICS_BODY_RECORDING_FLAG)
    {
        const auto body1Object = PhysicsBody::FromJoltBody(userData1);

        PrepareCollisionInfoFromManifold(
            collisionData, body1Object, PhysicsBody::FromJoltBody(userData2), manifold, false);
        collisionDataFilled = true;

        body1Object->RecordCollision(collisionData, physicsStep);
    }

    if (userData2 & PHYSICS_BODY_RECORDING_FLAG)
    {
        const auto body2Object = PhysicsBody::FromJoltBody(userData2);

        if (!collisionDataFilled)
        {
            PrepareCollisionInfoFromManifold(
                collisionData, PhysicsBody::FromJoltBody(userData1), body2Object, manifold, false);
        }

        body2Object->RecordCollision(collisionData, physicsStep);
    }

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
