// ------------------------------------ //
#include "SimpleShapes.hpp"

#include "Jolt/Physics/Collision/Shape/BoxShape.h"
#include "Jolt/Physics/Collision/Shape/CapsuleShape.h"
#include "Jolt/Physics/Collision/Shape/CylinderShape.h"
#include "Jolt/Physics/Collision/Shape/ScaledShape.h"
#include "Jolt/Physics/Collision/Shape/Shape.h"
#include "Jolt/Physics/Collision/Shape/SphereShape.h"

// ------------------------------------ //
namespace Thrive::Physics
{

JPH::RefConst<JPH::Shape> SimpleShapes::CreateSphere(
    float radius, float density /*= 1000*/, const JPH::PhysicsMaterial* material /*= nullptr*/)
{
    auto shape = new JPH::SphereShape(radius, material);
    shape->SetDensity(density);

    return shape;
}

JPH::RefConst<JPH::Shape> SimpleShapes::CreateBox(
    float halfSideLength, float density /*= 1000*/, const JPH::PhysicsMaterial* material /*= nullptr*/)
{
    return CreateBox(JPH::Vec3(halfSideLength, halfSideLength, halfSideLength), density, material);
}

JPH::RefConst<JPH::Shape> SimpleShapes::CreateBox(
    JPH::Vec3 halfExtends, float density /*= 1000*/, const JPH::PhysicsMaterial* material /*= nullptr*/)
{
    auto shape = new JPH::BoxShape(halfExtends, JPH::cDefaultConvexRadius, material);
    shape->SetDensity(density);

    return shape;
}

JPH::RefConst<JPH::Shape> SimpleShapes::CreateCylinder(
    float halfHeight, float radius, float density /*= 1000*/, const JPH::PhysicsMaterial* material /*= nullptr*/)
{
    auto shape = new JPH::CylinderShape(halfHeight, radius, JPH::cDefaultConvexRadius, material);
    shape->SetDensity(density);

    return shape;
}

JPH::RefConst<JPH::Shape> SimpleShapes::CreateCapsule(
    float halfHeight, float radius, float density /*= 1000*/, const JPH::PhysicsMaterial* material /*= nullptr*/)
{
    auto shape = new JPH::CapsuleShape(halfHeight, radius, material);
    shape->SetDensity(density);

    return shape;
}

// ------------------------------------ //
JPH::RefConst<JPH::Shape> SimpleShapes::Scale(const JPH::RefConst<JPH::Shape>& shape, float scale)
{
    return new JPH::ScaledShape(shape, JPH::Vec3(scale, scale, scale));
}

} // namespace Thrive::Physics
