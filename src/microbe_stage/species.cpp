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
