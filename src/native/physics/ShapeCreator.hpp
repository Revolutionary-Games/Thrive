#pragma once

#include "interop/CStructures.h"

#include "SimpleShapes.hpp"

namespace JPH
{
class IndexedTriangle;
} // namespace JPH

namespace Thrive::Physics
{

class ShapeWrapper;

BEGIN_PACKED_STRUCT;

struct PACKED_STRUCT SubShapeDefinition
{
    JQuat Rotation;
    JVecF3 Position;
    uint32_t UserData;
    ShapeWrapper* Shape;
};

END_PACKED_STRUCT;

/// \brief More advanced shape creation helper class than SimpleShapes
class ShapeCreator
{
public:
    ShapeCreator() = delete;

    /// \brief Create a convex shape from a list of points
    /// \param convexRadius Used convex radius for this shape, should be lower than the default value used for other
    /// shapes
    static JPH::RefConst<JPH::Shape> CreateConvex(const JPH::Array<JPH::Vec3>& points, float density = 1000,
        float convexRadius = 0.01f, const JPH::PhysicsMaterial* material = nullptr);

    /// \brief Variant for avoiding extra data copy from C-API
    static JPH::RefConst<JPH::Shape> CreateConvex(const JVecF3* points, size_t pointCount, float density = 1000,
        float scale = 1, float convexRadius = 0.01f, const JPH::PhysicsMaterial* material = nullptr);

    /// \brief Creates a shape composed of multiple other shapes that cannot change after creation
    /// \todo Figure out how to use physics materials here
    static JPH::RefConst<JPH::Shape> CreateStaticCompound(
        const std::vector<std::tuple<JPH::RefConst<JPH::Shape>, JPH::Vec3, JPH::Quat, uint32_t>>& subShapes);
    static JPH::RefConst<JPH::Shape> CreateStaticCompound(SubShapeDefinition* subShapes, size_t count);

    /// \brief Variant of the compound shape that is allowed to be modified (but has lower performance than static)
    static JPH::RefConst<JPH::Shape> CreateMutableCompound(
        const std::vector<std::tuple<JPH::RefConst<JPH::Shape>, JPH::Vec3, JPH::Quat, uint32_t>>& subShapes);
    static JPH::RefConst<JPH::Shape> CreateMutableCompound(SubShapeDefinition* subShapes, size_t count);

    /// \brief Creates a mesh collision (note that the performance is worse and this can't collide with everything even
    /// when movable)
    ///
    /// This doesn't support setting a density and the Jolt documentation says that two moving meshes can't collide
    /// with each other, so this is likely only usable on static or kinematic bodies
    /// \todo Materials support, each triangle can have its own so this is a bit complicated to setup
    static JPH::RefConst<JPH::Shape> CreateMesh(
        JPH::Array<JPH::Float3>&& vertices, JPH::Array<JPH::IndexedTriangle>&& triangles);

    // ------------------------------------ //
    // Advanced game related shapes

    static JPH::RefConst<JPH::Shape> CreateMicrobeShapeConvex(JVecF3* points, uint32_t pointCount, float density = 1000,
        float scale = 1, float thickness = 1.0f, const JPH::PhysicsMaterial* material = nullptr);
    static JPH::RefConst<JPH::Shape> CreateMicrobeShapeSpheres(JVecF3* points, uint32_t pointCount,
        float density = 1000, float scale = 1, const JPH::PhysicsMaterial* material = nullptr);
};

} // namespace Thrive::Physics

static_assert(
    sizeof(SubShapeDefinition) == sizeof(Thrive::Physics::SubShapeDefinition), "sub-shape creation data size mismatch");
