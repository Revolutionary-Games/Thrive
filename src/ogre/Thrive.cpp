
/*
-----------------------------------------------------------------------------
Filename:    BasicTutorial6.cpp
-----------------------------------------------------------------------------

This source file is part of the
   ___                 __    __ _ _    _
  /___\__ _ _ __ ___  / / /\ \ (_) | _(_)
 //  // _` | '__/ _ \ \ \/  \/ / | |/ / |
/ \_// (_| | | |  __/  \  /\  /| |   <| |
\___/ \__, |_|  \___|   \/  \/ |_|_|\_\_|
      |___/
      Tutorial Framework
      http://www.ogre3d.org/tikiwiki/
-----------------------------------------------------------------------------
*/
#include "Thrive.h"

#include <OgreException.h>
#include <OgreConfigFile.h>
#include <OgreCamera.h>
#include <OgreViewport.h>
#include <OgreSceneManager.h>
#include <OgreRenderWindow.h>
#include <OgreEntity.h>
#include <OgreWindowEventUtilities.h>

#include <math.h>

//-------------------------------------------------------------------------------------
Thrive::Thrive(void)
    : mRoot(0),
    mResourcesCfg(Ogre::StringUtil::BLANK),
    mPluginsCfg(Ogre::StringUtil::BLANK),
    mWindow(0),
    mSceneMgr(0),
    mCamera(0)
{
}
//-------------------------------------------------------------------------------------
Thrive::~Thrive(void)
{
    //Remove ourself as a Window listener
    Ogre::WindowEventUtilities::removeWindowEventListener(mWindow, this);
    windowClosed(mWindow);
    delete mRoot;
    delete mWorld;
}

bool Thrive::go(void)
{
#ifdef _DEBUG
    mResourcesCfg = "resources_d.cfg";
    mPluginsCfg = "plugins_d.cfg";
#else
    mResourcesCfg = "resources.cfg";
    mPluginsCfg = "plugins.cfg";
#endif

    // construct Ogre::Root
    mRoot = new Ogre::Root(mPluginsCfg);

    // setup resources
    // Load resource paths from config file
    Ogre::ConfigFile cf;
    cf.load(mResourcesCfg);

    // Go through all sections & settings in the file
    Ogre::ConfigFile::SectionIterator seci = cf.getSectionIterator();

    Ogre::String secName, typeName, archName;
    while (seci.hasMoreElements())
    {
        secName = seci.peekNextKey();
        Ogre::ConfigFile::SettingsMultiMap *settings = seci.getNext();
        Ogre::ConfigFile::SettingsMultiMap::iterator i;
        for (i = settings->begin(); i != settings->end(); ++i)
        {
            typeName = i->first;
            archName = i->second;
            Ogre::ResourceGroupManager::getSingleton().addResourceLocation(
                archName, typeName, secName);
        }
    }

    // Show the configuration dialog and initialise the system
    if(!(mRoot->restoreConfig() || mRoot->showConfigDialog()))
    {
        return false;
    }

    mWindow = mRoot->initialise(true, "Thrive");

    // Set default mipmap level (NB some APIs ignore this)
    Ogre::TextureManager::getSingleton().setDefaultNumMipmaps(5);
    // initialise all resource groups
    Ogre::ResourceGroupManager::getSingleton().initialiseAllResourceGroups();

    /////////////////////////////////////////////////////////////////

    // Create the SceneManager, in this case a generic one
    mSceneMgr = mRoot->createSceneManager("DefaultSceneManager");

    // Create the camera
    mCamera = mSceneMgr->createCamera("PlayerCam");
    mCamNode = mSceneMgr->getRootSceneNode()->createChildSceneNode(
            "MainCameraNode", Ogre::Vector3(0,0,30),Ogre::Quaternion::IDENTITY);
    mCamNode->attachObject(mCamera);
//    mCamNode->lookAt(Ogre::Vector3::ZERO,Ogre::SceneNode::TransformSpace::TS_WORLD);
//    mCamera->setPosition(Ogre::Vector3(0,20,0));
//    mCamera->lookAt(Ogre::Vector3::NEGATIVE_UNIT_Y);
//    Ogre::Quaternion Quat = Ogre::Quaternion::IDENTITY;
//    Quat.FromAngleAxis(Ogre::Radian(-1.157), Ogre::Vector3::UNIT_X);
//    mCamera->setOrientation(Quat);
    mCamera->setNearClipDistance(5);
    mCamera->setFarClipDistance(2000);

    // Create one viewport, entire window
    Ogre::Viewport* vp = mWindow->addViewport(mCamera);
    vp->setBackgroundColour(Ogre::ColourValue(0,0,0));

    // Alter the camera aspect ratio to match the viewport
    mCamera->setAspectRatio(
        Ogre::Real(vp->getActualWidth()) / Ogre::Real(vp->getActualHeight()));

    // Set ambient light
    mSceneMgr->setAmbientLight(Ogre::ColourValue(0.5, 0.5, 0.5));

    // Create a light
    Ogre::Light* l = mSceneMgr->createLight("MainLight");
    l->setPosition(20,80,50);

    Ogre::LogManager::getSingletonPtr()->logMessage("*** Initializing OIS ***");
    Ogre::LogManager::getSingletonPtr()->logMessage(Ogre::StringConverter::toString(fmodf(.4f,-1.0f)));
    Ogre::LogManager::getSingletonPtr()->logMessage(Ogre::StringConverter::toString(fmodf(-1.4f,-1.0f)));
    Ogre::LogManager::getSingletonPtr()->logMessage(Ogre::StringConverter::toString(fmodf(-.4f,-1.0f)));
    OIS::ParamList pl;
    size_t windowHnd = 0;
    std::ostringstream windowHndStr;

    mWindow->getCustomAttribute("WINDOW", &windowHnd);
    windowHndStr << windowHnd;
    pl.insert(std::make_pair(std::string("WINDOW"), windowHndStr.str()));

    mInputManager = OIS::InputManager::createInputSystem( pl );

    mKeyboard = static_cast<OIS::Keyboard*>(mInputManager->createInputObject( OIS::OISKeyboard, false ));
    mMouse = static_cast<OIS::Mouse*>(mInputManager->createInputObject( OIS::OISMouse, false ));

    //Set initial mouse clipping size
    windowResized(mWindow);

    //Register as a Window listener
    Ogre::WindowEventUtilities::addWindowEventListener(mWindow, this);

    // Create our World.  All this does right now is set the background (a sky plane)
    mWorld = new World(mSceneMgr);

    mTestCell = new Cell(mSceneMgr, Ogre::Vector3::ZERO);
    mTestCell2 = new Cell(mSceneMgr, Ogre::Vector3(10,0,0));

    mRoot->addFrameListener(this);

    mRoot->startRendering();

    return true;
}

//Adjust mouse clipping area
void Thrive::windowResized(Ogre::RenderWindow* rw)
{
    unsigned int width, height, depth;
    int left, top;
    rw->getMetrics(width, height, depth, left, top);

    const OIS::MouseState &ms = mMouse->getMouseState();
    ms.width = width;
    ms.height = height;
}

//Unattach OIS before window shutdown (very important under Linux)
void Thrive::windowClosed(Ogre::RenderWindow* rw)
{
    //Only close for window that created OIS (the main window in these demos)
    if( rw == mWindow )
    {
        if( mInputManager )
        {
            mInputManager->destroyInputObject( mMouse );
            mInputManager->destroyInputObject( mKeyboard );

            OIS::InputManager::destroyInputSystem(mInputManager);
            mInputManager = 0;
        }
    }
}

bool Thrive::frameRenderingQueued(const Ogre::FrameEvent& evt)
{
    if(mWindow->isClosed())
        return false;

    // Capture/update each device
    mKeyboard->capture();
    mMouse->capture();

    // Move camera
    Ogre::Vector3 Move = Ogre::Vector3::ZERO;
    if(mKeyboard->isKeyDown(OIS::KC_A))
        Move += Ogre::Vector3::NEGATIVE_UNIT_X;
    if(mKeyboard->isKeyDown(OIS::KC_D))
        Move += Ogre::Vector3::UNIT_X;
    if(mKeyboard->isKeyDown(OIS::KC_W))
        Move += Ogre::Vector3::UNIT_Y;
    if(mKeyboard->isKeyDown(OIS::KC_S))
        Move += Ogre::Vector3::NEGATIVE_UNIT_Y;
    if(mKeyboard->isKeyDown(OIS::KC_R))
        Move += Ogre::Vector3::NEGATIVE_UNIT_Z;
    if(mKeyboard->isKeyDown(OIS::KC_F))
        Move += Ogre::Vector3::UNIT_Z;
    mCamNode->translate(Move * 8 * evt.timeSinceLastFrame, Ogre::SceneNode::TransformSpace::TS_WORLD);

    OIS::MouseState ms = mMouse->getMouseState();
	if (isMousePressed) {
		if (!ms.buttonDown(OIS::MouseButtonID::MB_Left)) {
			isMousePressed = false;
		}
	} else {
		if (ms.buttonDown(OIS::MouseButtonID::MB_Left)) {
			mWorld->setBackground("Background/Blue1");
			isMousePressed = true;
		}
	}

    // Reposition background planes
    mWorld->Update(mCamNode->getPosition());

    if(mKeyboard->isKeyDown(OIS::KC_ESCAPE))
        return false;

    return true;
}
