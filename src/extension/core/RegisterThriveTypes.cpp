// ------------------------------------ //
#include "RegisterThriveTypes.hpp"

#include "nodes/DebugDrawer.hpp"

#include "ThriveConfig.hpp"

#include "atlas/ExtendedArrayMesh.hpp"

// ------------------------------------ //
namespace Thrive
{
void InitializeThriveModule(godot::ModuleInitializationLevel level)
{
    if (level != godot::MODULE_INITIALIZATION_LEVEL_SCENE)
    {
        return;
    }
	
	GDREGISTER_CLASS(Thrive::ExtendedArrayMesh);
    GDREGISTER_CLASS(Thrive::ThriveConfig);
    GDREGISTER_CLASS(Thrive::DebugDrawer);
}

void UnInitializeThriveModule(godot::ModuleInitializationLevel level)
{
    if (level != godot::MODULE_INITIALIZATION_LEVEL_SCENE)
    {
        return;
    }
}

} // namespace Thrive

extern "C"
{
    // Ignore diagnostic to use the exact same syntax as the Godot example
#pragma clang diagnostic push
#pragma ide diagnostic ignored "misc-misplaced-const"

    // This is how this module is initialized for use in Godot
    // GDE_EXPORT does the same as our macro API_EXPORT
    GDExtensionBool GDE_EXPORT ThriveExtensionLibraryInit(GDExtensionInterfaceGetProcAddress p_get_proc_address,
        const GDExtensionClassLibraryPtr p_library, GDExtensionInitialization* r_initialization)
    {
        godot::GDExtensionBinding::InitObject init_obj(p_get_proc_address, p_library, r_initialization);

        init_obj.register_initializer(Thrive::InitializeThriveModule);
        init_obj.register_terminator(Thrive::UnInitializeThriveModule);
        init_obj.set_minimum_library_initialization_level(godot::MODULE_INITIALIZATION_LEVEL_SCENE);

        return init_obj.init();
    }

#pragma clang diagnostic pop
}
