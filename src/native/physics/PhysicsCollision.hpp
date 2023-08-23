#pragma once

#include <array>
#include <cstdint>

#include "Include.h"

namespace Thrive::Physics
{

class PhysicsBody;

/// \brief Recorded physics collision. Must match the memory layout of the C# side PhysicsCollision class.
///
/// If the size in bytes is changed PhysicsCollision in CStructures.h must also be updated (size defined in
/// Include.h.in)
struct PhysicsCollision
{
public:
    std::array<char, PHYSICS_USER_DATA_SIZE> FirstUserData;

    std::array<char, PHYSICS_USER_DATA_SIZE> SecondUserData;

    const PhysicsBody* FirstBody;

    const PhysicsBody* SecondBody;

    // Sub shape data for detecting which specific parts of the objects collided. Note that in CollisionFilterCallback
    // these are unknown and are set to COLLISION_UNKNOWN_SUB_SHAPE
    uint32_t FirstSubShapeData;

    uint32_t SecondSubShapeData;

    /// How big the object overlap is (this is directly correlated to how hard the collision is)
    float PenetrationAmount;

    /// True in collision filter and on the first physics update this collision appeared
    bool JustStarted;

    // Without packed attribute there are 3 bytes of extra padding here
};

static_assert(sizeof (PhysicsCollision) == PHYSICS_COLLISION_DATA_SIZE);

using CollisionRecordListType = PhysicsCollision*;

/// Callback that returns false when a collision should not be allowed. Note that sub shapes and penetration amounts
/// are not calculated yet when this is called
using CollisionFilterCallback = bool (*)(const PhysicsCollision& potentialCollision);

} // namespace Thrive::Physics
