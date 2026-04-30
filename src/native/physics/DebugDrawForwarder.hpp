#pragma once

#include <Jolt/Jolt.h>

#ifdef JPH_DEBUG_RENDERER

#include <cstddef>

#include <Jolt/Renderer/DebugRenderer.h>

#include "core/Mutex.hpp"
#include "core/NativeLibIntercommunication.hpp"

namespace Thrive::Physics
{

constexpr float MaxDebugDrawRate = 1 / 60.0f;
constexpr bool AutoAdjustDebugDrawRateWhenSlow = true;
constexpr float DebugDrawLODBias = 2;
constexpr float DefaultMaxDistanceToDrawLinesFromCamera = 180;

/// When we have a lot of forwarded data, we sort it by distance to the camera to ensure the closest things are drawn
/// first before we run out of draw budget
constexpr size_t SortForwardedDebugLinesAfter = 20000;
constexpr size_t SortForwardedDebugTrianglesAfter = 15000;

/// \brief Forwards debug draw from the physics system out of this native library
class DebugDrawForwarder : public JPH::DebugRenderer
{
    using LineDrawEntry = std::tuple<JVec3, JVec3, JColour>;
    using TriangleDrawEntry = std::tuple<JVec3, JVec3, JVec3, JColour>;

public:
    // One extra level of deferring to allow this to not need to be updated whenever the pointers change as that'd be
    // a bit hard to forward from the other project
    using LineCallback = OnDebugLines*;
    using TriangleCallback = OnDebugTriangles*;

    /// \brief Variant of vertex that doesn't require converting back to floats after world space calculation
    /// and has already converted colour info
    class DVertex
    {
    public:
        JVec3 mPosition;
        JPH::Float3 mNormal;
        JPH::Float2 mUV;
        JColour mColor;
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
    void SetOutputLineReceiver(LineCallback callback);
    void SetOutputTriangleReceiver(TriangleCallback callback);

    void ClearOutputReceivers();

    bool HasAReceiver() const noexcept;

    // DebugRenderer interface implementation
    void DrawLine(JPH::RVec3Arg inFrom, JPH::RVec3Arg inTo, JPH::ColorArg inColor) override;
    void DrawTriangle(JPH::RVec3Arg inV1, JPH::RVec3Arg inV2, JPH::RVec3Arg inV3, JPH::ColorArg inColor,
        ECastShadow inCastShadow = ECastShadow::Off) override;
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
        cameraPositionForDrawDistance = JPH::RVec3(position);
        cameraPositionFasterAccess = JoltToJVec3(position);
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

    inline void SetMaxDrawDistance(float drawDistance)
    {
        maxModelDistance = drawDistance;
        maxModelDistanceSquared = static_cast<double>(drawDistance) * drawDistance;
    }

private:
    void DrawTriangleInternal(
        const DVertex& vertex1, const DVertex& vertex2, const DVertex& vertex3, JColour colourTint, bool wireFrame);

    [[nodiscard]] inline bool IsPointWithinDrawDistance(JPH::RVec3Arg position) const
    {
        return (position - cameraPositionForDrawDistance).LengthSq() <= maxModelDistanceSquared;
    }

    void SortDrawBuffersIfAboveThreshold();

    [[nodiscard]] double GetDistanceSquared(const JVec3& position) const
    {
        // Use a camera position info in fast-to-access memory layout
        const auto x = position.X - cameraPositionFasterAccess.X;
        const auto y = position.Y - cameraPositionFasterAccess.Y;
        const auto z = position.Z - cameraPositionFasterAccess.Z;

        return x * x + y * y + z * z;
    }

    [[nodiscard]] double GetClosestDistanceSquared(const LineDrawEntry& entry) const
    {
        return std::min(GetDistanceSquared(std::get<0>(entry)), GetDistanceSquared(std::get<1>(entry)));
    }

    [[nodiscard]] double GetClosestDistanceSquared(const TriangleDrawEntry& entry) const
    {
        return std::min(std::min(GetDistanceSquared(std::get<0>(entry)), GetDistanceSquared(std::get<1>(entry))),
            GetDistanceSquared(std::get<2>(entry)));
    }

private:
    /// Apparently debug rendering happens from multiple threads, so we need a lock
    Mutex mutex;

    /// Next ID to use for a predefined batch of geometry
    uint32_t nextBatchID = 1;

    // Might get used if sending each geometry just once is implemented
    // uint32_t nextGeometryID = 1;

    // /// Predefined geometries and mapping to their IDs
    // std::unordered_map<GeometryRef, uint32_t> cachedGeometries;

    // ------------------------------------ //
    // Actual variables of this debug forwarder, everything else needed to be default Jolt stuff

    std::vector<LineDrawEntry> lineBuffer;
    std::vector<TriangleDrawEntry> triangleBuffer;

    LineCallback lineCallback = nullptr;
    TriangleCallback triangleCallback = nullptr;

    JPH::Vec3 cameraPosition = {};
    JPH::RVec3 cameraPositionForDrawDistance = {};
    JVec3 cameraPositionFasterAccess = {};
    float cameraLODBias = DebugDrawLODBias;
    float minDrawDelta = MaxDebugDrawRate;
    bool adjustRateOnLag = AutoAdjustDebugDrawRateWhenSlow;
    float maxModelDistance = DefaultMaxDistanceToDrawLinesFromCamera;
    double maxModelDistanceSquared =
        static_cast<double>(DefaultMaxDistanceToDrawLinesFromCamera) * DefaultMaxDistanceToDrawLinesFromCamera;

    float timeSinceDraw = 1;
};

} // namespace Thrive::Physics

#endif // JPH_DEBUG_RENDERER
