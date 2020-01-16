#include "microbe_stage/simulation_parameters.h"
#include "microbe_stage/organelle_template.h"

using namespace thrive;

TJsonRegistry<Compound> SimulationParameters::compoundRegistry;
TJsonRegistry<BioProcess> SimulationParameters::bioProcessRegistry;
TJsonRegistry<Biome> SimulationParameters::biomeRegistry;
TJsonRegistry<Background> SimulationParameters::backgroundRegistry;
TJsonRegistry<OrganelleType> SimulationParameters::organelleRegistry;
TJsonRegistry<MembraneType> SimulationParameters::membraneRegistry;
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
    SimulationParameters::organelleRegistry = TJsonRegistry<OrganelleType>(
        "./Data/Scripts/simulation_parameters/microbe_stage/organelles.json");
    SimulationParameters::membraneRegistry = TJsonRegistry<MembraneType>(
        "./Data/Scripts/simulation_parameters/microbe_stage/membranes.json");

    SimulationParameters::speciesNameController =
        SpeciesNameController("./Data/Scripts/simulation_parameters/"
                              "microbe_stage/species_names.json");
}
