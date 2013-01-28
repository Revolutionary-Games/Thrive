#ifndef __BasicTutorial6_h_
#define __BasicTutorial6_h_

#include "CellStage/World.h"
 
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
 
    // OIS Input devices26
    OIS::InputManager*  mInputManager;
    OIS::Mouse*         mMouse;
    OIS::Keyboard*      mKeyboard;
    
    // World
    World*              mWorld;
};
 
#endif // #ifndef __BasicTutorial6_h_