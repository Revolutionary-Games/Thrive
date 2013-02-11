#include "Component.h"

OgreEntityComponent::OgreEntityComponent()
{
        
}
std::string OgreEntityComponent::getType()
{
    return "OgreEntity";
}

VelocityComponent::VelocityComponent()
{
        
}
std::string VelocityComponent::getType()
{
    return "Velocity";
}

OgreNodeComponent::OgreNodeComponent()
{

}
std::string OgreNodeComponent::getType()
{
    return "OgreNode";
}

AgentComponent::AgentComponent()
{
    
}
std::string AgentComponent::getType()
{
    return "Agent";
}

ColisionGroupComponent::ColisionGroupComponent()
{
    
}
std::string ColisionGroupComponent::getType()
{
    return "ColisionGroup";
}