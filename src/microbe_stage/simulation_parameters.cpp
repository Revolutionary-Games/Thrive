#include "microbe_stage/simulation_parameters.h"

using namespace thrive;

TJsonRegistry<Compound> SimulationParameters::compoundRegistry;
TJsonRegistry<BioProcess> SimulationParameters::bioProcessRegistry;
TJsonRegistry<Biome> SimulationParameters::biomeRegistry;
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
    // SimulationParameters::organelleRegistry =
    // TJsonRegistry<OrganelleType>("./Data/Scripts/SimulationParameters/MicrobeStage/Organelles.json");

    SimulationParameters::speciesNameController = SpeciesNameController(
        "./Data/Scripts/SimulationParameters/MicrobeStage/SpeciesNames.json");

    // Getting the JSON file where the data is stored.
    std::ifstream jsonFile;
    jsonFile.open("./Data/Scripts/SimulationParameters/MicrobeStage/"
                  "StartingCompounds.json");
    LEVIATHAN_ASSERT(jsonFile.is_open(), "The file "
                                         "'./Data/Scripts/SimulationParameters/"
                                         "MicrobeStage/StartingCompounds.json' "
                                         "failed to load!");
    Json::Value rootElement;
    try {
        jsonFile >> rootElement;
    } catch(const Json::RuntimeError& e) {
        LOG_ERROR(
            std::string("Syntax error in json file: StartingCompounds.json") +
            ", description: " + std::string(e.what()));
        throw e;
    }

    // TODO: add some sort of validation of the receiving JSON file, otherwise
    // it fails silently and makes the screen go black.
    jsonFile.close();
}
