#include "microbe_stage/script_bindings.h"

#include "scripting/luabind.h"
#include "microbe_stage/agent.h"

luabind::scope
thrive::MicrobeBindings::luaBindings() {
    return (
        // Components
        AgentComponent::luaBindings(),
        AgentAbsorberComponent::luaBindings(),
        AgentEmitterComponent::luaBindings(),
        TimedAgentEmitterComponent::luaBindings(),
        // Systems
        AgentLifetimeSystem::luaBindings(),
        AgentMovementSystem::luaBindings(),
        AgentAbsorberSystem::luaBindings(),
        AgentEmitterSystem::luaBindings(),
        // Other
        AgentRegistry::luaBindings()
    );
}


