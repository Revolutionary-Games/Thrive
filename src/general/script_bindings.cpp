#include "general/script_bindings.h"

#include "scripting/luabind.h"
#include "general/timed_life_system.h"

luabind::scope
thrive::GeneralBindings::luaBindings() {
    return (
        // Components
        TimedLifeComponent::luaBindings(),
        // Systems
        TimedLifeSystem::luaBindings()
        // Other
    );
}


