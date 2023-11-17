// ------------------------------------ //
#include "ContactListener.hpp"

#include "Jolt/Physics/Body/Body.h"
#include "Jolt/Physics/Collision/CollideShape.h"
#include "Jolt/Physics/Collision/Shape/CompoundShape.h"
#include "Jolt/Physics/Collision/Shape/SubShapeID.h"

#include "DebugDrawForwarder.hpp"
#include "PhysicsBody.hpp"

// ------------------------------------ //
namespace Thrive::Physics
{
ContactListener::ContactListener()
{
    if (COLLISION_UNKNOWN_SUB_SHAPE != JPH::SubShapeID().GetValue())
    {
        LOG_ERROR("Incorrectly configured unknown collision value compared to what Jolt has");
        abort();
    }
}

// ------------------------------------ //
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

#ifdef AUTO_RESOLVE_FIRST_LEVEL_SHAPE_INDEX
inline void PrepareCollisionInfoFromManifold(PhysicsCollision& collision, const PhysicsBody* body1,
    const JPH::Body& joltBody1, const PhysicsBody* body2, const JPH::Body& joltBody2,
    const JPH::ContactManifold& manifold, bool justStarted, bool swapOrder)
#else
inline void PrepareCollisionInfoFromManifold(PhysicsCollision& collision, const PhysicsBody* body1,
    const PhysicsBody* body2, const JPH::ContactManifold& manifold, bool justStarted, bool swapOrder)
#endif

{
    if (swapOrder)
    {
        PrepareBasicCollisionInfo(collision, body2, body1);
    }
    else
    {
        PrepareBasicCollisionInfo(collision, body1, body2);
    }

#ifdef AUTO_RESOLVE_FIRST_LEVEL_SHAPE_INDEX
    if (swapOrder)
    {
        collision.SecondSubShapeData = ResolveTopLevelSubShapeId(&joltBody1, manifold.mSubShapeID1);
        collision.FirstSubShapeData = ResolveTopLevelSubShapeId(&joltBody2, manifold.mSubShapeID2);
    }
    else
    {
        collision.FirstSubShapeData = ResolveTopLevelSubShapeId(&joltBody1, manifold.mSubShapeID1);
        collision.SecondSubShapeData = ResolveTopLevelSubShapeId(&joltBody2, manifold.mSubShapeID2);
    }
#else
    if (swapOrder)
    {
        collision.SecondSubShapeData = manifold.mSubShapeID1.GetValue();
        collision.FirstSubShapeData = manifold.mSubShapeID2.GetValue();
    }
    else
    {
        collision.FirstSubShapeData = manifold.mSubShapeID1.GetValue();
        collision.SecondSubShapeData = manifold.mSubShapeID2.GetValue();
    }
#endif

    collision.PenetrationAmount = manifold.mPenetrationDepth;

    collision.JustStarted = justStarted;
}

JPH::ValidateResult ContactListener::OnContactValidate(const JPH::Body& body1, const JPH::Body& body2,
    JPH::RVec3Arg baseOffset, const JPH::CollideShapeResult& collisionResult)
{
    JPH::ValidateResult result = JPH::ContactListener::OnContactValidate(body1, body2, baseOffset, collisionResult);

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
                        // The filter always has the current object as the first body so this data needs to be always
                        // written
                        PrepareBasicCollisionInfo(collisionData, body2Object, body1Object);

                        // Prepare the common collision data for the callback if not already
                        if (!collisionDataFilled)
                        {
                            ClearUnknownDataForCollisionFilter(collisionData);
                            collisionData.JustStarted = true;
                        }

                        disallow = !filter2(collisionData);
                    }

                    // And then based on ignore list
                    if (!disallow)
                        disallow = body2Object->IsBodyIgnored(body1.GetID());
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
    UNUSED(settings);

#ifdef JPH_DEBUG_RENDERER
    // Add the new collision
    {
        Lock lock(currentCollisionsMutex);
        JPH::SubShapeIDPair key(body1.GetID(), manifold.mSubShapeID1, body2.GetID(), manifold.mSubShapeID2);
        currentCollisions[key] = CollisionPair(manifold.mBaseOffset, manifold.mRelativeContactPointsOn1);
    }
#endif

    // TODO: should relative velocities be stored somehow here? The Jolt documentation mentions that can be used to
    // determine how hard the collision is

    // Recording collisions (we record the start as only on the next update does the persisted connection trigger,
    // and well there are some potential gameplay uses for the initial collision flag)
    const auto userData1 = body1.GetUserData();
    const auto userData2 = body2.GetUserData();

    if (userData1 & PHYSICS_BODY_RECORDING_FLAG)
    {
        const auto body1Object = PhysicsBody::FromJoltBody(userData1);

        // Get target location to directly write the collision info to, this saves one memory copy per recorded
        // collision
        auto* writeTarget = body1Object->GetNextCollisionRecordLocation(physicsStep);

        // Likely is used here as we are optimistic the collision counts are in control in terms of how many recording
        // slots there are
        if (writeTarget) [[likely]]
        {
#ifdef AUTO_RESOLVE_FIRST_LEVEL_SHAPE_INDEX
            PrepareCollisionInfoFromManifold(
                *writeTarget, body1Object, body1, PhysicsBody::FromJoltBody(userData2), body2, manifold, true, false);
#else
            PrepareCollisionInfoFromManifold(
                *writeTarget, body1Object, PhysicsBody::FromJoltBody(userData2), manifold, true, false);
#endif
        }
    }

    if (userData2 & PHYSICS_BODY_RECORDING_FLAG)
    {
        const auto body2Object = PhysicsBody::FromJoltBody(userData2);

        auto* writeTarget = body2Object->GetNextCollisionRecordLocation(physicsStep);

        if (writeTarget) [[likely]]
        {
#ifdef AUTO_RESOLVE_FIRST_LEVEL_SHAPE_INDEX
            PrepareCollisionInfoFromManifold(
                *writeTarget, PhysicsBody::FromJoltBody(userData1), body1, body2Object, body2, manifold, true, true);
#else
            PrepareCollisionInfoFromManifold(
                *writeTarget, PhysicsBody::FromJoltBody(userData1), body2Object, manifold, true, true);
#endif
        }
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
    UNUSED(settings);

#ifdef JPH_DEBUG_RENDERER
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
#endif

    // Contact recording
    const auto userData1 = body1.GetUserData();
    const auto userData2 = body2.GetUserData();

    if (userData1 & PHYSICS_BODY_RECORDING_FLAG)
    {
        const auto body1Object = PhysicsBody::FromJoltBody(userData1);

        // TODO: if we ever move to an approach where multiple physics steps can happen and collisions need to be
        // recorded persistently over a few physics updates we might need to somehow implement filtering here
        auto* writeTarget = body1Object->GetNextCollisionRecordLocation(physicsStep);

        if (writeTarget) [[likely]]
        {
#ifdef AUTO_RESOLVE_FIRST_LEVEL_SHAPE_INDEX
            PrepareCollisionInfoFromManifold(
                *writeTarget, body1Object, body1, PhysicsBody::FromJoltBody(userData2), body2, manifold, false, false);
#else

            PrepareCollisionInfoFromManifold(
                *writeTarget, body1Object, PhysicsBody::FromJoltBody(userData2), manifold, false, false);
#endif
        }
    }

    if (userData2 & PHYSICS_BODY_RECORDING_FLAG)
    {
        const auto body2Object = PhysicsBody::FromJoltBody(userData2);

        auto* writeTarget = body2Object->GetNextCollisionRecordLocation(physicsStep);

        if (writeTarget) [[likely]]
        {
#ifdef AUTO_RESOLVE_FIRST_LEVEL_SHAPE_INDEX
            PrepareCollisionInfoFromManifold(
                *writeTarget, PhysicsBody::FromJoltBody(userData1), body1, body2Object, body2, manifold, false, true);
#else
            PrepareCollisionInfoFromManifold(
                *writeTarget, PhysicsBody::FromJoltBody(userData1), body2Object, manifold, false, true);
#endif
        }
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
#ifdef JPH_DEBUG_RENDERER
    // Remove the contact
    {
        Lock lock(currentCollisionsMutex);

        const auto iter = currentCollisions.find(subShapePair);
        if (iter != currentCollisions.end())
            currentCollisions.erase(iter);
    }
#else
    UNUSED(subShapePair);
#endif
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

// ------------------------------------ //
uint ResolveTopLevelSubShapeId(const JPH::Body* body, JPH::SubShapeID subShapeId)
{
    JPH::SubShapeID unusedRemainder;
    return ResolveSubShapeId(body->GetShape(), subShapeId, unusedRemainder);
}

uint ResolveSubShapeId(const JPH::Shape* shape, JPH::SubShapeID subShapeId, JPH::SubShapeID& remainder)
{
    switch (shape->GetType())
    {
        case JPH::EShapeType::Compound:
            return static_cast<const JPH::CompoundShape*>(shape)->GetSubShapeIndexFromID(subShapeId, remainder);
        // Could add the following in the future
        /*case JPH::EShapeType::Decorated:
            break;
        case JPH::EShapeType::Mesh:
            break;
        case JPH::EShapeType::HeightField:
            break;
        case JPH::EShapeType::SoftBody:
            break;*/
        default:
            // Type that doesn't have sub-shapes
            return 0;
    }
}

} // namespace Thrive::Physics
