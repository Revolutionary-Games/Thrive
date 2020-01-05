// ------------------------------------ //
#include "species.h"

using namespace thrive;
// ------------------------------------ //
Species::Species(const std::string& name) : name(name) {}

Species::~Species()
{
    SAFE_RELEASE(organelles);
    SAFE_RELEASE(avgCompoundAmounts);
}
// ------------------------------------ //
void
    Species::setPopulationFromPatches(int32_t population)
{
    if(population < 0) {
        this->population = 0;
    } else {
        this->population = population;
    }
}
void
    Species::applyImmediatePopulationChange(int32_t change)
{
    population += change;

    if(population < 0)
        population = 0;
}
// ------------------------------------ //
bool
    Species::isPlayerSpecies() const
{
    return name == "Default";
}
// ------------------------------------ //
std::string
    Species::getFormattedName(bool identifier)
{
    std::string result;

    result = genus + " " + epithet;

    if(identifier)
        result += " (" + name + ")";

    return result;
}
// ------------------------------------ //
Json::Value
    Species::toJSON(bool full /*= false*/) const
{
    Json::Value result;

    result["isBacteria"] = isBacteria;
    result["membraneType"] = membraneType;
    result["name"] = name;
    result["genus"] = genus;
    result["epithet"] = epithet;
    result["stringCode"] = stringCode;

    result["aggression"] = aggression;
    result["opportunism"] = opportunism;
    result["fear"] = fear;
    result["activity"] = activity;
    result["focus"] = focus;
    result["population"] = population;
    result["generation"] = generation;

    result["isPlayerSpecies"] = isPlayerSpecies();

    Json::Value color;
    color["r"] = colour.X;
    color["g"] = colour.Y;
    color["b"] = colour.Z;
    color["a"] = colour.W;
    result["color"] = color;

    if(full) {
        LOG_WARNING("Species: toJSON: full is not implemented");
    }

    return result;
}
// ------------------------------------ //
Species*
    Species::factory(const std::string& name)
{
    return new Species(name);
}
