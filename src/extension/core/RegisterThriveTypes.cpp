#include "RegisterThriveTypes.hpp"

#include "nodes/DebugDrawer.hpp"
#include "nodes/CompoundCloudPlane.hpp"
#include "ThriveConfig.hpp"

#include <godot_cpp/core/class_db.hpp>
#include <godot_cpp/core/defs.hpp>
#include <godot_cpp/godot.hpp>

namespace Thrive {

void InitializeThriveModule(godot::ModuleInitializationLevel level) {
    if (level != godot::MODULE_INITIALIZATION_LEVEL_SCENE) {
        return;
    }

    GDREGISTER_CLASS(Thrive::ThriveConfig);
    GDREGISTER_CLASS(Thrive::DebugDrawer);
    GDREGISTER_CLASS(Thrive::CompoundCloudPlane);
}

void UnInitializeThriveModule(godot::ModuleInitializationLevel level) {
    if (level != godot::MODULE_INITIALIZATION_LEVEL_SCENE) {
        return;
    }
}

} // namespace Thrive

extern "C" {
GDExtensionBool GDE_EXPORT ThriveExtensionLibraryInit(GDExtensionInterfaceGetProcAddress p_get_proc_address,
    const GDExtensionClassLibraryPtr p_library, GDExtensionInitialization* r_initialization) {
    godot::GDExtensionBinding::InitObject init_obj(p_get_proc_address, p_library, r_initialization);

    init_obj.register_initializer(Thrive::InitializeThriveModule);
    init_obj.register_terminator(Thrive::UnInitializeThriveModule);
    init_obj.set_minimum_library_initialization_level(godot::MODULE_INITIALIZATION_LEVEL_SCENE);

    return init_obj.init();
}
}
