#include "microbe_stage/script_bindings.h"

#include "scripting/luabind.h"
#include "microbe_stage/compound.h"
#include "microbe_stage/compound_absorber_system.h"
#include "microbe_stage/compound_emitter_system.h"
#include "microbe_stage/compound_registry.h"
#include "microbe_stage/bio_process_registry.h"
#include "microbe_stage/membrane_system.h"
#include "microbe_stage/compound_cloud_system.h"
#include "microbe_stage/agent_cloud_system.h"

luabind::scope
thrive::MicrobeBindings::luaBindings() {
    return (
        // Components
        CompoundComponent::luaBindings(),
        CompoundAbsorberComponent::luaBindings(),
        CompoundEmitterComponent::luaBindings(),
        TimedCompoundEmitterComponent::luaBindings(),
        MembraneComponent::luaBindings(),
        CompoundCloudComponent::luaBindings(),
        AgentCloudComponent::luaBindings(),
        // Systems
        CompoundMovementSystem::luaBindings(),
        CompoundAbsorberSystem::luaBindings(),
        CompoundEmitterSystem::luaBindings(),
        MembraneSystem::luaBindings(),
        CompoundCloudSystem::luaBindings(),
        AgentCloudSystem::luaBindings(),
        // Other
        CompoundRegistry::luaBindings(),
        BioProcessRegistry::luaBindings()
    );
}
