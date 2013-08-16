#include "microbe_stage/script_bindings.h"

#include "scripting/luabind.h"
#include "microbe_stage/agent.h"

luabind::scope
thrive::MicrobeBindings::luaBindings() {
    return (
        AgentEmitterComponent::luaBindings()
    );
}


