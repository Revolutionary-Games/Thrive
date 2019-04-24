#include "general/properties_component.h"

#include <Entities/GameWorld.h>

using namespace thrive;

AgentProperties::AgentProperties() : Leviathan::Component(TYPE)
{
    speciesName = "";
    agentType = "";
}

std::string
    AgentProperties::getSpeciesName()
{
    return this->speciesName;
}
std::string
    AgentProperties::getAgentType()
{
    return this->agentType;
}

ObjectID
    AgentProperties::getParentEntity()
{
    return this->parentId;
}

void
    AgentProperties::setSpeciesName(std::string newString)
{
    this->speciesName = newString;
}
void
    AgentProperties::setAgentType(std::string newString)
{
    this->agentType = newString;
}
void
    AgentProperties::setParentEntity(ObjectID parentId)
{
    this->parentId = parentId;
}

// DamageOnTouch component
DamageOnTouchComponent::DamageOnTouchComponent() : Leviathan::Component(TYPE) {}

void
    DamageOnTouchComponent::setDamage(double damage)
{
    this->damage = damage;
}

double
    DamageOnTouchComponent::getDamage()
{
    return this->damage;
}

void
    DamageOnTouchComponent::setDeletes(bool deletes)
{
    this->deletes = deletes;
}

bool
    DamageOnTouchComponent::getDeletes()
{
    return this->deletes;
}
