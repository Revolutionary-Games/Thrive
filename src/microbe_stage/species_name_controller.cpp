#include "species_name_controller.h"

#include <Define.h>
#include <fstream>
#include <jsoncpp/json.h>

using namespace thrive;

SpeciesNameController::SpeciesNameController() {}

SpeciesNameController::SpeciesNameController(std::string jsonFilePath)
{
    // Getting the JSON file where the data is stored.
    std::ifstream jsonFile;
    jsonFile.open(
        "./Data/Scripts/SimulationParameters/MicrobeStage/SpeciesNames.json");
    LEVIATHAN_ASSERT(jsonFile.is_open(), "The file "
                                         "'./Data/Scripts/SimulationParameters/"
                                         "MicrobeStage/StartingCompounds.json' "
                                         "failed to load!");
    Json::Value rootElement;
    jsonFile >> rootElement;
    // TODO: add some sort of validation of the receiving JSON file, otherwise
    // it fails silently and makes the screen go black.
    jsonFile.close();

    for(Json::Value::ArrayIndex i = 0; i < rootElement["prefixes"].size(); i++)
        prefixes.push_back(rootElement["prefixes"][i].asString());

    for(Json::Value::ArrayIndex i = 0; i < rootElement["cofixes"].size(); i++)
        cofixes.push_back(rootElement["cofixes"][i].asString());

    for(Json::Value::ArrayIndex i = 0; i < rootElement["suffixes"].size(); i++)
        suffixes.push_back(rootElement["suffixes"][i].asString());
}
