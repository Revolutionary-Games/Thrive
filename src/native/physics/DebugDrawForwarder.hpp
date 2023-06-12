#pragma once

#include "Jolt/Jolt.h"

#ifdef JPH_DEBUG_RENDERER

#include "Jolt/Renderer/DebugRenderer.h"

#include "core/Mutex.hpp"

namespace Thrive::Physics
{

constexpr float MaxDebugDrawRate = 1 / 60.0f;
constexpr bool AutoAdjustDebugDrawRateWhenSlow = true;
constexpr float DebugDrawLODBias = 2;
constexpr float DefaultMaxDistanceToDrawLinesFromCamera = 120;

/// \brief Forwards debug draw from the physics system out of this native library
/// \todo It would probably benefit the performance a ton if this was directly made to interact with the Godot C++ API
class DebugDrawForwarder : public JPH::DebugRenderer
{
public:
    using LineCallback = void(JPH::RVec3Arg from, JPH::RVec3Arg to, JPH::Float4 colour);
    using TriangleCallback = void(JPH::RVec3Arg v1, JPH::RVec3Arg v2, JPH::RVec3Arg v3, JPH::Float4 colour);

    /// \brief Variant of vertex that doesn't require converting back to floats after world space calculation
    /// and has already converted colour info
    class DVertex
    {
    public:
        JPH::RVec3Arg mPosition;
        JPH::Float3 mNormal;
        JPH::Float2 mUV;
        JPH::Float4 mColor;
    };

private:
    DebugDrawForwarder();

public:
    static DebugDrawForwarder& GetInstance()
    {
        static DebugDrawForwarder instance;
        return instance;
    }

    void FlushOutput();
    void SetOutputLineReceiver(std::function<LineCallback> callback);
    void SetOutputTriangleReceiver(std::function<TriangleCallback> callback);

    void ClearOutputReceivers();

    bool HasAReceiver() const noexcept;

    // DebugRenderer interface implementation
    void DrawLine(JPH::RVec3Arg inFrom, JPH::RVec3Arg inTo, JPH::ColorArg inColor) override;
    void DrawTriangle(JPH::RVec3Arg inV1, JPH::RVec3Arg inV2, JPH::RVec3Arg inV3, JPH::ColorArg inColor) override;
    void DrawGeometry(JPH::RMat44Arg inModelMatrix, const JPH::AABox& inWorldSpaceBounds, float inLODScaleSq,
        JPH::ColorArg inModelColor, const GeometryRef& inGeometry, ECullMode inCullMode, ECastShadow inCastShadow,
        EDrawMode inDrawMode) override;
    void DrawText3D(
        JPH::RVec3Arg inPosition, const std::string_view& inString, JPH::ColorArg inColor, float inHeight) override;

    // These seem to be about caching and reusing things
    Batch CreateTriangleBatch(const Triangle* inTriangles, int inTriangleCount) override;
    Batch CreateTriangleBatch(
        const Vertex* inVertices, int inVertexCount, const uint32_t* inIndices, int inIndexCount) override;

    /// \brief Returns true once it is time to render debug stuff
    ///
    /// This is used to rate limit the expensive debug drawing a bit
    [[nodiscard]] bool TimeToRenderDebug(float delta)
    {
        timeSinceDraw += delta;

        if (timeSinceDraw >= minDrawDelta)
        {
            timeSinceDraw = 0;
            return true;
        }

        return false;
    }

    inline void SetCameraPositionForLOD(JPH::Vec3Arg position)
    {
        cameraPosition = position;
    }

    inline void SetCameraLODBias(float newBias)
    {
        cameraLODBias = newBias;
    }

    inline void SetMaxDebugDrawFPS(float framerate)
    {
        minDrawDelta = 1 / framerate;
    }

    inline void SetAutoAdjustMaxDrawFPS(bool autoAdjustOnLag)
    {
        adjustRateOnLag = autoAdjustOnLag;
    }

    inline void SetMaxDrawDistance(float drawDistance){
        maxModelDistance = drawDistance;
    }

private:
    void DrawTriangleInternal(
        const DVertex& vertex1, const DVertex& vertex2, const DVertex& vertex3, JPH::Float4 colourTint, bool wireFrame);

private:
    /// Apparently debug rendering happens from multiple threads so we need a lock
    Mutex mutex;

    /// Next ID to use for a predefined batch of geometry
    uint32_t nextBatchID = 1;

    // Might get used if sending each geometry just once is implemented
    // uint32_t nextGeometryID = 1;

    // /// Predefined geometries and mapping to their IDs
    // std::unordered_map<GeometryRef, uint32_t> cachedGeometries;

    // ------------------------------------ //
    // Actual variables of this debug forwarder, everything else needed to be default Jolt stuff

    std::vector<std::tuple<JPH::RVec3Arg, JPH::RVec3Arg, JPH::Float4>> lineBuffer;
    std::vector<std::tuple<JPH::RVec3Arg, JPH::RVec3Arg, JPH::RVec3Arg, JPH::Float4>> triangleBuffer;

    std::function<LineCallback> lineCallback;
    std::function<TriangleCallback> triangleCallback;

    JPH::Vec3 cameraPosition = {};
    float cameraLODBias = DebugDrawLODBias;
    float minDrawDelta = MaxDebugDrawRate;
    bool adjustRateOnLag = AutoAdjustDebugDrawRateWhenSlow;
    float maxModelDistance = DefaultMaxDistanceToDrawLinesFromCamera;

    float timeSinceDraw = 1;
};

} // namespace Thrive::Physics

#endif // JPH_DEBUG_RENDERER
