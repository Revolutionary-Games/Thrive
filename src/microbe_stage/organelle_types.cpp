#include "organelle_types.h"

#include <Script/ScriptConversionHelpers.h>
#include <Script/ScriptExecutor.h>

#include <boost/range/adaptor/map.hpp>

#include <map>
#include <string>
#include <vector>

using namespace thrive;

OrganelleType::OrganelleType() {}

OrganelleType::OrganelleType(Json::Value value)
{
    name = value["name"].asString();
    gene = value["gene"].asString();
    mesh = value["mesh"].asString();
    texture = value["texture"].asString();
    mass = value["mass"].asFloat();
    chanceToCreate = value["chanceToCreate"].asFloat();
    prokaryoteChance = value["prokaryoteChance"].asFloat();

    Json::Value componentData = value["components"];
    std::vector<std::string> componentNames = componentData.getMemberNames();

    for(std::string componentName : componentNames) {
        Json::Value parameterData = componentData[componentName];
        std::vector<std::string> parameterNames =
            parameterData.getMemberNames();
        std::map<std::string, Json::Value> parameters;

        for(std::string parameterName : parameterNames) {
            parameters[parameterName] = parameterData[parameterName];
        }

        components[componentName] = parameters;
    }

    Json::Value processData = value["processes"];
    std::vector<std::string> processNames = processData.getMemberNames();

    for(std::string processName : processNames) {
        processes[processName] = processData[processName].asDouble();
    }

    // Getting the hex information.
    Json::Value hexArray = value["hexes"];

    for(Json::Value hex : hexArray) {
        hexes.push_back(Int2(hex["q"].asInt(), hex["r"].asInt()));
    }

    // Getting the initial composition information.
    Json::Value initialCompositionData = value["initialComposition"];
    std::vector<std::string> compoundInternalNames =
        initialCompositionData.getMemberNames();

    for(std::string compoundInternalName : compoundInternalNames) {
        initialComposition[compoundInternalName] =
            initialCompositionData[compoundInternalName].asDouble();
    }

    mpCost = value["mpCost"].asInt();
}

CScriptArray*
    OrganelleType::getComponentKeys() const
{
    return Leviathan::ConvertIteratorToASArray(
        (components | boost::adaptors::map_keys).begin(),
        (components | boost::adaptors::map_keys).end(),
        Leviathan::ScriptExecutor::Get()->GetASEngine(), "array<string>");
}

CScriptArray*
    OrganelleType::getComponentParameterKeys(std::string component)
{
    return Leviathan::ConvertIteratorToASArray(
        (components[component] | boost::adaptors::map_keys).begin(),
        (components[component] | boost::adaptors::map_keys).end(),
        Leviathan::ScriptExecutor::Get()->GetASEngine(), "array<string>");
}

double
    OrganelleType::getComponentParameterAsDouble(std::string component,
        std::string parameter)
{
    return components[component][parameter].asDouble();
}

std::string
    OrganelleType::getComponentParameterAsString(std::string component,
        std::string parameter)
{
    return components[component][parameter].asString();
}

CScriptArray*
    OrganelleType::getProcessKeys() const
{
    return Leviathan::ConvertIteratorToASArray(
        (processes | boost::adaptors::map_keys).begin(),
        (processes | boost::adaptors::map_keys).end(),
        Leviathan::ScriptExecutor::Get()->GetASEngine(), "array<string>");
}

float
    OrganelleType::getProcessTweakRate(std::string process)
{
    return processes[process];
}

CScriptArray*
    OrganelleType::getHexes() const
{
    return Leviathan::ConvertIteratorToASArray(hexes.begin(), hexes.end(),
        Leviathan::ScriptExecutor::Get()->GetASEngine(), "array<Int2>");
}

CScriptArray*
    OrganelleType::getInitialCompositionKeys() const
{
    return Leviathan::ConvertIteratorToASArray(
        (initialComposition | boost::adaptors::map_keys).begin(),
        (initialComposition | boost::adaptors::map_keys).end(),
        Leviathan::ScriptExecutor::Get()->GetASEngine(), "array<string>");
}

double
    OrganelleType::getInitialComposition(std::string compound)
{
    return initialComposition[compound];
}
