#include "System.h"
#include "Engine.h"
#include "../Agents/Agent.h"
#include "../Agents/KeyboardAgent.h"
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
        target->ogreNode->node->translate(deltaTime*target->velocity->velocity, Ogre::SceneNode::TransformSpace::TS_WORLD);
    }
}

ControllSystem::ControllSystem(Engine* e)
{
    engine = e;
}
void ControllSystem::update(Ogre::Real deltaTime){
    targets = (std::vector<ControllerNode*>*)engine->getNodeList("Controller");
    for (std::vector<ControllerNode*>::iterator i = targets->begin(); i!=targets->end(); i++)
    {
        ControllerNode* target = (*i);
        if (target->agent->agent->getType()=="Keyboard")
        {
            KeyboardAgent* agent = (KeyboardAgent*)target->agent;
            Ogre::Vector3 direction= agent->update();
            target->velocity->velocity+=direction*0.025f;//Should change for performAction(action) or something
        }
        else if (target->agent->getType()=="LearningAI")
        {
            /*
             // More-or-less the code that will need to be called on a learning AI agent
             // There are also some other ways
             LearningAIAgent agent = (LearningAIAgent*)target->agent; 
             Action action = agent.update(); // Agent will access the state of the world to decide
             int reward = performAction(action);
             agent->onActionCompleted(action,reward); // Agent will see the resoulting state to update its weights, no need to pass
             */
        }
    }
}