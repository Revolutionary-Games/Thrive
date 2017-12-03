#pragma once

#include "general/json_registry.h"
#include "microbe_stage/compounds.h"
#include "microbe_stage/simulation_parameters.h"

#include <unordered_map>

namespace thrive {

class SimulationParameters;

class BioProcess : public RegistryType {
public:
	// The second number is the amount of compound.
	std::unordered_map<unsigned int, unsigned int> inputs;
	std::unordered_map<unsigned int, unsigned int> outputs;

	BioProcess();

	BioProcess(Json::Value value);
};

}
