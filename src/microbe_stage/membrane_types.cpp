#include "membrane_types.h"
#include "general/json_registry.h"

using namespace thrive;

MembraneType::MembraneType() {}

MembraneType::MembraneType(Json::Value value)
{
    cellWall = value["cellWall"].asBool();
    normalTexture = value["normalTexture"].asString();
    damagedTexture = value["damagedTexture"].asString();
    movementFactor = value["movementFactor"].asFloat();
    osmoregulationFactor = value["osmoregulationFactor"].asFloat();
    resourceAbsorptionFactor = value["resourceAbsorptionFactor"].asFloat();
    hitpoints = value["hitpoints"].asFloat();
    physicalResistance = value["physicalResistance"].asFloat();
    toxinResistance = value["toxinResistance"].asFloat();
    editorCost = value["editorCost"].asInt();
}
