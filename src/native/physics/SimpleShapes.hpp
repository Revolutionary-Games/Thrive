#pragma once

#include "Jolt/Core/Reference.h"

#include "Include.h"

namespace JPH
{
class Shape;
class PhysicsMaterial;
} // namespace JPH

namespace Thrive::Physics
{

/// \brief Helpers for creating simple shapes
class SimpleShapes
{
public:
    SimpleShapes() = delete;

    static JPH::RefConst<JPH::Shape> CreateSphere(float radius, float density = 1000, const JPH::PhysicsMaterial* material = nullptr);

    static JPH::RefConst<JPH::Shape> CreateBox(float halfSideLength, float density = 1000, const JPH::PhysicsMaterial* material = nullptr);
    static JPH::RefConst<JPH::Shape> CreateBox(JPH::Vec3 halfExtends, float density = 1000, const JPH::PhysicsMaterial* material = nullptr);

    static JPH::RefConst<JPH::Shape> CreateCylinder(
        float halfHeight, float radius, float density = 1000, const JPH::PhysicsMaterial* material = nullptr);

    static JPH::RefConst<JPH::Shape> CreateCapsule(
        float halfHeight, float radius, float density = 1000, const JPH::PhysicsMaterial* material = nullptr);

    static JPH::RefConst<JPH::Shape> Scale(const JPH::RefConst<JPH::Shape>& shape, float scale);
};

} // namespace Thrive::Physics
