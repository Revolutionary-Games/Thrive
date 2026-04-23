// ------------------------------------ //
#include "DebugDrawer.hpp"

#include <cstdint>

BEGIN_GODOT_INCLUDES;
#include <godot_cpp/classes/camera3d.hpp>
#include <godot_cpp/classes/engine.hpp>
#include <godot_cpp/classes/immediate_mesh.hpp>
#include <godot_cpp/classes/mesh_instance3d.hpp>
#include <godot_cpp/classes/resource_loader.hpp>
#include <godot_cpp/classes/viewport.hpp>
#include <godot_cpp/variant/utility_functions.hpp>
END_GODOT_INCLUDES;

#include "core/GodotConversions.hpp"
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
    ClassDB::bind_method(D_METHOD("remove_debug_draw"), &DebugDrawer::RemoveDebugDraw);
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

    lineMesh = godot::Ref<godot::ImmediateMesh>(memnew(godot::ImmediateMesh));
    triangleMesh = godot::Ref<godot::ImmediateMesh>(memnew(godot::ImmediateMesh));

    lineDrawer->set_mesh(lineMesh);
    lineDrawer->set_visible(false);
    lineDrawer->set_ignore_occlusion_culling(true);
    lineDrawer->set_extra_cull_margin(1000);

    triangleDrawer->set_mesh(triangleMesh);
    triangleDrawer->set_visible(false);
    triangleDrawer->set_ignore_occlusion_culling(true);
    triangleDrawer->set_extra_cull_margin(1000);

    // Set an initial AABB as we might not have the camera yet
    UpdateDrawAabb({});

    // TODO: implement debug text drawing (this is a Control to support that in the future)
}

// ------------------------------------ //
void DebugDrawer::_process(double delta)
{
    // Don't do anything if not initialized
    if (lineDrawer == nullptr)
        return;

    if (physicsDebugSupported && currentPhysicsDebugLevel > 0)
    {
        // Update camera before drawing / even when not drawing anything to ensure culling works
        UpdateDebugCameraLocation();
    }

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

        if (!warnedAboutHittingMemoryLimit && usedDrawMemory + SingleTriangleDrawMemoryUse * 100 >= drawMemoryLimit)
        {
            warnedAboutHittingMemoryLimit = true;

            // Put some extra buffer in the memory advice
            extraNeededDrawMemory += SingleTriangleDrawMemoryUse * 100;

            ERR_PRINT("Debug drawer hit immediate geometry memory limit (extra needed memory: " +
                godot::String::num_int64(static_cast<int64_t>(extraNeededDrawMemory / 1024)) +
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
void DebugDrawer::OnReceiveLines(const std::vector<std::tuple<JVec3, JVec3, JColour>>& lineBuffer) noexcept
{
    for (const auto& entry : lineBuffer)
    {
        DrawLine(JToGodot(std::get<0>(entry)), JToGodot(std::get<1>(entry)), JToGodot(std::get<2>(entry)));
    }
}

void DebugDrawer::OnReceiveTriangles(
    const std::vector<std::tuple<JVec3, JVec3, JVec3, JColour>>& triangleBuffer) noexcept
{
    for (const auto& entry : triangleBuffer)
    {
        DrawTriangle(JToGodot(std::get<0>(entry)), JToGodot(std::get<1>(entry)), JToGodot(std::get<2>(entry)),
            JToGodot(std::get<3>(entry)));
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

void DebugDrawer::RemoveDebugDraw() noexcept
{
    auto* config = ThriveConfig::Instance();

    if (!config)
        return;

    physicsDebugSupported = config->IsDebugDrawSupported();

    if (physicsDebugSupported)
    {
        config->RegisterDebugDrawReceiver(nullptr);
    }
}

// ------------------------------------ //
// Drawing methods
void DebugDrawer::DrawLine(const godot::Vector3& from, const godot::Vector3& to, const godot::Color& colour)
{
    if (usedLineDrawMemory + SingleLineDrawMemoryUse >= perTypeDrawLimit)
    {
        // Needs double the actual limit raise because each type gets just half of the total
        extraNeededDrawMemory += SingleLineDrawMemoryUse * 2;
        return;
    }

    StartDrawingIfNotYetThisFrame();

    if (!startedLineDraw)
    {
        lineMesh->clear_surfaces();
        lineMesh->surface_begin(godot::Mesh::PRIMITIVE_LINES, lineMaterial);
        startedLineDraw = true;
    }

    lineMesh->surface_set_color(colour);
    lineMesh->surface_add_vertex(from);
    lineMesh->surface_set_color(colour);
    lineMesh->surface_add_vertex(to);

    usedDrawMemory += SingleLineDrawMemoryUse;
    usedLineDrawMemory += SingleLineDrawMemoryUse;
}

void DebugDrawer::DrawTriangle(const godot::Vector3& vertex1, const godot::Vector3& vertex2,
    const godot::Vector3& vertex3, const godot::Color& colour)
{
    if (usedTriangleDrawMemory + SingleTriangleDrawMemoryUse >= perTypeDrawLimit)
    {
        extraNeededDrawMemory += SingleTriangleDrawMemoryUse * 2;
        return;
    }

    StartDrawingIfNotYetThisFrame();

    if (!startedTriangleDraw)
    {
        triangleMesh->clear_surfaces();
        triangleMesh->surface_begin(godot::Mesh::PRIMITIVE_TRIANGLES, triangleMaterial);
        startedTriangleDraw = true;
    }

    triangleMesh->surface_set_color(colour);
    triangleMesh->surface_add_vertex(vertex1);

    triangleMesh->surface_set_color(colour);
    triangleMesh->surface_add_vertex(vertex2);

    triangleMesh->surface_set_color(colour);
    triangleMesh->surface_add_vertex(vertex3);

    usedDrawMemory += SingleTriangleDrawMemoryUse;
    usedTriangleDrawMemory += SingleTriangleDrawMemoryUse;
}

void DebugDrawer::StartDrawingIfNotYetThisFrame()
{
    if (drawnThisFrame)
        return;

    usedDrawMemory = 0;
    usedLineDrawMemory = 0;
    usedTriangleDrawMemory = 0;
    extraNeededDrawMemory = 0;

    drawnThisFrame = true;
}

void DebugDrawer::UpdateDrawAabb(const godot::Vector3& center)
{
    const auto radius =
        godot::Vector3{DEBUG_DRAW_MAX_DISTANCE, DEBUG_DRAW_MAX_DISTANCE, DEBUG_DRAW_MAX_DISTANCE};
    const auto bounds = godot::AABB(center - radius, radius * 2.0f);

    lineDrawer->set_custom_aabb(bounds);
    triangleDrawer->set_custom_aabb(bounds);
}

void DebugDrawer::UpdateDebugCameraLocation()
{
    // The physics debug culling depends on this position even before a successful draw happens.
    const auto* camera = get_viewport()->get_camera_3d();

    if (camera != nullptr)
    {
        const auto cameraLocation = camera->get_global_position();

        SetDebugCameraLocation(cameraLocation);
        UpdateDrawAabb(cameraLocation);
    }
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
