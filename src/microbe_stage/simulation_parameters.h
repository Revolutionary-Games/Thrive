#pragma once

#include "general/json_registry.h"
#include "microbe_stage/compounds.h"
#include "microbe_stage/bioprocesses.h"
#include "microbe_stage/biomes.h"
#include "microbe_stage/organelle_types.h"
#include "microbe_stage/species.h"
#include "microbe_stage/bacteria_types.h"

#include <unordered_map>

namespace thrive {

class BioProcess;
class Biome;

class SimulationParameters {
public:
	static TJsonRegistry<Compound> compoundRegistry;
	static TJsonRegistry<BioProcess> bioProcessRegistry;
	static TJsonRegistry<Biome> biomeRegistry;
	static TJsonRegistry<OrganelleType> organelleRegistry;
	static TJsonRegistry<Species> speciesRegistry;
	static TJsonRegistry<BacteriaType> bacteriaRegistry;
	static std::unordered_map<size_t, unsigned int> newSpeciesStartingCompounds;

	static void init();
};

}
