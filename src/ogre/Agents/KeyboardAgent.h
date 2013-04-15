#ifndef KEYBOARDAGENT_H
#define	KEYBOARDAGENT_H

#include <OgreRoot.h>
#include <OISEvents.h>
#include <OISInputManager.h>
#include <OISKeyboard.h>
#include <OISMouse.h>
#include <OgreWindowEventUtilities.h>
#include <OgreRenderWindow.h>

#include "Agent.h"

class KeyboardAgent : public Agent
{
public:
    KeyboardAgent(Ogre::Vector3&);
    std::string getType();
    Ogre::Vector3 update();
    OIS::Keyboard*      mKeyboard; 
    Ogre::Vector3       move;
private:
    OIS::InputManager*  mInputManager;
};

#endif	/* KEYBOARDAGENT_H */

