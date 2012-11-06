#include "BasicTutorial6.h"
 
#include <OgreException.h>
#include <OgreConfigFile.h>

#include <OgreCamera.h>
#include <OgreSceneManager.h>
#include <OgreRenderWindow.h>
#include <OgreEntity.h>

#include <OgreWindowEventUtilities.h>

//-------------------------------------------------------------------------------------
BasicTutorial6::BasicTutorial6(void)
        :mRoot(0),
        mPluginsCfg(Ogre::StringUtil::BLANK),
        mResourcesCfg(Ogre::StringUtil::BLANK)
{    
}
//-------------------------------------------------------------------------------------
BasicTutorial6::~BasicTutorial6(void)
{
    Ogre::WindowEventUtilities::removeWindowEventListener(mWindow, this);
    windowClosed(mWindow);
    delete mRoot;
}
 
bool BasicTutorial6::go(void)
{

#ifdef NDEBUG
    mPluginsCfg = "plugins.cfg";
    mResourcesCfg = "resources.cfg";
#else
    mResourcesCfg = "resources_d.cfg";
    mPluginsCfg = "plugins_d.cfg";
#endif
    
    // construct Ogre::Root
    mRoot = new Ogre::Root(mPluginsCfg);
    
    // Setup resources
    Ogre::ConfigFile cf;
    cf.load(mResourcesCfg);
    
    Ogre::ConfigFile::SectionIterator seci = cf.getSectionIterator();
    
    Ogre::String secName, typeName, archName;
    while(seci.hasMoreElements())
    {
        secName = seci.peekNextKey();
        Ogre::ConfigFile::SettingsMultiMap *settings = seci.getNext();
        Ogre::ConfigFile::SettingsMultiMap::iterator i;
        for(i = settings->begin(); i != settings->end(); ++i)
        {
            typeName = i->first;
            archName = i->second;
            Ogre::ResourceGroupManager::getSingleton().addResourceLocation(
                    archName, typeName, secName);
        }
    }
    
    // configure gfx settings
    if(!(mRoot->restoreConfig() || mRoot->showConfigDialog()))
    {    return false;  }
    /* optional timesaver for development: 
     Ogre::RenderSystem *rs = mRoot->getRenderSystemByName("OpenGL Rendering Subsystem");
     mRoot->setRenderSystem(rs);
     rs->setConfigOption("Full Screen", "No");
     rs->setConfigOption("Video Mode", "800 x 600 @ 32-bit colour"); */
    
    // Render window
    mWindow = mRoot->initialise(true, "BasicTutorial 6");
    
    // Default mipmap level
    Ogre::TextureManager::getSingleton().setDefaultNumMipmaps(5);
    // initialise all resource groups
    Ogre::ResourceGroupManager::getSingleton().initialiseAllResourceGroups();
    
    // Create scenemanager
    mSceneMgr = mRoot->createSceneManager("DefaultSceneManager");
    
    // Create the camera
    mCamera = mSceneMgr->createCamera("PlayerCam");
    mCamera->setPosition(Ogre::Vector3(0,0,80));
    mCamera->lookAt(Ogre::Vector3(0,0,-300));
    mCamera->setNearClipDistance(5);
    
    // Viewport
    Ogre::Viewport* vp = mWindow->addViewport(mCamera);
    vp->setBackgroundColour(Ogre::ColourValue(0,0,0));
    
    mCamera->setAspectRatio(
        Ogre::Real(vp->getActualWidth()) / Ogre::Real(vp->getActualHeight()));
    
    // scene
    Ogre::Entity* ogreHead = mSceneMgr->createEntity("Head", "ogrehead.mesh");
    
    Ogre::SceneNode* headNode = mSceneMgr->getRootSceneNode()->createChildSceneNode();
    headNode->attachObject(ogreHead);    
    
    // ambient light
    mSceneMgr->setAmbientLight(Ogre::ColourValue(0.5,0.5,0.5));
    
    // light
    Ogre::Light* l = mSceneMgr->createLight("MainLight");
    l->setPosition(20,80,50);
        
    Ogre::LogManager::getSingletonPtr()->logMessage("*** Initializing OIS ***");
    OIS::ParamList pl;
    size_t windowHnd = 0;
    std::ostringstream windowHndStr;
    
    mWindow->getCustomAttribute("WINDOW", &windowHnd);
    windowHndStr << windowHnd;
    pl.insert(std::make_pair(std::string("WINDOW"), windowHndStr.str()));
    
    mInputManager = OIS::InputManager::createInputSystem(pl);
    
    mKeyboard = static_cast<OIS::Keyboard*>(mInputManager->createInputObject( OIS::OISKeyboard, false ));
    mMouse = static_cast<OIS::Mouse*>(mInputManager->createInputObject( OIS::OISMouse, false ));
    
    // set initial mouse clipping
    windowResized(mWindow);
    // register window listener
    Ogre::WindowEventUtilities::addWindowEventListener(mWindow,this);
    
    mRoot->addFrameListener(this);
    mRoot->startRendering();
    
    return true;
}

/// Adjust mouse clipping area
void BasicTutorial6::windowResized(Ogre::RenderWindow* rw)
{
    unsigned int width, height, depth;
    int left, top;
    rw->getMetrics(width, height, depth, left, top);
    
    const OIS::MouseState &ms = mMouse->getMouseState();
    ms.width = width;
    ms.height = height;
}

// Unattach OIS before window shutdown (very important in linux)
void BasicTutorial6::windowClosed(Ogre::RenderWindow* rw)
{
    // only close for window that created OIS
    if(rw == mWindow)
    {
        if(mInputManager)
        {
            mInputManager->destroyInputObject(mMouse);
            mInputManager->destroyInputObject(mKeyboard);
            
            OIS::InputManager::destroyInputSystem(mInputManager);
            mInputManager = 0;
        }
    }
}


bool BasicTutorial6::frameRenderingQueued(const Ogre::FrameEvent& evt)
{
    if(mWindow->isClosed())
        return false;
    
    // need to capture each device
    mKeyboard->capture();
    mMouse->capture();
    
    if(mKeyboard->isKeyDown(OIS::KC_ESCAPE))
        return false;
    
    return true;
}
 
#if OGRE_PLATFORM == OGRE_PLATFORM_WIN32
#define WIN32_LEAN_AND_MEAN
#include "windows.h"
#endif
 
#ifdef __cplusplus
extern "C" {
#endif
 
#if OGRE_PLATFORM == OGRE_PLATFORM_WIN32
    INT WINAPI WinMain( HINSTANCE hInst, HINSTANCE, LPSTR strCmdLine, INT )
#else
    int main(int argc, char *argv[])
#endif
    {
        // Create application object
        BasicTutorial6 app;
 
        try {
            app.go();
        } catch( Ogre::Exception& e ) {
            // possibly delete ogre.cfg?
#if OGRE_PLATFORM == OGRE_PLATFORM_WIN32
            MessageBox( NULL, e.getFullDescription().c_str(), "An exception has occured!", MB_OK | MB_ICONERROR | MB_TASKMODAL);
#else
            std::cerr << "An exception has occured: " <<
                e.getFullDescription().c_str() << std::endl;
#endif
        }
 
        return 0;
    }
 
#ifdef __cplusplus
}
#endif