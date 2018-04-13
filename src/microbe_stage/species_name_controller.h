#pragma once

#include <string>
#include <vector>

namespace thrive {

class SpeciesNameController {
public:
	std::vector<std::string> prefixes;
	std::vector<std::string> cofixes;
	std::vector<std::string> suffixes;

	SpeciesNameController();

	SpeciesNameController(std::string jsonFilePath);
};

}