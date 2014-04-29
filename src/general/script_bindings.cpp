#include "general/script_bindings.h"

#include "scripting/luabind.h"
#include "general/timed_life_system.h"
#include "general/unlocking_system.h"
#include "general/powerup_system.h"

luabind::scope
thrive::GeneralBindings::luaBindings() {
    return (
        // Components
        TimedLifeComponent::luaBindings(),
        LockedMapComponent::luaBindings(),
        PowerupComponent::luaBindings(),
        // Systems
        TimedLifeSystem::luaBindings(),
        PowerupSystem::luaBindings()
        // Other
    );
}


