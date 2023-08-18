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

    int32_t FirstSubShapeData;

    int32_t SecondSubShapeData;

    float PenetrationAmount;

    // Without packed attribute there are 4 bytes of extra padding here
};

static_assert(sizeof (PhysicsCollision) == PHYSICS_COLLISION_DATA_SIZE);

using CollisionRecordListType = PhysicsCollision*;

using CollisionFilterCallback = bool (*)(const PhysicsCollision& potentialCollision);

} // namespace Thrive::Physics
