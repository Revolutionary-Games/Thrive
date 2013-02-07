#ifndef NODE_H
#define	NODE_H

#include "Component.h"

class Node
{
public:
    virtual ~Node(){};
    virtual std::string getType() = 0;
};

class MoveNode : public Node
{
public:
    MoveNode();
    OgreNodeComponent* ogreNode;
    VelocityComponent* velocity;
    std::string getType();  
};

class RenderNode : public Node
{
public:
    RenderNode();
    OgreEntityComponent* entity;
    std::string getType();
};

class ControllerNode : public Node
{
public:
    ControllerNode();
    VelocityComponent* velocity;
    AgentComponent* agent;
    std::string getType();
};


#endif	/* NODE_H */

