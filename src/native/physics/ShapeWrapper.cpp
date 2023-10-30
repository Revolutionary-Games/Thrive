// ------------------------------------ //
#include "ShapeWrapper.hpp"

#include "core/Logger.hpp"

// ------------------------------------ //
namespace Thrive::Physics
{

#ifdef USE_OBJECT_POOLS
ShapeWrapper::ShapeWrapper(const JPH::RefConst<JPH::Shape>& wrappedShape, ReleaseCallback deleteCallback) :
    RefCounted<ShapeWrapper>(deleteCallback),
#else
ShapeWrapper::ShapeWrapper(const JPH::RefConst<JPH::Shape>& wrappedShape) :
#endif
    shape(wrappedShape)
{
    if (shape == nullptr)
    {
        LOG_ERROR("Cannot create a shape where the Jolt shape failed to be created");
        abort();
    }
}

#ifdef USE_OBJECT_POOLS
ShapeWrapper::ShapeWrapper(JPH::RefConst<JPH::Shape>&& wrappedShape, ReleaseCallback deleteCallback) :
    RefCounted<ShapeWrapper>(deleteCallback),
#else
ShapeWrapper::ShapeWrapper(JPH::RefConst<JPH::Shape>&& wrappedShape) :
#endif
    shape(wrappedShape)
{
}

} // namespace Thrive::Physics
