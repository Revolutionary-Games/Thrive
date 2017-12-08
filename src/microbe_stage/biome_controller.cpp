#include "biome_controller.h"
#include "simulation_parameters.h"
#include "biomes.h"

#include <Utility/Random.h>

using namespace thrive;

BiomeController::BiomeController() {
	setRandomBiome();
}

void BiomeController::setBiome(size_t id) {
	currentBiomeId = id;
}

void BiomeController::setBiome(std::string internalBiomeName) {
	// Kinda inefficient but eh, you shouldn't use strings for efficiency in the first place!
	size_t id = SimulationParameters::biomeRegistry.getTypeData(internalBiomeName).id;
	setBiome(id);
}

void BiomeController::setRandomBiome() {
	size_t numberOfBiomes = SimulationParameters::biomeRegistry.getSize();
	LEVIATHAN_ASSERT(numberOfBiomes > 0, "There are no biomes loaded in the registry");
	size_t randomId = Leviathan::Random::Get()->GetNumber(0, numberOfBiomes);
	setBiome(randomId);
}

size_t BiomeController::getCurrentBiome() {
	return currentBiomeId;
}
