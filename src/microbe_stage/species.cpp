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
