// ------------------------------------ //
#include "ThriveConfig.hpp"
#include <godot_cpp/core/class_db.hpp>
#include <godot_cpp/variant/utility_functions.hpp>

// ------------------------------------ //
namespace Thrive
{

constexpr int INIT_MAGIC = 765442;
int InitValueLocation = -1;

ThriveConfig::~ThriveConfig()
{
    if (initialized)
    {
        godot::UtilityFunctions::printerr("ThriveConfig is still initialized during destruction, Shutdown should be called first");
    }
}

void ThriveConfig::_bind_methods()
{
    godot::ClassDB::bind_method(godot::D_METHOD("ReportOtherVersions", "csharpVersion", "nativeLibraryVersion"), &ThriveConfig::ReportOtherVersions);
    godot::ClassDB::bind_method(godot::D_METHOD("Initialize", "intercommunication"), &ThriveConfig::Initialize);
    godot::ClassDB::bind_method(godot::D_METHOD("Shutdown"), &ThriveConfig::Shutdown);
}

// ------------------------------------ //
bool ThriveConfig::ReportOtherVersions(int csharpVersion, int nativeLibraryVersion) noexcept
{
    if (csharpVersion != THRIVE_EXTENSION_VERSION)
    {
        godot::UtilityFunctions::printerr("Thrive GDExtension version mismatch");
        return false;
    }

    if (nativeLibraryVersion != THRIVE_LIBRARY_VERSION)
    {
        godot::UtilityFunctions::printerr("Thrive native library version mismatch");
        return false;
    }

    return true;
}

ThriveConfig* ThriveConfig::InitializeImplementation(NativeLibIntercommunication& intercommunication) noexcept
{
    if (intercommunication.SanityCheckValue != INTEROP_MAGIC_VALUE)
    {
        godot::UtilityFunctions::printerr("Interop data passed to Thrive Extension is corrupt (unexpected magic value)");
        return nullptr;
    }

    // Init succeeded
    initialized = true;
    InitValueLocation = INIT_MAGIC;

    return this;
}

godot::Variant ThriveConfig::Initialize(const godot::Variant& intercommunication) noexcept
{
    if (intercommunication.get_type() != godot::Variant::INT)
    {
        godot::UtilityFunctions::printerr("Extension initialize expected to get an int as parameter");
        return godot::Variant(false);
    }

    const auto convertedIntercommunication =
        reinterpret_cast<NativeLibIntercommunication*>(static_cast<int64_t>(intercommunication));

    if (convertedIntercommunication == nullptr)
    {
        godot::UtilityFunctions::printerr("Extension initialize was given a null value as the intercommunication object");
        return godot::Variant(false);
    }

    return godot::Variant(reinterpret_cast<int64_t>(InitializeImplementation(*convertedIntercommunication)));
}

bool ThriveConfig::Shutdown() noexcept
{
    if (!initialized)
    {
        godot::UtilityFunctions::printerr("This config object is not initialized (shutdown called)");
        return false;
    }

    initialized = false;
    return true;
}

// ------------------------------------ //
int ThriveConfig::GetVersion() const noexcept
{
    // Detect library load conflicts between what Godot loaded and what is used through C# interop
    if (InitValueLocation != INIT_MAGIC)
    {
        godot::UtilityFunctions::printerr("Unexpected value in init data location. Has this library been loaded twice from conflicting places?");
        return -1;
    }

    return THRIVE_EXTENSION_VERSION;
}

} // namespace Thrive
