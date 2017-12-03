#pragma once

#include "general/json_registry.h"
#include "microbe_stage/compounds.h"
#include "microbe_stage/simulation_parameters.h"
#include "biomes.h"

#include <unordered_map>
#include <string>
#include <vector>

using namespace thrive;

Biome::Biome() {}

Biome::Biome(Json::Value value) {
	background = value["background"].asString();

	// Getting the compound information.
	Json::Value compoundData = value["compounds"];
	std::vector<std::string> compoundInternalNames = compoundData.getMemberNames();

	for (std::string compoundInternalName : compoundInternalNames) {
		unsigned int amount = compoundData[compoundInternalName]["amount"].asUInt();
		double density = compoundData[compoundInternalName]["density"].asDouble();


		// Getting the compound id from the compound registry.
		unsigned int id = SimulationParameters::compoundRegistry.getTypeData(compoundInternalName).id;

		compounds.emplace(std::piecewise_construct,
			std::forward_as_tuple(id),
			std::forward_as_tuple(amount, density));
	}
}
