#include "microbe_stage/simulation_parameters.h"

using namespace thrive;

TJsonRegistry<Compound> SimulationParameters::compoundRegistry;
TJsonRegistry<BioProcess> SimulationParameters::bioProcessRegistry;
TJsonRegistry<Biome> SimulationParameters::biomeRegistry;
TJsonRegistry<OrganelleType> SimulationParameters::organelleRegistry;
TJsonRegistry<Species> SimulationParameters::speciesRegistry;
std::unordered_map<size_t, unsigned int> SimulationParameters::newSpeciesStartingCompounds;

void SimulationParameters::init() {
	// Loading the registries.
	SimulationParameters::compoundRegistry = TJsonRegistry<Compound>("./Data/Scripts/SimulationParameters/MicrobeStage/Compounds.json");
	SimulationParameters::bioProcessRegistry = TJsonRegistry<BioProcess>("./Data/Scripts/SimulationParameters/MicrobeStage/BioProcesses.json");
	SimulationParameters::biomeRegistry = TJsonRegistry<Biome>("./Data/Scripts/SimulationParameters/MicrobeStage/Biomes.json");
	SimulationParameters::organelleRegistry = TJsonRegistry<OrganelleType>("./Data/Scripts/SimulationParameters/MicrobeStage/Organelles.json");
	SimulationParameters::speciesRegistry = TJsonRegistry<Species>("./Data/Scripts/SimulationParameters/MicrobeStage/Species.json");

	// Getting the JSON file where the data is stored.
	std::ifstream jsonFile;
	jsonFile.open("./Data/Scripts/SimulationParameters/MicrobeStage/StartingCompounds.json");
	LEVIATHAN_ASSERT(jsonFile.is_open(), "The file './Data/Scripts/SimulationParameters/MicrobeStage/StartingCompounds.json' failed to load!");
	Json::Value rootElement;
	jsonFile >> rootElement;
	// TODO: add some sort of validation of the receiving JSON file, otherwise it fails silently and makes the screen go black.
	jsonFile.close();

	// Getting the starting compounds.
	std::vector<std::string> compoundInternalNames = rootElement.getMemberNames();
	for (std::string compoundInternalName : compoundInternalNames) {
		unsigned int amount = rootElement[compoundInternalName].asUInt();

		// Getting the compound id from the compound registry.
		size_t id = SimulationParameters::compoundRegistry.getTypeData(compoundInternalName).id;

		SimulationParameters::newSpeciesStartingCompounds.emplace(id, amount);
	}
}
