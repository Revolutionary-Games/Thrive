// ------------------------------------ //
#include "DebugDrawer.hpp"

#include <godot_cpp/classes/camera3d.hpp>
#include <godot_cpp/classes/engine.hpp>
#include <godot_cpp/classes/immediate_mesh.hpp>
#include <godot_cpp/classes/mesh_instance3d.hpp>
#include <godot_cpp/classes/resource_loader.hpp>
#include <godot_cpp/classes/viewport.hpp>
#include <godot_cpp/variant/utility_functions.hpp>

#include "core/GodotJoltConversions.hpp"
#include "core/ThriveConfig.hpp"

// ------------------------------------ //
namespace Thrive
{

/// \brief Assumption of what the vertex layout memory use is for immediate geometry (3 floats for position,
/// 3 floats for normal, 2 floats for UVs, 4 floats for colour).
///
/// It's really hard to find this in Godot source code so this is a pure assumption that has been tested to work fine.
constexpr long MemoryUseOfIntermediateVertex = sizeof(float) * (3 + 3 + 2 + 4);

/// 2 vertices + space in index buffer
constexpr long SingleLineDrawMemoryUse = MemoryUseOfIntermediateVertex * 2 + sizeof(uint32_t);

/// 3 vertices
constexpr long SingleTriangleDrawMemoryUse = MemoryUseOfIntermediateVertex * 3 + sizeof(uint32_t);

const godot::Vector3 DebugDrawer::pointOffsetLeft = {-PointLineWidth, 0, 0};
const godot::Vector3 DebugDrawer::pointOffsetUp = {0, PointLineWidth, 0};
const godot::Vector3 DebugDrawer::pointOffsetRight = {PointLineWidth, 0, 0};
const godot::Vector3 DebugDrawer::pointOffsetDown = {0, -PointLineWidth, 0};
const godot::Vector3 DebugDrawer::pointOffsetForward = {0, 0, -PointLineWidth};
const godot::Vector3 DebugDrawer::pointOffsetBack = {0, 0, PointLineWidth};

DebugDrawer* DebugDrawer::instance = nullptr;

void DebugDrawer::_bind_methods()
{
    using namespace godot;

    ClassDB::bind_method(D_METHOD("get_debug_level"), &DebugDrawer::GetDebugLevel);
    ClassDB::bind_method(D_METHOD("set_debug_level", "newLevel"), &DebugDrawer::SetDebugLevel);
    ADD_PROPERTY(PropertyInfo(Variant::INT, "debug_level"), "set_debug_level", "get_debug_level");

    ClassDB::bind_method(D_METHOD("get_debug_draw_available"), &DebugDrawer::GetDebugDrawAvailable);
    ADD_PROPERTY(PropertyInfo(Variant::BOOL, "debug_draw_available", PROPERTY_HINT_NONE, "", PROPERTY_USAGE_NO_EDITOR),
        {}, "get_debug_draw_available");

    ClassDB::bind_method(D_METHOD("init"), &DebugDrawer::Init);

    ClassDB::bind_method(D_METHOD("get_debug_camera_location"), &DebugDrawer::GetDebugCameraLocation);
    ClassDB::bind_method(D_METHOD("set_debug_camera_location", "location"), &DebugDrawer::SetDebugCameraLocation);
    ADD_PROPERTY(PropertyInfo(Variant::VECTOR3, "debug_camera_location"), "set_debug_camera_location",
        "get_debug_camera_location");

    ClassDB::bind_method(D_METHOD("increment_physics_debug_level"), &DebugDrawer::IncrementPhysicsDebugLevel);
    ClassDB::bind_method(D_METHOD("enable_physics_debug"), &DebugDrawer::EnablePhysicsDebug);
    ClassDB::bind_method(D_METHOD("disable_physics_debug"), &DebugDrawer::DisablePhysicsDebug);

    ClassDB::bind_method(D_METHOD("add_line", "from", "to", "colour"), &DebugDrawer::AddLine);
    ClassDB::bind_method(D_METHOD("add_point", "point", "colour"), &DebugDrawer::AddPoint);

    ADD_SIGNAL(MethodInfo(SignalNameOnDebugCameraPositionChanged, PropertyInfo(Variant::VECTOR3, "position")));
    ADD_SIGNAL(MethodInfo(SignalNameOnPhysicsDebugLevelChanged, PropertyInfo(Variant::INT, "debugLevel")));

    ClassDB::bind_method(D_METHOD("get_native_instance"), &DebugDrawer::GetThis);
    ClassDB::bind_method(D_METHOD("register_debug_draw"), &DebugDrawer::RegisterDebugDraw);
}

DebugDrawer::DebugDrawer() :
    SignalOnDebugCameraPositionChanged(SignalNameOnDebugCameraPositionChanged),
    SignalOnPhysicsDebugLevelChanged(SignalNameOnPhysicsDebugLevelChanged)
{
    if (godot::Engine::get_singleton()->is_editor_hint())
        return;

    if (instance != nullptr)
        ERR_PRINT("Multiple DebugDrawer native instances created");

    instance = this;
}

DebugDrawer::~DebugDrawer()
{
    auto* config = ThriveConfig::Instance();

    if (config)
    {
        config->RegisterDebugDrawReceiver(nullptr);
    }

    instance = nullptr;
}

void DebugDrawer::_ready()
{
    // We have to do initialization like this as apparently an attached C# script will mess things up and not call the
    // native ready
    if (lineDrawer == nullptr)
        Init();
}

void DebugDrawer::Init()
{
    if (lineDrawer != nullptr)
        ERR_PRINT("Init called twice");

    Node::_ready();

    if (godot::Engine::get_singleton()->is_editor_hint())
        return;

    lineMaterial = godot::ResourceLoader::get_singleton()->load("res://src/engine/DebugLineMaterial.tres");
    triangleMaterial = godot::ResourceLoader::get_singleton()->load("res://src/engine/DebugTriangleMaterial.tres");

    lineDrawer = get_node<godot::MeshInstance3D>("LineDrawer");
    triangleDrawer = get_node<godot::MeshInstance3D>("TriangleDrawer");

    if (!lineDrawer || !triangleDrawer)
    {
        ERR_PRINT("Failed to get DebugDrawer required child node");
        return;
    }

    // Make sure the debug stuff is always rendered
    const auto quiteBigAABB = godot::AABB(godot::Vector3{0, 0, 0},
        godot::Vector3{DEBUG_DRAW_MAX_DISTANCE_ORIGIN, DEBUG_DRAW_MAX_DISTANCE_ORIGIN, DEBUG_DRAW_MAX_DISTANCE_ORIGIN});

    lineDrawer->set_custom_aabb(quiteBigAABB);
    triangleDrawer->set_custom_aabb(quiteBigAABB);

    lineMesh = godot::Ref<godot::ImmediateMesh>(memnew(godot::ImmediateMesh));
    triangleMesh = godot::Ref<godot::ImmediateMesh>(memnew(godot::ImmediateMesh));

    lineDrawer->set_mesh(lineMesh);
    lineDrawer->set_visible(false);

    triangleDrawer->set_mesh(triangleMesh);
    triangleDrawer->set_visible(false);

    // TODO: implement debug text drawing (this is a Control to support that in the future)
}

// ------------------------------------ //
void DebugDrawer::_process(double delta)
{
    // Don't do anything if not initialized
    if (lineDrawer == nullptr)
        return;

    if (!timedLines.empty())
    {
        // Only draw the other debug lines if physics lines have been updated to avoid flicker
        if (!physicsDebugSupported || currentPhysicsDebugLevel == 0 || drawnThisFrame)
        {
            HandleTimedLines((float)delta);
        }
        else
        {
            // To make lines not stick around longer with physics debug
            OnlyElapseLineTime((float)delta);
        }
    }

    if (drawnThisFrame)
    {
        timeInactive = 0;

        // Finish the geometry
        if (startedLineDraw)
        {
            lineMesh->surface_end();
            startedLineDraw = false;
        }

        if (startedTriangleDraw)
        {
            triangleMesh->surface_end();
            startedTriangleDraw = false;
        }

        lineDrawer->set_visible(true);
        triangleDrawer->set_visible(true);
        drawnThisFrame = false;

        // Send camera position to the debug draw for LOD purposes
        const auto* camera = get_viewport()->get_camera_3d();

        if (camera != nullptr)
        {
            SetDebugCameraLocation(camera->get_global_position());
        }

        if (!warnedAboutHittingMemoryLimit && usedDrawMemory + SingleTriangleDrawMemoryUse * 100 >= drawMemoryLimit)
        {
            warnedAboutHittingMemoryLimit = true;

            // Put some extra buffer in the memory advice
            extraNeededDrawMemory += SingleTriangleDrawMemoryUse * 100;

            ERR_PRINT("Debug drawer hit immediate geometry memory limit (extra needed memory: " +
                godot::String::num(static_cast<int64_t>(extraNeededDrawMemory / 1024)) +
                " KiB), some things were not rendered (this message won't repeat even if the problem occurs again)");
        }

        // This needs to reset here so that StartDrawingIfNotYetThisFrame gets called again
        usedDrawMemory = 0;
        return;
    }

    timeInactive += delta;

    if (currentPhysicsDebugLevel < 1 || timeInactive > HideAfterInactiveFor)
    {
        lineDrawer->set_visible(false);
        triangleDrawer->set_visible(false);
    }
}

// ------------------------------------ //
void DebugDrawer::IncrementPhysicsDebugLevel() noexcept
{
    // C# side prints a warning so we don't do one here
    if (!physicsDebugSupported)
        return;

    currentPhysicsDebugLevel = (currentPhysicsDebugLevel + 1) % MAX_DEBUG_DRAW_LEVEL;

    emit_signal(SignalNameOnPhysicsDebugLevelChanged, currentPhysicsDebugLevel);

    godot::UtilityFunctions::print("Setting physics debug level to: ", currentPhysicsDebugLevel);
}

void DebugDrawer::EnablePhysicsDebug() noexcept
{
    if (currentPhysicsDebugLevel == 0)
        IncrementPhysicsDebugLevel();
}

void DebugDrawer::DisablePhysicsDebug() noexcept
{
    if (currentPhysicsDebugLevel == 0)
        return;

    currentPhysicsDebugLevel = 0;

    godot::UtilityFunctions::print("Disabling physics debug");

    emit_signal(SignalNameOnPhysicsDebugLevelChanged, currentPhysicsDebugLevel);
}

// ------------------------------------ //
void DebugDrawer::OnReceiveLines(
    const std::vector<std::tuple<JPH::RVec3Arg, JPH::RVec3Arg, JPH::Float4>>& lineBuffer) noexcept
{
    for (const auto& entry : lineBuffer)
    {
        DrawLine(JoltToGodot(std::get<0>(entry)), JoltToGodot(std::get<1>(entry)), JoltToGodot(std::get<2>(entry)));
    }
}

void DebugDrawer::OnReceiveTriangles(
    const std::vector<std::tuple<JPH::RVec3Arg, JPH::RVec3Arg, JPH::RVec3Arg, JPH::Float4>>& triangleBuffer) noexcept
{
    for (const auto& entry : triangleBuffer)
    {
        DrawTriangle(JoltToGodot(std::get<0>(entry)), JoltToGodot(std::get<1>(entry)), JoltToGodot(std::get<2>(entry)),
            JoltToGodot(std::get<3>(entry)));
    }
}

bool DebugDrawer::RegisterDebugDraw() noexcept
{
    auto* config = ThriveConfig::Instance();

    if (!config)
    {
        ERR_PRINT("ThriveConfig is inaccessible");
        return false;
    }

    physicsDebugSupported = config->IsDebugDrawSupported();

    if (physicsDebugSupported)
    {
        if (lineDrawer == nullptr)
            ERR_PRINT("DebugDrawer not initialized but told to register debug draw (this will crash soon)");

        config->RegisterDebugDrawReceiver(this);
    }

    return physicsDebugSupported;
}

// ------------------------------------ //
// Drawing methods
void DebugDrawer::DrawLine(const godot::Vector3& from, const godot::Vector3& to, const godot::Color& colour)
{
    if (usedDrawMemory + SingleLineDrawMemoryUse >= drawMemoryLimit)
    {
        extraNeededDrawMemory += SingleLineDrawMemoryUse;
        return;
    }

    StartDrawingIfNotYetThisFrame();

    if (!startedLineDraw)
    {
        lineMesh->clear_surfaces();
        lineMesh->surface_begin(godot::Mesh::PRIMITIVE_LINES, lineMaterial);
        startedLineDraw = true;
    }

    lineMesh->surface_add_vertex(from);
    lineMesh->surface_set_color(colour);
    lineMesh->surface_add_vertex(to);
    lineMesh->surface_set_color(colour);

    usedDrawMemory += SingleLineDrawMemoryUse;
}

void DebugDrawer::DrawTriangle(const godot::Vector3& vertex1, const godot::Vector3& vertex2,
    const godot::Vector3& vertex3, const godot::Color& colour)
{
    if (usedDrawMemory + SingleTriangleDrawMemoryUse >= drawMemoryLimit)
    {
        extraNeededDrawMemory += SingleLineDrawMemoryUse;
        return;
    }

    StartDrawingIfNotYetThisFrame();

    if (!startedTriangleDraw)
    {
        triangleMesh->clear_surfaces();
        triangleMesh->surface_begin(godot::Mesh::PRIMITIVE_TRIANGLES, triangleMaterial);
        startedTriangleDraw = true;
    }

    triangleMesh->surface_add_vertex(vertex1);
    triangleMesh->surface_set_color(colour);

    triangleMesh->surface_add_vertex(vertex2);
    triangleMesh->surface_set_color(colour);

    triangleMesh->surface_add_vertex(vertex3);
    triangleMesh->surface_set_color(colour);

    usedDrawMemory += SingleTriangleDrawMemoryUse;
}

void DebugDrawer::StartDrawingIfNotYetThisFrame()
{
    if (drawnThisFrame)
        return;

    usedDrawMemory = 0;
    extraNeededDrawMemory = 0;

    drawnThisFrame = true;
}

// ------------------------------------ //
// TimedLine handling
void DebugDrawer::HandleTimedLines(float delta)
{
    auto count = timedLines.size();
    for (size_t i = 0; i < count; ++i)
    {
        auto& line = timedLines[i];

        line.TimePassed += delta;

        const auto fraction = line.TimePassed / LineLifeTime;

        if (fraction >= 1)
        {
            // Line time ended
            // Copy last element here to effectively erase the item at current index without keeping order
            timedLines[i] = timedLines[count - 1];
            --i;
            --count;
            timedLines.pop_back();
            continue;
        }

        const auto endColour = godot::Color(line.Colour, 0);

        const auto colour = line.Colour.lerp(endColour, fraction);

        DrawLine(line.From, line.To, colour);
    }
}

void DebugDrawer::OnlyElapseLineTime(float delta)
{
    for (auto& line : timedLines)
    {
        line.TimePassed += delta;
    }
}

} // namespace Thrive
