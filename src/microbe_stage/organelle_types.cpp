#include "microbe_stage/compounds.h"
#include "microbe_stage/simulation_parameters.h"
#include "organelle_types.h"

using namespace thrive;

OrganelleType::OrganelleType() {}

OrganelleType::OrganelleType(Json::Value value) {
	mass = value["mass"].asDouble();
	gene = value["gene"].asString()[0];
	mpCost = value["mpCost"].asUInt();
	mesh = value["mesh"].asString();
	isLocked = value["locked"].asBool();

	// Getting the organelle composition.
	std::vector<std::string> compoundInternalNames = value["composition"].getMemberNames();
	for (std::string compoundInternalName : compoundInternalNames) {
		unsigned int amount = value["composition"][compoundInternalName].asUInt();

		// Getting the compound id from the compound registry.
		unsigned int id = SimulationParameters::compoundRegistry.getTypeData(compoundInternalName).id;

		composition.emplace(id, amount);
	}
}
