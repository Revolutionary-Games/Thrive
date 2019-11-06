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
        "./Data/Scripts/simulation_parameters/microbe_stage/compounds.json");
    SimulationParameters::bioProcessRegistry =
        TJsonRegistry<BioProcess>("./Data/Scripts/simulation_parameters/"
                                  "microbe_stage/bio_processes.json");
    SimulationParameters::biomeRegistry = TJsonRegistry<Biome>(
        "./Data/Scripts/simulation_parameters/microbe_stage/biomes.json");
    SimulationParameters::backgroundRegistry = TJsonRegistry<Background>(
        "./Data/Scripts/simulation_parameters/microbe_stage/backgrounds.json");
    // SimulationParameters::organelleRegistry =
    // TJsonRegistry<OrganelleType>("./Data/Scripts/SimulationParameters/MicrobeStage/Organelles.json");

    SimulationParameters::speciesNameController =
        SpeciesNameController("./Data/Scripts/simulation_parameters/"
                              "microbe_stage/species_names.json");
}
