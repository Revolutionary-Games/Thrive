// ------------------------------------ //
#include "ThriveConfig.hpp"

// ------------------------------------ //
namespace Thrive
{

constexpr int INIT_MAGIC = 765442;

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
        ERR_PRINT("This Thrive GDExtension version was compiled against Thrive native version " +
            godot::String::num_int64(THRIVE_LIBRARY_VERSION) +
            " but it is now tried to be used with version: " + godot::String::num_int64(nativeLibraryVersion));
        return false;
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

    if (false)
    {
        ERR_PRINT("ThriveConfig object initialization failed");
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
        ERR_PRINT("Extension initialize expected to get an int as parameter");
        return {false};
    }

    const auto convertedIntercommunication =
        reinterpret_cast<NativeLibIntercommunication*>((int64_t)intercommunication);

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

    initialized = false;
    return true;
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
