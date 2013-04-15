#include "System.h"
#include "Engine.h"
#include "../Agents/Agent.h"
#include "../Agents/KeyboardAgent.h"
#include "../Agents/RandomAgent.h"
#include <vector>
#include <boost/lexical_cast.hpp>

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
        target->velocity->velocity*=0.995f;
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
        //MessageBox( NULL, "Updating controller", target->agent->agent->getType().c_str() , MB_OK | MB_ICONERROR | MB_TASKMODAL);
       
        if (target->agent->agent->getType()=="Keyboard")
        {
            KeyboardAgent* agent = (KeyboardAgent*)target->agent;
            Ogre::Vector3 direction= agent->update();
            target->velocity->velocity+=direction*0.025f;//Should change for performAction(action) or something
        }
        else if (target->agent->agent->getType()=="LearningAI")
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
        else if (target->agent->agent->getType()=="Random")
        {
            //MessageBox( NULL, "Updating controller random", target->agent->agent->getType().c_str() , MB_OK | MB_ICONERROR | MB_TASKMODAL);
       
            RandomAgent* agent = (RandomAgent*)target->agent->agent;
            Ogre::Vector3 direction= agent->update();
            target->velocity->velocity+=direction*0.025f;//Should change for performAction(action) or something
        
        }
    }
}

ColisionSystem::ColisionSystem(Engine* e)
{
    engine = e;
}

void ColisionSystem::update(Ogre::Real)
{
    targets = (std::vector<ColisionNode*>*)engine->getNodeList("Colision");
    for (std::vector<ColisionNode*>::iterator i = targets->begin(); i!=targets->end()-1;i++)
    {
        for (std::vector<ColisionNode*>::iterator j = i+1; j!=targets->end();j++)
        {
            if (true)//colidesWith.count({(*i)->colisionGroup->colisionGroup,(*j)->colisionGroup->colisionGroup})==1||colidesWith.count({(*j)->colisionGroup->colisionGroup,(*i)->colisionGroup->colisionGroup})==1)
            {
                //MessageBox( NULL, boost::lexical_cast<std::string>((*i)->ogreNode->node->getPosition()).c_str(), "An exception has occured!", MB_OK | MB_ICONERROR | MB_TASKMODAL);
                //MessageBox( NULL, boost::lexical_cast<std::string>((*j)->ogreNode->node->getPosition()).c_str(), "An exception has occured!", MB_OK | MB_ICONERROR | MB_TASKMODAL);
                
                Ogre::Vector3 posI = (*i)->ogreNode->node->getPosition();
                Ogre::Vector3 posJ = (*j)->ogreNode->node->getPosition();
                Ogre::Real combinedRadius = (*i)->colisionGroup->radius+(*j)->colisionGroup->radius;
                if (posI.distance(posJ)<combinedRadius)
                {
                    //MessageBox( NULL, "coliding", "An exception has occured!", MB_OK | MB_ICONERROR | MB_TASKMODAL);
                        //MessageBox( NULL, boost::lexical_cast<std::string>(1/(posI-posJ)).c_str(), "An exception has occured!", MB_OK | MB_ICONERROR | MB_TASKMODAL);
                    Ogre::Real sDistance = (posI-posJ).squaredLength();
                    (*i)->velocity->velocity+=(posI-posJ)*1/sDistance;
                    (*j)->velocity->velocity+=(posJ-posI)*1/sDistance;
                }
            }
        }
    }
}