#pragma once

#include <array>
#include <cstdint>

#include "Include.h"

namespace Thrive::Physics
{
class PhysicsBody;

/// \brief Recorded physics cast ray hit. Contains user data from the hit body. Must match the memory layout of the
/// C# side PhysicsRayWithUserData class.
///
/// If the size in bytes is changed, PhysicsRayWithUserData in CStructures.h must also be updated (size defined in
/// Include.h.in)
struct PhysicsRayWithUserData
{
public:
    std::array<char, PHYSICS_USER_DATA_SIZE> BodyUserData;

    /// Pointer to the hit body's extra data object
    const PhysicsBody* Body;

    /// The hit sub shape
    uint32_t SubShapeData;

    /// How far along the cast ray this hit was as a fraction of the total length
    float HitFraction;

    // There's 4 bytes of extra padding here
};

static_assert(sizeof (PhysicsRayWithUserData) == PHYSICS_RAY_DATA_SIZE);


} // namespace Thrive::Physics
