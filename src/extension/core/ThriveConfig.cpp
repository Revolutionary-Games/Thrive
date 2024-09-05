// ------------------------------------ //
#include "ThriveConfig.hpp"

// ------------------------------------ //
namespace Thrive
{

ThriveConfig::ThriveConfig()
{
}

ThriveConfig::~ThriveConfig()
{
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
bool ThriveConfig::ReportOtherVersions(int csharpVersion, int nativeLibraryVersion)
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

} // namespace Thrive
