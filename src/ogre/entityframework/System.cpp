#include "System.h"
#include "Engine.h"
#include <vector>

MoveSystem::MoveSystem(Engine* e)
{
    engine = e;
}
void MoveSystem::update(Ogre::Real deltaTime){
    targets = (std::vector<MoveNode*>*)engine->getNodeList("Move");
    for (std::vector<MoveNode*>::iterator i = targets->begin(); i!=targets->end(); i++)
    {
        MoveNode* target = (*i);
        //This line crashes the aplication. turns out that typeid doesen't work as i expect
        //it finds a node, but that node is not of the type we need
        //MessageBox( NULL, target->ogreNode->getType().c_str(), "Error"/*target->ogreNode->type.c_str()*/, MB_OK | MB_ICONERROR | MB_TASKMODAL);
        target->ogreNode->node->translate(deltaTime*target->velocity->velocity, Ogre::SceneNode::TransformSpace::TS_WORLD);
    }
}
