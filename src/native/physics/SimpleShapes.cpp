// ------------------------------------ //
#include "SimpleShapes.hpp"

#include "Jolt/Physics/Collision/Shape/BoxShape.h"
#include "Jolt/Physics/Collision/Shape/CapsuleShape.h"
#include "Jolt/Physics/Collision/Shape/CylinderShape.h"
#include "Jolt/Physics/Collision/Shape/ScaledShape.h"
#include "Jolt/Physics/Collision/Shape/Shape.h"
#include "Jolt/Physics/Collision/Shape/SphereShape.h"
// ------------------------------------ //
using namespace Thrive::Physics;

JPH::RefConst<JPH::Shape> SimpleShapes::CreateSphere(float radius, const JPH::PhysicsMaterial* material /*= nullptr*/)
{
    return new JPH::SphereShape(radius, material);
}

JPH::RefConst<JPH::Shape> SimpleShapes::CreateBox(
    float halfSideLength, const JPH::PhysicsMaterial* material /*= nullptr*/)
{
    return CreateBox(JPH::Vec3(halfSideLength, halfSideLength, halfSideLength), material);
}

JPH::RefConst<JPH::Shape> SimpleShapes::CreateBox(
    JPH::Vec3 halfExtends, const JPH::PhysicsMaterial* material /*= nullptr*/)
{
    return new JPH::BoxShape(halfExtends, JPH::cDefaultConvexRadius, material);
}

JPH::RefConst<JPH::Shape> SimpleShapes::CreateCylinder(
    float halfHeight, float radius, const JPH::PhysicsMaterial* material /*= nullptr*/)
{
    return new JPH::CylinderShape(halfHeight, radius, JPH::cDefaultConvexRadius, material);
}

JPH::RefConst<JPH::Shape> SimpleShapes::CreateCapsule(
    float halfHeight, float radius, const JPH::PhysicsMaterial* material /*= nullptr*/)
{
    return new JPH::CapsuleShape(halfHeight, radius, material);
}

// ------------------------------------ //
JPH::RefConst<JPH::Shape> SimpleShapes::Scale(const JPH::RefConst<JPH::Shape>& shape, float scale)
{
    return new JPH::ScaledShape(shape, JPH::Vec3(scale, scale, scale));
}
