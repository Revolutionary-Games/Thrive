#pragma once

#include "general/json_registry.h"
#include "microbe_stage/compounds.h"
#include "microbe_stage/bioprocesses.h"
#include "microbe_stage/biomes.h"

namespace thrive {

class BioProcess;
class Biome;

class SimulationParameters {
public:
	static TJsonRegistry<Compound> compoundRegistry;
	static TJsonRegistry<BioProcess> bioProcessRegistry;
	static TJsonRegistry<Biome> biomeRegistry;
};

}
