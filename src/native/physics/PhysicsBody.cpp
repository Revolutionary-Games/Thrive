// ------------------------------------ //
#include "PhysicsBody.hpp"

#include "Jolt/Physics/Body/Body.h"

#include "core/Logger.hpp"

#include "BodyControlState.hpp"
#include "TrackedConstraint.hpp"

// ------------------------------------ //
namespace Thrive::Physics
{
#ifdef USE_OBJECT_POOLS
PhysicsBody::PhysicsBody(JPH::Body* body, JPH::BodyID bodyId, ReleaseCallback deleteCallback) noexcept :
    RefCounted<PhysicsBody>(deleteCallback),
#else
PhysicsBody::PhysicsBody(JPH::Body* body, JPH::BodyID bodyId) noexcept :
#endif
    id(bodyId)
{
    body->SetUserData(CalculateUserPointer());
}

PhysicsBody::~PhysicsBody() noexcept
{
    if (containedInWorld != nullptr)
        LOG_ERROR("PhysicsBody deleted while it is still in the world, this is going to cause memory corruption!");
}

// ------------------------------------ //
void PhysicsBody::SetCollisionRecordingTarget(CollisionRecordListType target, int maxCount) noexcept
{
    collisionRecordingTarget = target;
    maxCollisionsToRecord = maxCount;
    activeRecordedCollisionCount = 0;
}

void PhysicsBody::ClearCollisionRecordingTarget() noexcept
{
    collisionRecordingTarget = nullptr;
    maxCollisionsToRecord = 0;
    activeRecordedCollisionCount = 0;
}

// ------------------------------------ //
bool PhysicsBody::AddCollisionIgnore(const PhysicsBody& ignoredBody, bool skipDuplicates) noexcept
{
    const auto idToAdd = ignoredBody.GetId();

    if (skipDuplicates)
    {
        const auto end = ignoredCollisions.end();
        for (auto iter = ignoredCollisions.begin(); iter != end; ++iter)
        {
            if (*iter == idToAdd)
            {
                return false;
            }
        }
    }

    ignoredCollisions.emplace_back(idToAdd);
    return true;
}

bool PhysicsBody::RemoveCollisionIgnore(const PhysicsBody& noLongerIgnored) noexcept
{
    const auto idToRemove = noLongerIgnored.GetId();

    const auto end = ignoredCollisions.end();
    for (auto iter = ignoredCollisions.begin(); iter != end; ++iter)
    {
        if (*iter == idToRemove){
            ignoredCollisions.erase(iter);
            return true;
        }
    }

    return false;
}

void PhysicsBody::SetCollisionIgnores(PhysicsBody* const& ignoredBodies, int ignoreCount) noexcept
{
    ignoredCollisions.clear();

    for (int i = 0; i < ignoreCount; ++i)
    {
        ignoredCollisions.emplace_back(ignoredBodies[i].GetId());
    }
}

void PhysicsBody::SetSingleCollisionIgnore(const PhysicsBody& ignoredBody) noexcept
{
    ignoredCollisions.clear();
    ignoredCollisions.emplace_back(ignoredBody.GetId());
}

void PhysicsBody::ClearCollisionIgnores() noexcept
{
    ignoredCollisions.clear();
}

// ------------------------------------ //
bool PhysicsBody::EnableBodyControlIfNotAlready() noexcept
{
    // If already enabled
    if (bodyControlStateIfActive != nullptr)
        return false;

    bodyControlStateIfActive = std::make_unique<BodyControlState>();

    return true;
}

bool PhysicsBody::DisableBodyControl() noexcept
{
    if (bodyControlStateIfActive == nullptr)
        return false;

    bodyControlStateIfActive.reset();

    return true;
}

// ------------------------------------ //
void PhysicsBody::MarkUsedInWorld(PhysicalWorld* world) noexcept
{
    if (containedInWorld)
        LOG_ERROR("PhysicsBody marked used when already in use");

    containedInWorld = world;

    // Calling this method is the way that bodies become attached again (if detached previously)
    detached = false;
}

void PhysicsBody::MarkRemovedFromWorld() noexcept
{
    if (!containedInWorld)
        LOG_ERROR("PhysicsBody marked removed from world when it wasn't used in the first place");

    containedInWorld = nullptr;
}

void PhysicsBody::NotifyConstraintAdded(TrackedConstraint& constraint) noexcept
{
    constraintsThisIsPartOf.emplace_back(&constraint);

    // To save on performance this doesn't check on duplicate constraint adds
}

void PhysicsBody::NotifyConstraintRemoved(TrackedConstraint& constraint) noexcept
{
    for (auto iter = constraintsThisIsPartOf.rbegin(); iter != constraintsThisIsPartOf.rend(); ++iter)
    {
        if (iter->get() == &constraint)
        {
            constraintsThisIsPartOf.erase((iter + 1).base());
            return;
        }
    }

    LOG_ERROR("PhysicsBody notified of removed constraint that this wasn't a part of");
}

} // namespace Thrive::Physics
