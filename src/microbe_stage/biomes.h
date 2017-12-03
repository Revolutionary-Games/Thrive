#pragma once

#include "general/json_registry.h"
#include "microbe_stage/compounds.h"
#include "microbe_stage/simulation_parameters.h"

#include <unordered_map>
#include <string>
#include <vector>

namespace thrive {

struct BiomeCompoundData {
public:
	unsigned int amount = 0;
	double density = 1.0;

	BiomeCompoundData(unsigned int amount, double density):
		amount(amount),
		density(density)
	{}
};

class SimulationParameters;

class Biome : public RegistryType {
public:
	std::unordered_map<unsigned int, BiomeCompoundData> compounds;
	std::string background = "error";

	Biome();

	Biome(Json::Value value);
};

}
