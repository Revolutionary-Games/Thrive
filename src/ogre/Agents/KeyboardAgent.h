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
    KeyboardAgent(OIS::Keyboard*);
    std::string getType();
    Ogre::Vector3 update();
private:
    OIS::Keyboard*      mKeyboard;  
    OIS::InputManager*  mInputManager;
};

#endif	/* KEYBOARDAGENT_H */

