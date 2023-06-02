// ------------------------------------ //
#include "ShapeCreator.hpp"

#include "Jolt/Math/Trigonometry.h"
#include "Jolt/Physics/Collision/Shape/ConvexHullShape.h"
#include "Jolt/Physics/Collision/Shape/MeshShape.h"
#include "Jolt/Physics/Collision/Shape/MutableCompoundShape.h"
#include "Jolt/Physics/Collision/Shape/StaticCompoundShape.h"

// ------------------------------------ //
namespace Thrive::Physics
{

JPH::RefConst<JPH::Shape> ShapeCreator::CreateConvex(const JPH::Array<JPH::Vec3>& points,
    float convexRadius /*= 0.01f*/, const JPH::PhysicsMaterial* material /*= nullptr*/)
{
    return JPH::ConvexHullShapeSettings(points, convexRadius, material).Create().Get();
}

JPH::RefConst<JPH::Shape> ShapeCreator::CreateStaticCompound(
    const std::vector<std::tuple<JPH::RefConst<JPH::Shape>, JPH::Vec3, JPH::Quat, uint32_t>>& subShapes)
{
    JPH::StaticCompoundShapeSettings settings;

    for (const auto& shape : subShapes)
    {
        settings.AddShape(std::get<1>(shape), std::get<2>(shape), std::get<0>(shape), std::get<3>(shape));
    }

    return settings.Create().Get();
}

JPH::RefConst<JPH::Shape> ShapeCreator::CreateMutableCompound(
    const std::vector<std::tuple<JPH::RefConst<JPH::Shape>, JPH::Vec3, JPH::Quat, uint32_t>>& subShapes)
{
    JPH::MutableCompoundShapeSettings settings;

    for (const auto& shape : subShapes)
    {
        settings.AddShape(std::get<1>(shape), std::get<2>(shape), std::get<0>(shape), std::get<3>(shape));
    }

    return settings.Create().Get();
}

JPH::RefConst<JPH::Shape> ShapeCreator::CreateMesh(
    JPH::Array<JPH::Float3>&& vertices, JPH::Array<JPH::IndexedTriangle>&& triangles)
{
    // Create torus
    JPH::MeshShapeSettings mesh;

    // TODO: materials support (each triangle can have a different one)
    // mesh.mMaterials

    mesh.mTriangleVertices = std::move(vertices);
    mesh.mIndexedTriangles = std::move(triangles);

    return mesh.Create().Get();
}

} // namespace Thrive::Physics
