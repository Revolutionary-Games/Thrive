#include "microbe_stage/simulation_parameters.h"

using namespace thrive;

TJsonRegistry<Compound> SimulationParameters::compoundRegistry;
TJsonRegistry<BioProcess> SimulationParameters::bioProcessRegistry;
TJsonRegistry<Biome> SimulationParameters::biomeRegistry;
TJsonRegistry<Background> SimulationParameters::backgroundRegistry;
// TJsonRegistry<OrganelleType> SimulationParameters::organelleRegistry;
SpeciesNameController SimulationParameters::speciesNameController;

void
    SimulationParameters::init()
{
    // Loading the registries.
    SimulationParameters::compoundRegistry = TJsonRegistry<Compound>(
        "./Data/Scripts/SimulationParameters/MicrobeStage/Compounds.json");
    SimulationParameters::bioProcessRegistry = TJsonRegistry<BioProcess>(
        "./Data/Scripts/SimulationParameters/MicrobeStage/BioProcesses.json");
    SimulationParameters::biomeRegistry = TJsonRegistry<Biome>(
        "./Data/Scripts/SimulationParameters/MicrobeStage/Biomes.json");
    SimulationParameters::backgroundRegistry = TJsonRegistry<Background>(
        "./Data/Scripts/SimulationParameters/MicrobeStage/backgrounds.json");
    // SimulationParameters::organelleRegistry =
    // TJsonRegistry<OrganelleType>("./Data/Scripts/SimulationParameters/MicrobeStage/Organelles.json");

    SimulationParameters::speciesNameController = SpeciesNameController(
        "./Data/Scripts/SimulationParameters/MicrobeStage/SpeciesNames.json");
}
