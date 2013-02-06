#ifndef COMPONENT_H_
#define COMPONENT_H_

#include <OgreRoot.h>
#include <OgreEntity.h>

class Component
{
public:
	//virtual ~Component() = 0;
};

class OgreEntityComponent : public Component
{
public:
    OgreEntityComponent();
    Ogre::Entity* entity;
};

class VelocityComponent : public Component
{
public:
    VelocityComponent();
    Ogre::Vector3 velocity;
};

class OgreNodeComponent : public Component
{
public:
    OgreNodeComponent();
    Ogre::SceneNode* node;
};



#endif /* COMPONENT_H_ */