#ifndef NODE_H
#define	NODE_H

#include "Component.h"

class Node
{
public:
    virtual ~Node(){}
};

class MoveNode : public Node
{
public:
    MoveNode();
    OgreNodeComponent* ogreNode;
    VelocityComponent* velocity;
};

class RenderNode : public Node
{
public:
    RenderNode();
    OgreEntityComponent* entity;
};


#endif	/* NODE_H */

