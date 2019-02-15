#include "compounds.h"
#include "general/json_registry.h"

using namespace thrive;

Compound::Compound() {}

Compound::Compound(size_t id,
    const std::string& name,
    bool isCloud,
    bool isUseful,
    bool isEnvironmental,
    Ogre::ColourValue colour) :
    RegistryType(id, name),
    isCloud(isCloud), isUseful(isUseful), isEnvironmental(isEnvironmental),
    colour(colour)
{}

Compound::Compound(Json::Value value)
{
    volume = value["volume"].asDouble();
    isCloud = value["isCloud"].asBool();
    isUseful = value["isUseful"].asBool();
    isEnvironmental = value["isEnvironmental"].asBool();
    // Setting the cloud colour.
    float r = value["colour"]["r"].asFloat();
    float g = value["colour"]["g"].asFloat();
    float b = value["colour"]["b"].asFloat();
    colour = Ogre::ColourValue(r, g, b, 1.0);
}
