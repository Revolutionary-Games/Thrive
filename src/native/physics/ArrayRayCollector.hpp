#pragma once

#include "Jolt/Physics/Body/BodyLock.h"
#include "Jolt/Physics/Body/BodyLockInterface.h"
#include "Jolt/Physics/Collision/CastResult.h"
#include "Jolt/Physics/Collision/Shape/Shape.h"

#include "PhysicsBody.hpp"
#include "PhysicsRayWithUserData.hpp"

namespace Thrive::Physics
{

/// \brief Helper class to collect ray hits from Jolt into PhysicsRayWithUserData array
class ArrayRayCollector final
    : public JPH::CollisionCollector<JPH::RayCastResult, JPH::CollisionCollectorTraitsCastRay>,
      public NonCopyable
{
public:
    ArrayRayCollector(
        PhysicsRayWithUserData dataReceiver[], int maxHits, const JPH::BodyLockInterface& bodyLockInterface) :
        bodyInterface(bodyLockInterface),
        hitStorage(dataReceiver), hitStorageSpaceLeft(maxHits)
    {
        if (hitStorage == nullptr)
        {
            using namespace JPH;

            JPH_ASSERT(hitStorage);

            // This is an error condition, but we don't really want to throw here so instead this just for safety clear
            // the maxHits
            hitStorageSpaceLeft = 0;
        }
    }

    void AddHit(const ResultType& inResult) final
    {
        // Safety check against this being called too many times after running out of space
        if (hitStorageSpaceLeft < 1)
        {
            ForceEarlyOut();
            return;
        }

        // Store this hit
        // We need to lock the body to read the user data in it

        JPH::BodyLockRead lock(bodyInterface, inResult.mBodyID);
        if (!lock.Succeeded()) [[unlikely]]
        {
            // Can't read body
            return;
        }

        const JPH::Body& body = lock.GetBody();

        const auto* bodyWrapper = PhysicsBody::FromJoltBody(body.GetUserData());
        hitStorage->Body = bodyWrapper;

        if (bodyWrapper != nullptr)
        {
            std::memcpy(
                hitStorage->BodyUserData.data(), bodyWrapper->GetUserData().data(), hitStorage->BodyUserData.size());
        }
        else
        {
            std::memset(hitStorage->BodyUserData.data(), 0, hitStorage->BodyUserData.size());
        }

        hitStorage->HitFraction = inResult.mFraction;

        hitStorage->SubShapeData = inResult.mSubShapeID2.GetValue();

        // Increment the place in the given array we are writing to
        ++hitCount;
        ++hitStorage;
        --hitStorageSpaceLeft;

        // Stop collecting hits once there's no longer space
        if (hitStorageSpaceLeft < 0)
        {
            ForceEarlyOut();
        }
    }

    [[nodiscard]] inline int GetHitCount() const noexcept
    {
        return hitCount;
    }

private:
    const JPH::BodyLockInterface& bodyInterface;

    PhysicsRayWithUserData* hitStorage;
    int hitStorageSpaceLeft;
    int hitCount = 0;
};

} // namespace Thrive::Physics
