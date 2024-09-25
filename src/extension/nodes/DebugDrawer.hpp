#pragma once

#include <vector>

#include <godot_cpp/classes/control.hpp>
#include <godot_cpp/classes/material.hpp>
#include <Jolt/Jolt.h>
#include <Jolt/Math/Real.h>

namespace godot
{
class MeshInstance3D;
class ImmediateMesh;
} // namespace godot

namespace Thrive
{
/// How far away from the world origin debug draw works. When farther away no debug drawing happens. Setting this too
/// far seems to trigger a problem in Godot where a grey overlay is over all 3D content blocking everything.
/// TODO: if we need bigger worlds then the DebugDrawer will need to be updated to reposition its meshes to get a
/// smaller bounding box working
constexpr float DEBUG_DRAW_MAX_DISTANCE_ORIGIN = 1000000000.0f;

/// \brief Hides the debug drawing after this time of inactivity. Makes sure debug draw is still visible after a while
/// the game is paused, but will eventually clear up (for example if going to a part of the game that doesn't
/// use debug drawing
constexpr float HideAfterInactiveFor = 15.0f;

// These are used from C# so may not be changed
constexpr auto SignalNameOnDebugCameraPositionChanged = "OnPhysicsDebugCameraPositionChanged";
constexpr auto SignalNameOnPhysicsDebugLevelChanged = "OnPhysicsDebugLevelChanged";

/// \brief The native code side of the debug drawing in Thrive
///
/// This also has a C# side to forward requests here but the core logic is here in C++ for maximum performance
class DebugDrawer final : public godot::Control
{
    GDCLASS(DebugDrawer, godot::Control)

    static constexpr float LineLifeTime = 8;

    // For consistency should match the C# side
    static constexpr float PointLineWidth = 0.3f;

    static const godot::Vector3 pointOffsetLeft;
    static const godot::Vector3 pointOffsetUp;
    static const godot::Vector3 pointOffsetRight;
    static const godot::Vector3 pointOffsetDown;
    static const godot::Vector3 pointOffsetForward;
    static const godot::Vector3 pointOffsetBack;

    struct TimedLine
    {
    public:
        inline TimedLine(const godot::Vector3& from, const godot::Vector3& to, const godot::Color& colour) :
            From(from), To(to), Colour(colour), TimePassed(0)
        {
        }

        // These can't be const to have copy assignment operators
        godot::Vector3 From;
        godot::Vector3 To;
        godot::Color Colour;
        float TimePassed;
    };

public:
    const godot::StringName SignalOnDebugCameraPositionChanged;
    const godot::StringName SignalOnPhysicsDebugLevelChanged;

    DebugDrawer();
    ~DebugDrawer();

    void _ready() override;
    void Init();

    void _process(double delta) override;

    void IncrementPhysicsDebugLevel() noexcept;
    void EnablePhysicsDebug() noexcept;
    void DisablePhysicsDebug() noexcept;

    inline void AddLine(const godot::Vector3& from, const godot::Vector3& to, const godot::Color& colour) noexcept
    {
        AddTimedLine({from, to, colour});
    }

    inline void AddPoint(const godot::Vector3& position, const godot::Color& colour) noexcept
    {
        AddLine(position + pointOffsetLeft, position + pointOffsetRight, colour);
        AddLine(position + pointOffsetUp, position + pointOffsetDown, colour);
        AddLine(position + pointOffsetForward, position + pointOffsetBack, colour);
    }

    void OnReceiveLines(const std::vector<std::tuple<JPH::RVec3Arg, JPH::RVec3Arg, JPH::Float4>>& lineBuffer) noexcept;
    void OnReceiveTriangles(const std::vector<std::tuple<JPH::RVec3Arg, JPH::RVec3Arg, JPH::RVec3Arg, JPH::Float4>>&
            triangleBuffer) noexcept;

    bool RegisterDebugDraw() noexcept;

    [[nodiscard]] int GetDebugLevel() const noexcept
    {
        return currentPhysicsDebugLevel;
    }

    void SetDebugLevel(int newLevel) noexcept
    {
        currentPhysicsDebugLevel = newLevel;

        if (currentPhysicsDebugLevel > MAX_DEBUG_DRAW_LEVEL)
            currentPhysicsDebugLevel = MAX_DEBUG_DRAW_LEVEL;
    }

    [[nodiscard]] bool GetDebugDrawAvailable() const noexcept
    {
        return physicsDebugSupported;
    }

    [[nodiscard]] godot::Vector3 GetDebugCameraLocation() const noexcept
    {
        return debugCameraLocation;
    }

    void SetDebugCameraLocation(godot::Vector3 location) noexcept
    {
        if (debugCameraLocation == location)
            return;

        debugCameraLocation = location;

        emit_signal(SignalNameOnDebugCameraPositionChanged, debugCameraLocation);
    }

    [[nodiscard]] godot::Variant GetThis() noexcept
    {
        return {reinterpret_cast<int64_t>(this)};
    }

protected:
    static void _bind_methods();

private:
    void DrawLine(const godot::Vector3& from, const godot::Vector3& to, const godot::Color& colour);
    void DrawTriangle(const godot::Vector3& vertex1, const godot::Vector3& vertex2, const godot::Vector3& vertex3,
        const godot::Color& colour);
    void StartDrawingIfNotYetThisFrame();

    inline void AddTimedLine(const TimedLine& line)
    {
        timedLines.emplace_back(line);
    }

    void HandleTimedLines(float delta);
    void OnlyElapseLineTime(float delta);

private:
    static DebugDrawer* instance;

    godot::Ref<godot::Material> lineMaterial;
    godot::Ref<godot::Material> triangleMaterial;

    godot::MeshInstance3D* lineDrawer = nullptr;
    godot::MeshInstance3D* triangleDrawer = nullptr;

    godot::Ref<godot::ImmediateMesh> lineMesh;
    godot::Ref<godot::ImmediateMesh> triangleMesh;

    godot::Vector3 debugCameraLocation{0, 0, 0};

    std::vector<TimedLine> timedLines;

    /// \brief As the data is not drawn each frame, there's a delay before hiding the draw result
    double timeInactive = 0;

    int currentPhysicsDebugLevel = 0;

    /// Set a max limit to not draw way too much stuff and slow down things a ton. 4 megabytes
    const int drawMemoryLimit = 1024 * 1024 * 4;
    int usedDrawMemory = 0;
    int extraNeededDrawMemory = 0;

    bool physicsDebugSupported = false;
    bool warnedAboutHittingMemoryLimit = false;
    bool drawnThisFrame = false;

    bool startedLineDraw = false;
    bool startedTriangleDraw = false;
};

} // namespace Thrive
