#include "membrane_types.h"
#include "general/json_registry.h"

using namespace thrive;

MembraneType::MembraneType() {}

MembraneType::MembraneType(Json::Value value)
{
	cellWall = value["cellWall"].asBool();
    normalTexture = value["normalTexture"].asString();
    damagedTexture = value["damagedTexture"].asString();
    hitpoints = value["hitpoints"].asFloat();
}
