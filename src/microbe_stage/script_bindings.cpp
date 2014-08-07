#include "microbe_stage/script_bindings.h"

#include "scripting/luabind.h"
#include "microbe_stage/compound.h"
#include "microbe_stage/compound_absorber_system.h"
#include "microbe_stage/compound_emitter_system.h"
#include "microbe_stage/compound_registry.h"
#include "microbe_stage/bio_process_registry.h"
#include "microbe_stage/species_registry.h"

luabind::scope
thrive::MicrobeBindings::luaBindings() {
    return (
        // Components
        CompoundComponent::luaBindings(),
        CompoundAbsorberComponent::luaBindings(),
        CompoundEmitterComponent::luaBindings(),
        TimedCompoundEmitterComponent::luaBindings(),
        // Systems
        CompoundMovementSystem::luaBindings(),
        CompoundAbsorberSystem::luaBindings(),
        CompoundEmitterSystem::luaBindings(),
        // Other
        CompoundRegistry::luaBindings(),
        BioProcessRegistry::luaBindings(),
        SpeciesRegistry::luaBindings()
    );
}


