#pragma once

#include "general\json_registry.h"

#include <unordered_map>
#include <string>

struct BiomeCompoundData {
	int amount = 0;
	double density = 1.0;
};

struct Biome : public RegistryType {
public:
	std::unordered_map<unsigned int, BiomeCompoundData> compounds;
	std::string background;

	Biome() {}

	Biome(Json::Value value) {
		background = value["background"].asString();
	}
};
