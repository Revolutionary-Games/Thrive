// ------------------------------------ //
#include "ShapeCreator.hpp"

#include "Jolt/Math/Trigonometry.h"
#include "Jolt/Physics/Collision/Shape/ConvexHullShape.h"
#include "Jolt/Physics/Collision/Shape/MeshShape.h"
#include "Jolt/Physics/Collision/Shape/MutableCompoundShape.h"
#include "Jolt/Physics/Collision/Shape/StaticCompoundShape.h"

#include "core/Logger.hpp"
#include "interop/JoltTypeConversions.hpp"

#include "ShapeWrapper.hpp"

// ------------------------------------ //
namespace Thrive::Physics
{

JPH::RefConst<JPH::Shape> ShapeCreator::CreateConvex(const JPH::Array<JPH::Vec3>& points, float density /*= 1000*/,
    float convexRadius /*= 0.01f*/, const JPH::PhysicsMaterial* material /*= nullptr*/)
{
    auto settings = JPH::ConvexHullShapeSettings(points, convexRadius, material);
    settings.SetDensity(density);

    return settings.Create().Get();
}

JPH::RefConst<JPH::Shape> ShapeCreator::CreateConvex(const JVecF3* points, size_t pointCount, float density,
    float scale, float convexRadius, const JPH::PhysicsMaterial* material)
{
    // We need to convert the data, so we pass empty data for the points here
    auto settings = JPH::ConvexHullShapeSettings(nullptr, 0, convexRadius, material);

    auto& pointTarget = settings.mPoints;

    pointTarget.reserve(pointCount);

    if (scale == 1)
    {
        for (size_t i = 0; i < pointCount; ++i)
        {
            const auto& point = points[i];

            pointTarget.emplace_back(point.X, point.Y, point.Z);
        }
    }
    else
    {
        for (size_t i = 0; i < pointCount; ++i)
        {
            const auto& point = points[i];

            pointTarget.emplace_back(point.X * scale, point.Y * scale, point.Z * scale);
        }
    }

    settings.SetDensity(density);

    return settings.Create().Get();
}

JPH::RefConst<JPH::Shape> ShapeCreator::CreateStaticCompound(
    const std::vector<std::tuple<JPH::RefConst<JPH::Shape>, JPH::Vec3, JPH::Quat, uint32_t>>& subShapes)
{
    JPH::StaticCompoundShapeSettings settings;

    settings.mSubShapes.reserve(subShapes.size());

    for (const auto& shape : subShapes)
    {
        settings.AddShape(std::get<1>(shape), std::get<2>(shape), std::get<0>(shape), std::get<3>(shape));
    }

    return settings.Create().Get();
}

JPH::RefConst<JPH::Shape> ShapeCreator::CreateStaticCompound(SubShapeDefinition* subShapes, size_t count)
{
    JPH::StaticCompoundShapeSettings settings;

    settings.mSubShapes.reserve(count);

    for (size_t i = 0; i < count; ++i)
    {
        const auto& shape = subShapes[i];

        JPH_ASSERT(shape.Shape);

        settings.AddShape(
            Vec3FromCAPI(shape.Position), QuatFromCAPI(shape.Rotation), shape.Shape->GetShape(), shape.UserData);
    }

    return settings.Create().Get();
}

JPH::RefConst<JPH::Shape> ShapeCreator::CreateMutableCompound(
    const std::vector<std::tuple<JPH::RefConst<JPH::Shape>, JPH::Vec3, JPH::Quat, uint32_t>>& subShapes)
{
    JPH::MutableCompoundShapeSettings settings;

    settings.mSubShapes.reserve(subShapes.size());

    for (const auto& shape : subShapes)
    {
        settings.AddShape(std::get<1>(shape), std::get<2>(shape), std::get<0>(shape), std::get<3>(shape));
    }

    return settings.Create().Get();
}

JPH::RefConst<JPH::Shape> ShapeCreator::CreateMutableCompound(SubShapeDefinition* subShapes, size_t count)
{
    JPH::MutableCompoundShapeSettings settings;

    settings.mSubShapes.reserve(count);

    for (size_t i = 0; i < count; ++i)
    {
        const auto& shape = subShapes[i];

        JPH_ASSERT(shape.Shape);

        settings.AddShape(
            Vec3FromCAPI(shape.Position), QuatFromCAPI(shape.Rotation), shape.Shape->GetShape(), shape.UserData);
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

// ------------------------------------ //
JPH::RefConst<JPH::Shape> ShapeCreator::CreateMicrobeShapeConvex(JVecF3* points, uint32_t pointCount, float density,
    float scale, float thickness, const JPH::PhysicsMaterial* material /*= nullptr*/)
{
    if (pointCount < 1)
    {
        LOG_ERROR("Microbe shape point count is 0");
        return nullptr;
    }

    const auto halfThickness = thickness * 0.5f;

    // We don't use any of the explicit constructors as we want to do any needed type and scale conversions when
    // actually copying data to the array in the settings
    auto settings = JPH::ConvexHullShapeSettings();
    settings.mMaxConvexRadius = JPH::cDefaultConvexRadius;

    auto& pointTarget = settings.mPoints;
    pointTarget.reserve(pointCount * 2);

    if (scale != 1)
    {
        for (uint32_t i = 0; i < pointCount; ++i)
        {
            const auto& sourcePoint = points[i];

            const auto scaledX = sourcePoint.X * scale;
            const auto scaledY = sourcePoint.Y * scale;
            const auto scaledZ = sourcePoint.Z * scale;

            pointTarget.emplace_back(scaledX, scaledY - halfThickness, scaledZ);
            pointTarget.emplace_back(scaledX, scaledY + halfThickness, scaledZ);
        }
    }
    else
    {
        for (uint32_t i = 0; i < pointCount; ++i)
        {
            const auto& sourcePoint = points[i];

            pointTarget.emplace_back(sourcePoint.X, sourcePoint.Y - halfThickness, sourcePoint.Z);
            pointTarget.emplace_back(sourcePoint.X, sourcePoint.Y + halfThickness, sourcePoint.Z);
        }
    }

    if (material != nullptr)
        settings.mMaterial = material;

    settings.SetDensity(density);

    return settings.Create().Get();
}

JPH::RefConst<JPH::Shape> ShapeCreator::CreateMicrobeShapeSpheres(
    JVecF3* points, uint32_t pointCount, float density, float scale, const JPH::PhysicsMaterial* material /*= nullptr*/)
{
    if (pointCount < 1)
    {
        LOG_ERROR("Microbe shape point count is 0");
        return nullptr;
    }

    const auto sphereShape = SimpleShapes::CreateSphere(1 * scale, density, material);

    JPH::StaticCompoundShapeSettings settings;

    const auto rotation = JPH::Quat::sIdentity();

    for (uint32_t i = 0; i < pointCount; ++i)
    {
        const auto& sourcePoint = points[i];

        settings.AddShape(
            {sourcePoint.X * scale, sourcePoint.Y * scale, sourcePoint.Z * scale}, rotation, sphereShape.GetPtr(), 0);
    }

    // Individual materials and densities are set in the sub shapes, hopefully that is enough

    return settings.Create().Get();
}

} // namespace Thrive::Physics
