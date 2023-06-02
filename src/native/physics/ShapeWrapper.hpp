#pragma once

#include "Jolt/Core/Reference.h"
#include "Jolt/Physics/Collision/Shape/Shape.h"

namespace Thrive::Physics
{

/// \brief Wrapper class around JPH::Shape to allow the C API to use shapes
class ShapeWrapper : public RefCounted
{
public:
    explicit ShapeWrapper(const JPH::RefConst<JPH::Shape>& wrappedShape);
    explicit ShapeWrapper(JPH::RefConst<JPH::Shape>&& wrappedShape);

    const inline JPH::RefConst<JPH::Shape>& GetShape() const
    {
        return shape;
    }

private:
    const JPH::RefConst<JPH::Shape> shape;
};

} // namespace Thrive::Physics
