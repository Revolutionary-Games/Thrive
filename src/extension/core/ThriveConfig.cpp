// ------------------------------------ //
#include "ThriveConfig.hpp"

#include <godot_cpp/core/class_db.hpp>
#include <godot_cpp/variant/utility_functions.hpp>

#include "nodes/DebugDrawer.hpp"
#include "physics/DebugDrawForwarder.hpp"

// ------------------------------------ //
namespace Thrive
{

constexpr int INIT_MAGIC = 765442;

ThriveConfig* ThriveConfig::instance = nullptr;

DebugDrawer* activeDrawerInstance = nullptr;

void ForwardLines(const std::vector<std::tuple<JPH::RVec3Arg, JPH::RVec3Arg, JPH::Float4>>& lineBuffer) noexcept
{
    if (activeDrawerInstance == nullptr)
        return;

    activeDrawerInstance->OnReceiveLines(lineBuffer);
}

void ForwardTriangles(
    const std::vector<std::tuple<JPH::RVec3Arg, JPH::RVec3Arg, JPH::RVec3Arg, JPH::Float4>>& triangleBuffer) noexcept
{
    if (activeDrawerInstance == nullptr)
        return;

    activeDrawerInstance->OnReceiveTriangles(triangleBuffer);
}

int InitValueLocation = -1;

ThriveConfig::~ThriveConfig()
{
    if (initialized)
    {
        ERR_PRINT("ThriveConfig is still initialized during destruction, Shutdown should be called first");
    }
}

void ThriveConfig::_bind_methods()
{
    using namespace godot;
    ClassDB::bind_method(
        D_METHOD("ReportOtherVersions", "csharpVersion", "nativeLibraryVersion"), &ThriveConfig::ReportOtherVersions);
    ClassDB::bind_method(D_METHOD("Initialize", "intercommunication"), &ThriveConfig::Initialize);
    ClassDB::bind_method(D_METHOD("Shutdown"), &ThriveConfig::Shutdown);
}

// ------------------------------------ //
bool ThriveConfig::ReportOtherVersions(int csharpVersion, int nativeLibraryVersion) noexcept
{
    if (csharpVersion != THRIVE_EXTENSION_VERSION)
    {
        ERR_PRINT("Thrive GDExtension version (" + godot::String::num_int64(THRIVE_EXTENSION_VERSION) +
            ") doesn't match what the C# side version is: " + godot::String::num_int64(csharpVersion));
        return false;
    }

    if (nativeLibraryVersion != THRIVE_LIBRARY_VERSION)
    {
        // We'll try to be forward compatible, so just check if the native library is too old
        if (nativeLibraryVersion < THRIVE_LIBRARY_VERSION)
        {
            ERR_PRINT("This Thrive GDExtension version was compiled against Thrive native version " +
                godot::String::num_int64(THRIVE_LIBRARY_VERSION) +
                " but it is now tried to be used with version: " + godot::String::num_int64(nativeLibraryVersion));
            return false;
        }
    }

    return true;
}

ThriveConfig* ThriveConfig::InitializeImplementation(NativeLibIntercommunication& intercommunication) noexcept
{
    if (intercommunication.SanityCheckValue != INTEROP_MAGIC_VALUE)
    {
        ERR_PRINT("Interop data passed to Thrive Extension is corrupt (unexpected magic value)");
        return nullptr;
    }

    // This is kept for when there's more complex initialization
    /*if (false)
    {
        ERR_PRINT("ThriveConfig object initialization failed");
        return nullptr;
    }*/

    // Init succeeded
    initialized = true;
    InitValueLocation = INIT_MAGIC;
    instance = this;

    // Store for later accessing
    storedIntercommunication = &intercommunication;

    godot::UtilityFunctions::print("Thrive GDExtension initialized successfully");
    return this;
}

godot::Variant ThriveConfig::Initialize(const godot::Variant& intercommunication) noexcept
{
    if (intercommunication.get_type() != godot::Variant::INT)
    {
        ERR_PRINT("Extension initialize expected to get an int as parameter");
        return {false};
    }

    const auto convertedIntercommunication =
        reinterpret_cast<NativeLibIntercommunication*>(static_cast<int64_t>(intercommunication));

    if (convertedIntercommunication == nullptr)
    {
        ERR_PRINT("Extension initialize was given a null value as the intercommunication object");
        return {false};
    }

    return {reinterpret_cast<int64_t>(InitializeImplementation(*convertedIntercommunication))};
}

bool ThriveConfig::Shutdown() noexcept
{
    if (!initialized)
    {
        ERR_PRINT("This config object is not initialized (shutdown called)");
        return false;
    }

    instance = nullptr;
    initialized = false;
    return true;
}

// ------------------------------------ //

bool ThriveConfig::IsDebugDrawSupported() const noexcept
{
    if (!storedIntercommunication)
    {
        ERR_PRINT("ThriveConfig not initialized (missing intercommunication)");
        return false;
    }

    return storedIntercommunication->PhysicsDebugSupported;
}

void ThriveConfig::RegisterDebugDrawReceiver(DebugDrawer* drawer) noexcept
{
    if (!storedIntercommunication)
    {
        ERR_PRINT("ThriveConfig not initialized (missing intercommunication)");
        return;
    }

    if (drawer == nullptr)
    {
        storedIntercommunication->DebugLineReceiver = nullptr;
        storedIntercommunication->DebugTriangleReceiver = nullptr;
        activeDrawerInstance = nullptr;
        return;
    }

    activeDrawerInstance = drawer;
    storedIntercommunication->DebugLineReceiver = ForwardLines;
    storedIntercommunication->DebugTriangleReceiver = ForwardTriangles;
}

// ------------------------------------ //
int ThriveConfig::GetVersion() const noexcept
{
    // Detect library load conflicts between what Godot loaded and what is used through C# interop
    if (InitValueLocation != INIT_MAGIC)
    {
        ERR_PRINT(
            "Unexpected value in init data location. Has this library been loaded twice from conflicting places?");
        return -1;
    }

    return THRIVE_EXTENSION_VERSION;
}

} // namespace Thrive
