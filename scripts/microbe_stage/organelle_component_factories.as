#include "organelle_component.as"

// Include all the used organelle types for the factory functions
#include "organelle_components/nucleus_organelle.as"
#include "organelle_components/storage_organelle.as"
#include "organelle_components/processor_organelle.as"
#include "organelle_components/agent_vacuole.as"
#include "organelle_components/movement_organelle.as"
#include "organelle_components/pilus.as"

// ------------------------------------ //
// Factory functions for all the organelle components

OrganelleComponentType@ organelleFactory_nucleus()
{
    return NucleusOrganelle();
}

OrganelleComponentType@ organelleFactory_storage(float capacity)
{
    return StorageOrganelle(capacity);
}

OrganelleComponentType@ organelleFactory_processor()
{
    return ProcessorOrganelle();
}

OrganelleComponentType@ organelleFactory_agentVacuole(const string &in compound,
    const string &in process)
{
    return AgentVacuole(compound, process);
}

OrganelleComponentType@ organelleFactory_movement(float momentum, float torque)
{
    return MovementOrganelle(momentum, torque);
}

OrganelleComponentType@ organelleFactory_pilus()
{
    return Pilus();
}
