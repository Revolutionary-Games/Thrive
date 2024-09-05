#pragma once

#include <godot_cpp/classes/object.hpp>

#include "Include.h"

#include "shared/NativeLibIntercommunication.hpp"

namespace Thrive
{

/// Manages forwarding C# side configuration and other runtime data between Thrive parts into this module
class ThriveConfig : public godot::Object
{
    GDCLASS(ThriveConfig, godot::Object)
public:
    // ------------------------------------ //
    // Static access to this class to access configuration data. Invalid if this hasn't been initialized by the C#
    // code yet.

public:
    ThriveConfig();
    ~ThriveConfig() override;

    /// Checks that other running Thrive components are compatible
    bool ReportOtherVersions(int csharpVersion, int nativeLibraryVersion);

    /// Wrapper for InitializeImplementation to be called through Godot
    godot::Variant Initialize(const godot::Variant& intercommunication);

    ThriveConfig* InitializeImplementation(NativeLibIntercommunication& intercommunication);

    // ------------------------------------ //
    // C# interop methods
    int GetVersion();

protected:
    static void _bind_methods();

private:
};

} // namespace Thrive
