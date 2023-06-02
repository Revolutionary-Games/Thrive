// ------------------------------------ //
#include "ShapeWrapper.hpp"

// ------------------------------------ //
namespace Thrive::Physics
{

ShapeWrapper::ShapeWrapper(const JPH::RefConst<JPH::Shape>& wrappedShape) : shape(wrappedShape)
{
}

ShapeWrapper::ShapeWrapper(JPH::RefConst<JPH::Shape>&& wrappedShape) : shape(wrappedShape)
{
}

} // namespace Thrive::Physics
