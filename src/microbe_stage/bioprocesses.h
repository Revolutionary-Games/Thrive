#pragma once

#include "general/json_registry.h"

#include <map>

namespace thrive {

class SimulationParameters;

class BioProcess : public RegistryType {
public:
	// The second number is the amount of compound.
	std::map<unsigned int, unsigned int> inputs;
	std::map<unsigned int, unsigned int> outputs;

	BioProcess();

	BioProcess(Json::Value value);
};

}
