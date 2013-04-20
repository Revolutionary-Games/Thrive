#include "ogre/ogre_engine.h"

#include <OgreConfigFile.h>
#include <OgreRenderWindow.h>
#include <OgreRoot.h>
#include <OgreWindowEventUtilities.h>
#include <OISInputManager.h>
#include <OISKeyboard.h>
#include <OISMouse.h>
#include <stdlib.h>

using namespace thrive;

#ifdef _DEBUG
    static const char* RESOURCES_CFG = "resources_d.cfg";
    static const char* PLUGINS_CFG   = "plugins_d.cfg";
#else
    static const char* RESOURCES_CFG = "resources.cfg";
    static const char* PLUGINS_CFG   = "plugins.cfg";
#endif

struct OgreEngine::Implementation : public Ogre::WindowEventListener {

    ~Implementation() {
        Ogre::WindowEventUtilities::removeWindowEventListener(
            m_window, 
            this
        );
    }

    void
    loadConfig() {
        if(!(m_root->restoreConfig() or m_root->showConfigDialog()))
        {
            exit(EXIT_SUCCESS);
        }
    }

    void
    loadResources() {
        Ogre::ConfigFile config;
        config.load(RESOURCES_CFG);
        auto sectionIter = config.getSectionIterator();
        auto& resourceManager = Ogre::ResourceGroupManager::getSingleton();
        while (sectionIter.hasMoreElements()) {
            std::string sectionName = sectionIter.peekNextKey();
            Ogre::ConfigFile::SettingsMultiMap* sectionContent = sectionIter.getNext();
            for(auto& setting : *sectionContent) {
                std::string resourceType = setting.first;
                std::string resourceLocation = setting.second;
                resourceManager.addResourceLocation(
                    resourceLocation,
                    resourceType,
                    sectionName
                );
            }
        }
    }

    void
    setupCamera() {
        m_camera = m_sceneManager->createCamera("PlayerCam");
        m_camera->setNearClipDistance(5);
        m_camera->setFarClipDistance(2000);
        m_camera->setAutoAspectRatio(true);
        // Create node
        m_cameraNode = m_sceneManager->getRootSceneNode()->createChildSceneNode(
                "MainCameraNode", 
                Ogre::Vector3(0,0,30),
                Ogre::Quaternion::IDENTITY
        );
        m_cameraNode->attachObject(m_camera);
    }

    void
    setupInputManager() {
        const std::string HANDLE_NAME = "WINDOW";
        size_t windowHandle = 0;
        m_window->getCustomAttribute(HANDLE_NAME, &windowHandle);
        OIS::ParamList parameters;
        parameters.insert(std::make_pair(
            HANDLE_NAME, 
            std::to_string(windowHandle)
        ));
        m_inputManager = OIS::InputManager::createInputSystem(parameters);
        // Keyboard
        m_keyboard = static_cast<OIS::Keyboard*>(
            m_inputManager->createInputObject( OIS::OISKeyboard, false)
        );
        // Mouse
        m_mouse = static_cast<OIS::Mouse*>(
            m_inputManager->createInputObject( OIS::OISMouse, false)
        );
    }

    void
    setupLighting() {
        m_sceneManager->setAmbientLight(Ogre::ColourValue(0.5, 0.5, 0.5));
        Ogre::Light* light = m_sceneManager->createLight("MainLight");
        light->setPosition(0,0,10);
    }

    void
    setupSceneManager() {
        m_sceneManager = m_root->createSceneManager("DefaultSceneManager");
    }

    void
    setupViewport() {
        Ogre::Viewport* viewport = m_window->addViewport(m_camera);
        viewport->setBackgroundColour(Ogre::ColourValue(0,0,0));
        m_camera->setAutoAspectRatio(true);
    }

    void
    shutdownInputManager() {
        if (not m_inputManager) {
            return;
        }
        m_inputManager->destroyInputObject(m_mouse);
        m_inputManager->destroyInputObject(m_keyboard);
        OIS::InputManager::destroyInputSystem(m_inputManager);
        m_inputManager = nullptr;
    }

    void windowClosed(
        Ogre::RenderWindow* window
    ) {
        if (window == m_window) {
            this->shutdownInputManager();
        }
    }

    void windowResized(
        Ogre::RenderWindow* window
    ) {
        unsigned int width, height, colourDepth;
        int top, left;
        window->getMetrics(width, height, colourDepth, top, left);
        const OIS::MouseState &mouseState = m_mouse->getMouseState();
        mouseState.width = width;
        mouseState.height = height;
    }

    std::unique_ptr<Ogre::Root> m_root;

    Ogre::Camera* m_camera = nullptr;

    Ogre::SceneNode* m_cameraNode = nullptr;

    OIS::InputManager* m_inputManager = nullptr;

    OIS::Keyboard* m_keyboard = nullptr;

    OIS::Mouse* m_mouse = nullptr;

    Ogre::SceneManager* m_sceneManager = nullptr;

    Ogre::RenderWindow* m_window = nullptr;

};


OgreEngine::OgreEngine()
  : m_impl(new Implementation())
{
}


OgreEngine::~OgreEngine() {}


void
OgreEngine::init() {
    Engine::init();
    m_impl->m_root.reset(new Ogre::Root(PLUGINS_CFG));
    m_impl->loadResources();
    m_impl->loadConfig();
    m_impl->m_window = m_impl->m_root->initialise(true, "Thrive");
    // Set default mipmap level (NB some APIs ignore this)
    Ogre::TextureManager::getSingleton().setDefaultNumMipmaps(5);
    // initialise all resource groups
    Ogre::ResourceGroupManager::getSingleton().initialiseAllResourceGroups();
    // Setup
    m_impl->setupSceneManager();
    m_impl->setupCamera();
    m_impl->setupViewport();
    m_impl->setupLighting();
    m_impl->setupInputManager();
}


OIS::Keyboard*
OgreEngine::keyboard() const {
    return m_impl->m_keyboard;
}


Ogre::SceneManager*
OgreEngine::sceneManager() const {
    return m_impl->m_sceneManager;
}


void
OgreEngine::shutdown() {
    m_impl->shutdownInputManager();
    m_impl->m_window->destroy();
    m_impl->m_root.reset();
    Engine::shutdown();
}


Ogre::RenderWindow*
OgreEngine::window() const {
    return m_impl->m_window;
}


