#pragma once

#include "general\json_registry.h"

#include <unordered_map>

struct BioProcess : public RegistryType {
public:
	std::unordered_map<unsigned int, unsigned int> inputs;
	std::unordered_map<unsigned int, unsigned int> outputs;

	BioProcess() {}

	BioProcess(Json::Value value) {
		// TODO
	}
};
