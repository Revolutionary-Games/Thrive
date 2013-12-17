#include "microbe_stage/script_bindings.h"

#include "scripting/luabind.h"
#include "microbe_stage/compound.h"

luabind::scope
thrive::MicrobeBindings::luaBindings() {
    return (
        // Components
        CompoundComponent::luaBindings(),
        CompoundAbsorberComponent::luaBindings(),
        CompoundEmitterComponent::luaBindings(),
        TimedCompoundEmitterComponent::luaBindings(),
        // Systems
        CompoundLifetimeSystem::luaBindings(),
        CompoundMovementSystem::luaBindings(),
        CompoundAbsorberSystem::luaBindings(),
        CompoundEmitterSystem::luaBindings(),
        // Other
        CompoundRegistry::luaBindings()
    );
}


