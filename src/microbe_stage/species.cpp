#include "microbe_stage/compounds.h"
#include "microbe_stage/organelle_types.h"
#include "microbe_stage/simulation_parameters.h"
#include "species.h"

using namespace thrive;

Species::Species() {}

Species::Species(Json::Value value) {
	spawnDensity = value["spawnDensity"].asDouble();

	// Setting the cloud colour.
	float r = value["colour"]["r"].asFloat();
	float g = value["colour"]["g"].asFloat();
	float b = value["colour"]["b"].asFloat();
	colour = Ogre::ColourValue(r, g, b, 1.0);

	// Getting the starting compounds.
	std::vector<std::string> compoundInternalNames = value["startingCompounds"].getMemberNames();
	for (std::string compoundInternalName : compoundInternalNames) {
		unsigned int amount = value["startingCompounds"][compoundInternalName].asUInt();

		// Getting the compound id from the compound registry.
		size_t id = SimulationParameters::compoundRegistry.getTypeData(compoundInternalName).id;

		startingCompounds.emplace(id, amount);
	}

	// Getting the starting organelles.
	// TODO
}
