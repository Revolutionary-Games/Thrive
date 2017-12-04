#include "microbe_stage/simulation_parameters.h"

using namespace thrive;

TJsonRegistry<Compound> SimulationParameters::compoundRegistry;
TJsonRegistry<BioProcess> SimulationParameters::bioProcessRegistry;
TJsonRegistry<Biome> SimulationParameters::biomeRegistry;
TJsonRegistry<OrganelleType> SimulationParameters::organelleRegistry;
TJsonRegistry<Species> SimulationParameters::speciesRegistry;

void SimulationParameters::init() {
	SimulationParameters::compoundRegistry = TJsonRegistry<Compound>("./Data/Scripts/SimulationParameters/MicrobeStage/Compounds.json");
	SimulationParameters::bioProcessRegistry = TJsonRegistry<BioProcess>("./Data/Scripts/SimulationParameters/MicrobeStage/BioProcesses.json");
	SimulationParameters::biomeRegistry = TJsonRegistry<Biome>("./Data/Scripts/SimulationParameters/MicrobeStage/Biomes.json");
	SimulationParameters::organelleRegistry = TJsonRegistry<OrganelleType>("./Data/Scripts/SimulationParameters/MicrobeStage/Organelles.json");
	SimulationParameters::speciesRegistry = TJsonRegistry<Species>("./Data/Scripts/SimulationParameters/MicrobeStage/Species.json");
}
