#ifndef COMPONENT_H_
#define COMPONENT_H_

#include <OgreRoot.h>
#include <OgreEntity.h>
#include "../Agents/Agent.h"
#include "../CellStage/Organelle.h"

class Component
{
public:
	//virtual ~Component() = 0;
    virtual std::string getType() = 0;
};
/*
class OgreEntityComponent : public Component
{
public:
    OgreEntityComponent();
    Ogre::Entity* entity;
    std::string getType();
};
*/
class VelocityComponent : public Component
{
public:
    VelocityComponent();
    Ogre::Vector3 velocity;
    std::string getType();
};

class OgreNodeComponent : public Component
{
public:
    OgreNodeComponent();
    Ogre::SceneNode* node;
    std::string getType();
};

class AgentComponent : public Component
{
public:
    AgentComponent();
    Agent* agent;
    std::string getType();
};

class ColisionGroupComponent : public Component
{
public:
    ColisionGroupComponent();
    std::string colisionGroup;
    int radius;         //should be changed to something that works for any shape
    std::string getType();
};

class SpecieInfoComponent : public Component
{
public:
    SpecieInfoComponent();
    std::string specieName;
    Ogre::Entity* entity; //Model
    std::vector<Organelle*>* organelleList;
    //Attributes
    Ogre::Real mass;
    Ogre::Real acceleration;
    std::string getType();
};

class SpecieComponent : public Component
{
public:
    SpecieComponent();
    SpecieInfoComponent* specie;
    std::string getType();
};

#endif /* COMPONENT_H_ */
