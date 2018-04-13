#pragma once

#include "general/json_registry.h"
#include "compounds.h"
#include "bioprocesses.h"
#include "biomes.h"
#include "organelle_types.h"
#include "species.h"
#include "bacteria_types.h"
#include "species_name_controller.h"

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

	static SpeciesNameController speciesNameController;

	static std::unordered_map<size_t, unsigned int> newSpeciesStartingCompounds;

	static void init();
};

}
