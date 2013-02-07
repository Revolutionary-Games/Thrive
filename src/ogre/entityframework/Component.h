#ifndef COMPONENT_H_
#define COMPONENT_H_

#include <OgreRoot.h>
#include <OgreEntity.h>

class Component
{
public:
	//virtual ~Component() = 0;
    virtual std::string getType();
};

class OgreEntityComponent : public Component
{
public:
    OgreEntityComponent();
    Ogre::Entity* entity;
    std::string getType();
};

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



#endif /* COMPONENT_H_ */