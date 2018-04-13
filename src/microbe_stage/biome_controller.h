#pragma once

#include <string>

namespace thrive {

class BiomeController {
public:
	size_t currentBiomeId = 0;

	BiomeController();

	// Changes the current biome to the specified one.
	void setBiome(size_t id);
	void setBiome(std::string internalBiomeName);

	// Changes the current biome to a random one.
	void setRandomBiome();

	// Returns the id of the current biome.
	size_t getCurrentBiome();
};

}
