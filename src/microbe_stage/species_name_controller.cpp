#include "species_name_controller.h"

#include <Define.h>
#include <Include.h>
#include <Script/ScriptConversionHelpers.h>
#include <add_on/scriptarray/scriptarray.h>
#include <fstream>
#include <json/json.h>

using namespace thrive;

SpeciesNameController::SpeciesNameController() {}

SpeciesNameController::SpeciesNameController(std::string jsonFilePath)
{
    // Getting the JSON file where the data is stored.
    std::ifstream jsonFile;
    jsonFile.open("./Data/Scripts/simulation_parameters/microbe_stage/"
                  "species_names.json");
    LEVIATHAN_ASSERT(jsonFile.is_open(),
        "The file "
        "'./Data/Scripts/simulation_parameters/"
        "microbe_stage/starting_compounds.json' "
        "failed to load!");
    Json::Value rootElement;
    try {
        jsonFile >> rootElement;
    } catch(const Json::RuntimeError& e) {
        LOG_ERROR(std::string("Syntax error in json file: species_names.json") +
                  ", description: " + std::string(e.what()));
        throw e;
    }

    for(Json::Value::ArrayIndex i = 0; i < rootElement["prefixcofix"].size();
        i++)
        prefixcofixes.push_back(rootElement["prefixcofix"][i].asString());

    for(Json::Value::ArrayIndex i = 0; i < rootElement["prefixes_v"].size();
        i++)
        prefixes_v.push_back(rootElement["prefixes_v"][i].asString());

    for(Json::Value::ArrayIndex i = 0; i < rootElement["prefixes_c"].size();
        i++)
        prefixes_c.push_back(rootElement["prefixes_c"][i].asString());

    for(Json::Value::ArrayIndex i = 0; i < rootElement["cofixes_v"].size(); i++)
        cofixes_v.push_back(rootElement["cofixes_v"][i].asString());

    for(Json::Value::ArrayIndex i = 0; i < rootElement["cofixes_c"].size(); i++)
        cofixes_c.push_back(rootElement["cofixes_c"][i].asString());

    for(Json::Value::ArrayIndex i = 0; i < rootElement["suffixes_c"].size();
        i++)
        suffixes.push_back(rootElement["suffixes_c"][i].asString());

    for(Json::Value::ArrayIndex i = 0; i < rootElement["suffixes_v"].size();
        i++)
        suffixes.push_back(rootElement["suffixes_v"][i].asString());

    for(Json::Value::ArrayIndex i = 0; i < rootElement["suffixes_c"].size();
        i++)
        suffixes_c.push_back(rootElement["suffixes_c"][i].asString());

    for(Json::Value::ArrayIndex i = 0; i < rootElement["suffixes_v"].size();
        i++)
        suffixes_v.push_back(rootElement["suffixes_v"][i].asString());

    // TODO: add some sort of validation of the receiving JSON file, otherwise
    // it fails silently and makes the screen go black.
    jsonFile.close();
}

CScriptArray*
    SpeciesNameController::getVowelPrefixes()
{
    // Method taken from Leviathan::ConvertVectorToASArray
    return Leviathan::ConvertIteratorToASArray(std::begin(prefixes_v),
        std::end(prefixes_v), Leviathan::ScriptExecutor::Get()->GetASEngine());
}

CScriptArray*
    SpeciesNameController::getConsonantPrefixes()
{
    // Method taken from Leviathan::ConvertVectorToASArray
    return Leviathan::ConvertIteratorToASArray(std::begin(prefixes_c),
        std::end(prefixes_c), Leviathan::ScriptExecutor::Get()->GetASEngine());
}

CScriptArray*
    SpeciesNameController::getVowelCofixes()
{
    // Method taken from Leviathan::ConvertVectorToASArray
    return Leviathan::ConvertIteratorToASArray(std::begin(cofixes_v),
        std::end(cofixes_v), Leviathan::ScriptExecutor::Get()->GetASEngine());
}

CScriptArray*
    SpeciesNameController::getConsonantCofixes()
{
    // Method taken from Leviathan::ConvertVectorToASArray
    return Leviathan::ConvertIteratorToASArray(std::begin(cofixes_c),
        std::end(cofixes_c), Leviathan::ScriptExecutor::Get()->GetASEngine());
}

CScriptArray*
    SpeciesNameController::getSuffixes()
{
    // Method taken from Leviathan::ConvertVectorToASArray
    return Leviathan::ConvertIteratorToASArray(std::begin(suffixes),
        std::end(suffixes), Leviathan::ScriptExecutor::Get()->GetASEngine());
}

CScriptArray*
    SpeciesNameController::getVowelSuffixes()
{
    // Method taken from Leviathan::ConvertVectorToASArray
    return Leviathan::ConvertIteratorToASArray(std::begin(suffixes_v),
        std::end(suffixes_v), Leviathan::ScriptExecutor::Get()->GetASEngine());
}

CScriptArray*
    SpeciesNameController::getConsonantSuffixes()
{
    // Method taken from Leviathan::ConvertVectorToASArray
    return Leviathan::ConvertIteratorToASArray(std::begin(suffixes_c),
        std::end(suffixes_c), Leviathan::ScriptExecutor::Get()->GetASEngine());
}

CScriptArray*
    SpeciesNameController::getPrefixCofix()
{
    // Method taken from Leviathan::ConvertVectorToASArray
    return Leviathan::ConvertIteratorToASArray(std::begin(prefixcofixes),
        std::end(prefixcofixes),
        Leviathan::ScriptExecutor::Get()->GetASEngine());
}
