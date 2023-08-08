// ------------------------------------ //
#include "DebugDrawForwarder.hpp"

#include "Jolt/Math/Float4.h"

#include "core/Logger.hpp"
#include "core/Time.hpp"

// #define ENSURE_NO_COLOUR_OVER_SATURATION

// ------------------------------------ //
#ifdef JPH_DEBUG_RENDERER
namespace Thrive::Physics
{

/// \brief Apparently Jolt requires us to handle geometry references
class BatchImpl : public JPH::RefTargetVirtual,
                  public RefCounted
{
public:
    explicit BatchImpl(uint32_t inID) : id(inID)
    {
    }

    void AddRef() override
    {
        RefCounted::AddRef();
    }

    void Release() override
    {
        RefCounted::Release();
    }

    /// Vertices if this is specified like that, if empty then vertices are used
    std::vector<JPH::DebugRenderer::Triangle> triangles;

    /// Alternative to specifying entire triangles, this is an indexed rendering approach
    std::vector<JPH::DebugRenderer::Vertex> vertices;
    std::vector<uint32_t> indices;

    uint32_t id;
};

JPH::DVec3 FloatToDVec(JPH::Float3 input)
{
    return {input.x, input.y, input.z};
}

JPH::Float4 ColorToFloat4(const JPH::ColorArg color)
{
    constexpr float multiplier = 1 / 255.0f;
    return {(float)color.r * multiplier, (float)color.g * multiplier, (float)color.b * multiplier,
        (float)color.a * multiplier};
}

#ifdef ENSURE_NO_COLOUR_OVER_SATURATION
JPH::Float4 MixColour(JPH::Float4 baseColour, JPH::Float4 colourTint)
{
    return {std::max(baseColour.x * colourTint.x, 1.0f), std::max(baseColour.y * colourTint.y, 1.0f),
        std::max(baseColour.z * colourTint.z, 1.0f), std::max(baseColour.w * colourTint.w, 1.0f)};
}
#else
JPH::Float4 MixColour(JPH::Float4 baseColour, JPH::Float4 colourTint)
{
    return {baseColour.x * colourTint.x, baseColour.y * colourTint.y, baseColour.z * colourTint.z,
        baseColour.w * colourTint.w};
}
#endif // ENSURE_NO_COLOUR_OVER_SATURATION

// Apparently we need to act like a GPU just to get debug rendering done...
DebugDrawForwarder::DVertex TransformVertex(const JPH::RMat44& matrix, const JPH::DebugRenderer::Vertex& vertex)
{
    // TODO: for proper usage should we transform the normal as well?
    return DebugDrawForwarder::DVertex{
        matrix * FloatToDVec(vertex.mPosition), vertex.mNormal, vertex.mUV, ColorToFloat4(vertex.mColor)};
}

// ------------------------------------ //
DebugDrawForwarder::DebugDrawForwarder()
{
    Initialize();
}

// ------------------------------------ //
void DebugDrawForwarder::FlushOutput()
{
    const auto startTime = TimingClock::now();

    Lock lock(mutex);

    // Send the accumulated data
    if (lineCallback != nullptr)
    {
        for (const auto& line : lineBuffer)
        {
            lineCallback(std::get<0>(line), std::get<1>(line), std::get<2>(line));
        }
    }

    if (triangleCallback != nullptr)
    {
        for (const auto& triangle : triangleBuffer)
        {
            triangleCallback(
                std::get<0>(triangle), std::get<1>(triangle), std::get<2>(triangle), std::get<3>(triangle));
        }
    }

    lineBuffer.clear();
    triangleBuffer.clear();

    // TODO: clear geometries that haven't been used for a long time?

    if (adjustRateOnLag)
    {
        const auto duration = std::chrono::duration_cast<SecondDuration>(TimingClock::now() - startTime).count();

        if (duration < 0.012f)
        {
            minDrawDelta = MaxDebugDrawRate;
        }
        else if (duration < 0.018f)
        {
            minDrawDelta = 1 / 30.0f;
        }
        else if (duration < 0.025f)
        {
            minDrawDelta = 1 / 15.0f;
        }
        else
        {
            // Lag is bad, set to the lowest possible time
            minDrawDelta = 1 / 10.0f;
        }
    }
}

void DebugDrawForwarder::SetOutputLineReceiver(std::function<LineCallback> callback)
{
    lineCallback = std::move(callback);
}

void DebugDrawForwarder::SetOutputTriangleReceiver(std::function<TriangleCallback> callback)
{
    triangleCallback = std::move(callback);
}

void DebugDrawForwarder::ClearOutputReceivers()
{
    lineCallback = nullptr;
    triangleCallback = nullptr;
}

bool DebugDrawForwarder::HasAReceiver() const noexcept
{
    return lineCallback || triangleCallback;
}

// ------------------------------------ //
void DebugDrawForwarder::DrawLine(JPH::RVec3Arg inFrom, JPH::RVec3Arg inTo, JPH::ColorArg inColor)
{
    Lock lock(mutex);
    lineBuffer.emplace_back(inFrom, inTo, ColorToFloat4(inColor));
}

void DebugDrawForwarder::DrawTriangle(
    JPH::RVec3Arg inV1, JPH::RVec3Arg inV2, JPH::RVec3Arg inV3, JPH::ColorArg inColor, ECastShadow inCastShadow)
{
    // TODO: shadow support?
    UNUSED(inCastShadow);

    Lock lock(mutex);
    triangleBuffer.emplace_back(inV1, inV2, inV3, ColorToFloat4(inColor));
}

// It is always assumed that the renderer was responsible for creating the geometry instances, so we can cast them here
#pragma clang diagnostic push
#pragma ide diagnostic ignored "cppcoreguidelines-pro-type-static-cast-downcast"

void DebugDrawForwarder::DrawGeometry(JPH::RMat44Arg inModelMatrix, const JPH::AABox& inWorldSpaceBounds,
    float inLODScaleSq, JPH::ColorArg inModelColor, const JPH::DebugRenderer::GeometryRef& inGeometry,
    JPH::DebugRenderer::ECullMode inCullMode, JPH::DebugRenderer::ECastShadow inCastShadow,
    JPH::DebugRenderer::EDrawMode inDrawMode)
{
    // Skip rendering too faraway objects
    const auto distance = inWorldSpaceBounds.GetSqDistanceTo(cameraPosition);
    if (distance > maxModelDistance * maxModelDistance)
        return;

    const JPH::RMat44 transformMatrix = inModelMatrix;

    // TODO: support for different cull modes and shadows (front face probably needs to flip the triangles and no cull
    // needs to somehow signal up that culling should not be used
    if (inCullMode == ECullMode::CullBackFace)
    {
        // Default, already works
    }

    UNUSED(inCastShadow);

    // TODO: frustum culling? (or at least max distance from camera?)
    UNUSED(inWorldSpaceBounds);

    // TODO: Geometry caching (this is different from the raw batches as this has LODs)
    /*auto& geometryID = cachedGeometries[inGeometry];
    if (geometryID == 0)
    {
        // New geometry
        geometryID = nextGeometryID++;

        inGeometry->mBounds.mMin;
        inGeometry->mBounds.mMax;

        for (const LOD& lod : inGeometry->mLODs)
        {
            lod.mDistance;
            static_cast<const BatchImpl*>(lod.mTriangleBatch.GetPtr())->id;
        }
    }*/

    const auto modelTint = ColorToFloat4(inModelColor);

    for (const LOD& lod : inGeometry->mLODs)
    {
        // Due to us just sending all triangles etc. on each frame we try to pick a pretty low LOD here
        if (lod.mDistance * inLODScaleSq < distance * cameraLODBias)
            continue;

        const bool wireframe = inDrawMode == JPH::DebugRenderer::EDrawMode::Wireframe;
        const auto& meshData = *static_cast<const BatchImpl*>(lod.mTriangleBatch.GetPtr());

        Lock lock(mutex);

        if (meshData.triangles.empty())
        {
            for (size_t i = 0; i < meshData.indices.size(); i += 3)
            {
                DrawTriangleInternal(TransformVertex(transformMatrix, meshData.vertices[meshData.indices[i]]),
                    TransformVertex(transformMatrix, meshData.vertices[meshData.indices[i + 1]]),
                    TransformVertex(transformMatrix, meshData.vertices[meshData.indices[i + 2]]), modelTint, wireframe);
            }
        }
        else
        {
            for (const auto& triangle : meshData.triangles)
            {
                DrawTriangleInternal(TransformVertex(transformMatrix, triangle.mV[0]),
                    TransformVertex(transformMatrix, triangle.mV[1]), TransformVertex(transformMatrix, triangle.mV[2]),
                    modelTint, wireframe);
            }
        }

        return;
    }

    LOG_ERROR("No debug draw LOD could be selected");
}

#pragma clang diagnostic pop

void DebugDrawForwarder::DrawText3D(
    JPH::RVec3Arg inPosition, const std::string_view& inString, JPH::ColorArg inColor, float inHeight)
{
    // TODO: text rendering
    UNUSED(inPosition);
    UNUSED(inString);
    UNUSED(inColor);
    UNUSED(inHeight);
}

// ------------------------------------ //
JPH::DebugRenderer::Batch DebugDrawForwarder::CreateTriangleBatch(
    const JPH::DebugRenderer::Triangle* inTriangles, int inTriangleCount)
{
    if (inTriangles == nullptr || inTriangleCount == 0)
        return new BatchImpl(0);

    Lock lock(mutex);

    // TODO: maybe would be better to send this data to the other side to not duplicate a ton of vertices when drawing
    // Note that for certain data it is dynamically generated each frame so some solution is also needed there

    const auto batchId = nextBatchID++;

    // This isn't immediately wrapped in the smart pointer so this could leak if the copying throws, but as this is
    // just debug rendering there's not much point in doing this exactly right
    auto result = new BatchImpl(batchId);

    result->triangles.reserve(inTriangleCount);
    for (int i = 0; i < inTriangleCount; ++i)
    {
        result->triangles.emplace_back(inTriangles[i]);
    }

    return result;
}

JPH::DebugRenderer::Batch DebugDrawForwarder::CreateTriangleBatch(
    const JPH::DebugRenderer::Vertex* inVertices, int inVertexCount, const uint32_t* inIndices, int inIndexCount)
{
    if (inVertices == nullptr || inVertexCount == 0 || inIndices == nullptr || inIndexCount == 0)
        return new BatchImpl(0);

    Lock lock(mutex);

    // See the TODO in the above method

    const auto batchId = nextBatchID++;

    auto result = new BatchImpl(batchId);

    result->vertices.reserve(inVertexCount);
    for (int i = 0; i < inVertexCount; ++i)
    {
        result->vertices.emplace_back(inVertices[i]);
    }

    result->indices.reserve(inVertexCount);
    for (int i = 0; i < inIndexCount; ++i)
    {
        result->indices.emplace_back(inIndices[i]);
    }

    return result;
}

// ------------------------------------ //
void DebugDrawForwarder::DrawTriangleInternal(
    const DVertex& vertex1, const DVertex& vertex2, const DVertex& vertex3, JPH::Float4 colourTint, bool wireFrame)
{
    if (wireFrame)
    {
        lineBuffer.emplace_back(vertex1.mPosition, vertex2.mPosition, MixColour(vertex1.mColor, colourTint));
        lineBuffer.emplace_back(vertex2.mPosition, vertex3.mPosition, MixColour(vertex2.mColor, colourTint));
        lineBuffer.emplace_back(vertex3.mPosition, vertex1.mPosition, MixColour(vertex3.mColor, colourTint));
    }
    else
    {
        // TODO: per vertex colour
        triangleBuffer.emplace_back(
            vertex1.mPosition, vertex2.mPosition, vertex3.mPosition, MixColour(vertex1.mColor, colourTint));
    }
}

} // namespace Thrive::Physics
#endif // JPH_DEBUG_RENDERER
