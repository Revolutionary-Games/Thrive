#pragma once

#include "general/json_registry.h"

#include <map>
#include <vector>
#include <set>
#include <string>
#include <Common/Types.h>

namespace thrive {

class SimulationParameters;

class OrganelleType : public RegistryType {
public:
	double mass = 1000.0; // A large value so we notice if i screwed up. :D
	char gene = '@';
	unsigned int mpCost = 0;
	bool isLocked = true;
	std::string mesh = "default_organelle.mesh"; // TODO: have a default mesh so we can test organelles.
	std::set<Int2> hexes; // The sets occupied by this organnelle in the microbe.
	std::map<size_t, unsigned int> composition;
	std::vector<unsigned int> components; // This have the functionality of each organelle.

	OrganelleType();

	OrganelleType(Json::Value value);
};

}
