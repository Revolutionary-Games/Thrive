#include "System.h"
#include "Engine.h"
#include <vector>

MoveSystem::MoveSystem(Engine* e)
{
    engine = e;
}
void MoveSystem::update(Ogre::Real deltaTime){
    targets = (std::vector<MoveNode*>*)engine->getNodeList(typeid(MoveNode).name());
    for (std::vector<MoveNode*>::iterator i = targets->begin(); i!=targets->end(); i++)
    {
        MoveNode* target = (*i);
        //This line crashes the aplication. turns out that typeid doesen't work as i expect
        //it finds a node, but that node is not of the type we need
        //target->ogreNode->node->translate(/*deltaTime*target->velocity->velocity*/Ogre::Vector3::UNIT_X, Ogre::SceneNode::TransformSpace::TS_WORLD);
    }
}
