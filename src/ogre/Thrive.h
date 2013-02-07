#ifndef __BasicTutorial6_h_
#define __BasicTutorial6_h_

#include "CellStage/World.h"
#include "CellStage/Cell.h"

#include "entityframework/Engine.h"
#include "entityframework/Entity.h"
#include "entityframework/System.h"
#include "entityframework/Component.h"

#include "Agents/Agent.h"
#include "Agents/KeyboardAgent.h"

#include <OgreRoot.h>
#include <OISEvents.h>
#include <OISInputManager.h>
#include <OISKeyboard.h>
#include <OISMouse.h>
#include <OgreWindowEventUtilities.h>

class Thrive : public Ogre::WindowEventListener, public Ogre::FrameListener
{
public:
    Thrive(void);
    virtual ~Thrive(void);
    bool go(void);

protected:
    // Ogre::WindowEventListener
    virtual void windowResized(Ogre::RenderWindow* rw);
    virtual void windowClosed(Ogre::RenderWindow* rw);

    // Ogre::FrameListener
    virtual bool frameRenderingQueued(const Ogre::FrameEvent& evt);

private:
    Ogre::Root*         mRoot;
    Ogre::String        mResourcesCfg;
    Ogre::String        mPluginsCfg;
    Ogre::RenderWindow* mWindow;
    Ogre::SceneManager* mSceneMgr;
    Ogre::Camera*       mCamera;
    Ogre::SceneNode*    mCamNode;
    Ogre::Node*         playerNode;
    
    

    // OIS Input devices26
    OIS::InputManager*  mInputManager;
    OIS::Mouse*         mMouse;
    OIS::Keyboard*      mKeyboard;

    // OIS Variables
    bool                isMousePressed = false;

    // World
    World*              mWorld;
    Cell*               mTestCell;
    Cell*               mTestCell2;
    Cell*               mPlayerCell;
    Engine*             engine;
};

#endif // #ifndef __BasicTutorial6_h_
