#ifndef RANDOMAGENT_H
#define	RANDOMAGENT_H

#include <OgreRoot.h>
#include "Agent.h"

class RandomAgent : public Agent
{
public:
       RandomAgent();
       std::string getType();
       Ogre::Vector3 update();
};

#endif	/* RANDOMAGENT_H */

