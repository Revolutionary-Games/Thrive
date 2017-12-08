#pragma once

#include "general/json_registry.h"

#include <map>
#include <string>

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
	std::map<size_t, BiomeCompoundData> compounds;
	std::string background = "error";

	Biome();

	Biome(Json::Value value);
};

}
