#ifndef SYSTEM_H
#define	SYSTEM_H

#include "OgreRoot.h"
#include "System.h"
#include "Node.h"
#include "Component.h"
class Engine;
class ISystem
{
public:
    virtual ~ISystem(){}
    virtual void update(Ogre::Real) = 0;
    Engine* engine;
};

class MoveSystem : public ISystem
{
public:
    MoveSystem(Engine*);
    void update(Ogre::Real);
private:
    std::vector<MoveNode*>* targets;
};

class ControllSystem : public ISystem
{
public:
    ControllSystem(Engine* e);
    void update(Ogre::Real);
private:
    std::vector<ControllerNode*>* targets;
};


#endif	/* SYSTEM_H */

