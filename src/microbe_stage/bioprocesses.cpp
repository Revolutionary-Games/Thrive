#pragma once

#include "microbe_stage/compounds.h"
#include "microbe_stage/simulation_parameters.h"
#include "bioprocesses.h"

using namespace thrive;

BioProcess::BioProcess() {}

BioProcess::BioProcess(Json::Value value) {
	// Getting the input compounds.
	std::vector<std::string> compoundInternalNames = value["inputs"].getMemberNames();
	for (std::string compoundInternalName : compoundInternalNames) {
		unsigned int amount = value["inputs"][compoundInternalName].asUInt();

		// Getting the compound id from the compound registry.
		unsigned int id = SimulationParameters::compoundRegistry.getTypeData(compoundInternalName).id;

		inputs.emplace(id, amount);
	}

	// Getting the output compounds.
	compoundInternalNames = value["outputs"].getMemberNames();
	for (std::string compoundInternalName : compoundInternalNames) {
		unsigned int amount = value["outputs"][compoundInternalName].asUInt();

		// Getting the compound id from the compound registry.
		unsigned int id = SimulationParameters::compoundRegistry.getTypeData(compoundInternalName).id;

		outputs.emplace(id, amount);
	}
}
