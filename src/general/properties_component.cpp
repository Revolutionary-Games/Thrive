#include "general/properties_component.h"

#include <Entities/GameWorld.h>

using namespace thrive;

PropertiesComponent::PropertiesComponent() : Leviathan::Component(TYPE)
{
    string1 = "";
    string2 = "";
}

std::string
    PropertiesComponent::getStringOne()
{
    return this->string1;
}
std::string
    PropertiesComponent::getStringTwo()
{
    return this->string2;
}
void
    PropertiesComponent::setStringOne(std::string newString)
{
    this->string1 = newString;
}
void
    PropertiesComponent::setStringTwo(std::string newString)
{
    this->string2 = newString;
}
