#include "engine/engine.h"

#include "engine/component_collection.h"
#include "engine/entity_manager.h"
#include "engine/system.h"
#include "game.h"

// Bullet
#include "bullet/bullet_to_ogre_system.h"
#include "bullet/debug_drawing.h"
#include "bullet/on_collision.h"
#include "bullet/rigid_body_system.h"
#include "bullet/update_physics_system.h"

// Ogre
#include "ogre/camera_system.h"
#include "ogre/entity_system.h"
#include "ogre/keyboard_system.h"
#include "ogre/light_system.h"
#include "ogre/mouse_system.h"
#include "ogre/on_key.h"
#include "ogre/render_system.h"
#include "ogre/scene_node_system.h"
#include "ogre/sky_system.h"
#include "ogre/viewport_system.h"

// Scripting
#include "scripting/luabind.h"
#include "scripting/lua_state.h"
#include "scripting/on_update.h"
#include "scripting/script_initializer.h"


// Microbe
#include "microbe_stage/movement.h"

#include "util/contains.h"
#include "util/pair_hash.h"

#include <boost/algorithm/string.hpp>
#include <boost/filesystem.hpp>
#include <boost/lexical_cast.hpp>
#include <btBulletDynamicsCommon.h>
#include <chrono>
#include <forward_list>
#include <fstream>
#include <iostream>
#include <OgreConfigFile.h>
#include <OgreLogManager.h>
#include <OgreRenderWindow.h>
#include <OgreRoot.h>
#include <OgreWindowEventUtilities.h>
#include <OISInputManager.h>
#include <OISMouse.h>
#include <set>
#include <stdlib.h>
#include <unordered_map>

#include <iostream>

using namespace thrive;

#ifdef _DEBUG
    static const char* RESOURCES_CFG = "resources_d.cfg";
    static const char* PLUGINS_CFG   = "plugins_d.cfg";
#else
    static const char* RESOURCES_CFG = "resources.cfg";
    static const char* PLUGINS_CFG   = "plugins.cfg";
#endif

////////////////////////////////////////////////////////////////////////////////
// Engine
////////////////////////////////////////////////////////////////////////////////

struct Engine::Implementation : public Ogre::WindowEventListener {

    Implementation(
        Engine& engine
    ) : m_engine(engine),
        m_keyboardSystem(std::make_shared<KeyboardSystem>()),
        m_mouseSystem(std::make_shared<MouseSystem>()),
        m_viewportSystem(std::make_shared<OgreViewportSystem>())
    {
    }

    ~Implementation() {
        Ogre::WindowEventUtilities::removeWindowEventListener(
            m_graphics.renderWindow,
            this
        );
    }

    void
    addSystem(
        std::shared_ptr<System> system
    ) {
        m_systems.push_back(std::move(system));
    }

    void
    loadOgreConfig() {
        if(not (m_graphics.root->restoreConfig() or m_graphics.root->showConfigDialog()))
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
    loadScripts(
        const boost::filesystem::path& directory
    ) {
        namespace fs = boost::filesystem;
        fs::path manifestPath = directory / "manifest.txt";
        if (not fs::exists(manifestPath)) {
            return;
        }
        std::ifstream manifest(manifestPath.string());
        if (not manifest.is_open()) {
            throw std::runtime_error("Could not open manifest file: " + manifestPath.string());
        }
        std::string line;
        while(not manifest.eof()) {
            std::getline(manifest, line);
            boost::algorithm::trim(line);
            if (line.empty() or line.find("//") == 0) {
                continue;
            }
            fs::path manifestEntryPath = directory / line;
            if (not fs::exists(manifestEntryPath)) {
                std::cerr << "Warning: Could not find file " << manifestEntryPath.string() << std::endl;
                continue;
            }
            else if (fs::is_directory(manifestEntryPath)) {
                this->loadScripts(manifestEntryPath);
            }
            else if(luaL_dofile(m_luaState, manifestEntryPath.string().c_str())) {
                const char* errorMessage = lua_tostring(m_luaState, -1);
                std::cerr << "Error while parsing " << manifestEntryPath.string() << ": " << errorMessage << std::endl;
            }
        }
    }

    bool
    quitRequested() {
        return m_keyboardSystem->isKeyDown(
            OIS::KeyCode::KC_ESCAPE
        );
    }

    void
    setupGraphics() {
        this->setupLog();
        m_graphics.root.reset(new Ogre::Root(PLUGINS_CFG));
        this->loadResources();
        this->loadOgreConfig();
        m_graphics.renderWindow = m_graphics.root->initialise(true, "Thrive");
        m_mouseSystem->setWindowSize(
            m_graphics.renderWindow->getWidth(),
            m_graphics.renderWindow->getHeight()
        );
        Ogre::WindowEventUtilities::addWindowEventListener(
            m_graphics.renderWindow,
            this
        );
        // Set default mipmap level (NB some APIs ignore this)
        Ogre::TextureManager::getSingleton().setDefaultNumMipmaps(5);
        // initialise all resource groups
        Ogre::ResourceGroupManager::getSingleton().initialiseAllResourceGroups();
        // Setup
        m_graphics.sceneManager = m_graphics.root->createSceneManager(
            "DefaultSceneManager"
        );
        m_graphics.sceneManager->setAmbientLight(
            Ogre::ColourValue(0.5, 0.5, 0.5)
        );
        setupInputManager();
    }

    void
    setupInputManager() {
        const std::string HANDLE_NAME = "WINDOW";
        size_t windowHandle = 0;
        m_graphics.renderWindow->getCustomAttribute(HANDLE_NAME, &windowHandle);
        OIS::ParamList parameters;
        parameters.insert(std::make_pair(
            HANDLE_NAME,
            boost::lexical_cast<std::string>(windowHandle)
        ));
#if defined OIS_WIN32_PLATFORM
        parameters.insert(std::make_pair(std::string("w32_mouse"), std::string("DISCL_FOREGROUND" )));
        parameters.insert(std::make_pair(std::string("w32_mouse"), std::string("DISCL_NONEXCLUSIVE")));
        parameters.insert(std::make_pair(std::string("w32_keyboard"), std::string("DISCL_FOREGROUND")));
        parameters.insert(std::make_pair(std::string("w32_keyboard"), std::string("DISCL_NONEXCLUSIVE")));
#elif defined OIS_LINUX_PLATFORM
        parameters.insert(std::make_pair(std::string("x11_mouse_grab"), std::string("false")));
        parameters.insert(std::make_pair(std::string("x11_mouse_hide"), std::string("false")));
        parameters.insert(std::make_pair(std::string("x11_keyboard_grab"), std::string("false")));
        parameters.insert(std::make_pair(std::string("XAutoRepeatOn"), std::string("true")));
#endif
        m_inputManager = OIS::InputManager::createInputSystem(parameters);
    }

    void
    setupLog() {
        static Ogre::LogManager logManager;
        logManager.createLog("default", true, false, false);
    }

    void
    setupPhysics() {
        m_physics.collisionConfiguration.reset(new btDefaultCollisionConfiguration());
        m_physics.dispatcher.reset(new btCollisionDispatcher(
            m_physics.collisionConfiguration.get()
        ));
        m_physics.broadphase.reset(new btDbvtBroadphase());
        m_physics.solver.reset(new btSequentialImpulseConstraintSolver());
        m_physics.world.reset(new btDiscreteDynamicsWorld(
            m_physics.dispatcher.get(),
            m_physics.broadphase.get(),
            m_physics.solver.get(),
            m_physics.collisionConfiguration.get()
        ));
        m_physics.world->setGravity(btVector3(0,0,0));
    }

    void
    setupScripts() {
        initializeLua(m_luaState);
    }

    void
    setupSystems() {
        std::shared_ptr<System> systems[] = {
            // Input
            m_keyboardSystem,
            m_mouseSystem,
            // Scripts
            std::make_shared<OnUpdateSystem>(),
            std::make_shared<OnKeySystem>(),
            // Microbe
            std::make_shared<MicrobeMovementSystem>(),
            // Physics
            std::make_shared<RigidBodyInputSystem>(),
            std::make_shared<UpdatePhysicsSystem>(),
            std::make_shared<RigidBodyOutputSystem>(),
            std::make_shared<BulletToOgreSystem>(),
            std::make_shared<OnCollisionSystem>(),
            std::make_shared<BulletDebugDrawSystem>(),
            // Graphics
            std::make_shared<OgreAddSceneNodeSystem>(),
            std::make_shared<OgreUpdateSceneNodeSystem>(),
            std::make_shared<OgreCameraSystem>(),
            std::make_shared<OgreLightSystem>(),
            std::make_shared<SkySystem>(),
            std::make_shared<OgreEntitySystem>(),
            m_viewportSystem, // Has to come *after* camera system
            std::make_shared<OgreRemoveSceneNodeSystem>(),
            std::make_shared<RenderSystem>()
        };
        for (auto system : systems) {
            this->addSystem(system);
        }
    }

    void
    shutdownInputManager() {
        if (not m_inputManager) {
            return;
        }
        OIS::InputManager::destroyInputSystem(m_inputManager);
        m_inputManager = nullptr;
    }

    bool 
    windowClosing(
        Ogre::RenderWindow* window
    ) override {
        if (window == m_graphics.renderWindow) {
            Game::instance().quit();
        }
        return true;
    }

    void
    windowResized(
        Ogre::RenderWindow* window
    ) override {
        if (window == m_graphics.renderWindow) {
            m_mouseSystem->setWindowSize(
                window->getWidth(),
                window->getHeight()
            );
        }
    }

    // Lua state must be one of the last to be destroyed, so keep it at top. 
    // The reason for that is that some components keep luabind::object 
    // instances around that rely on the lua state to still exist when they
    // are destroyed. Since those components are destroyed with the entity 
    // manager, the lua state has to live longer than the manager.
    LuaState m_luaState;

    Engine& m_engine;

    EntityManager m_entityManager;

    struct Graphics {

        Ogre::SceneManager* sceneManager = nullptr;

        std::unique_ptr<Ogre::Root> root;

        Ogre::RenderWindow* renderWindow = nullptr;

    } m_graphics;

    OIS::InputManager* m_inputManager = nullptr;

    std::shared_ptr<KeyboardSystem> m_keyboardSystem;

    std::shared_ptr<MouseSystem> m_mouseSystem;

    struct Physics {

        std::unique_ptr<btBroadphaseInterface> broadphase;

        std::unique_ptr<btCollisionConfiguration> collisionConfiguration;

        std::unique_ptr<btDispatcher> dispatcher;

        std::unique_ptr<btConstraintSolver> solver;

        std::unique_ptr<btDiscreteDynamicsWorld> world;

    } m_physics;

    std::list<std::shared_ptr<System>> m_systems;

    std::shared_ptr<OgreViewportSystem> m_viewportSystem;

};




Engine::Engine() 
  : m_impl(new Implementation(*this))
{
}


Engine::~Engine() { }


EntityManager&
Engine::entityManager() {
    return m_impl->m_entityManager;
}


void
Engine::init() {
    m_impl->setupPhysics();
    m_impl->setupScripts();
    m_impl->setupGraphics();
    m_impl->setupSystems();
    m_impl->loadScripts("../scripts");
    for (auto& system : m_impl->m_systems) {
        system->init(this);
    }
}


OIS::InputManager*
Engine::inputManager() const {
    return m_impl->m_inputManager;
}


KeyboardSystem&
Engine::keyboardSystem() const {
    return *m_impl->m_keyboardSystem;
}


MouseSystem&
Engine::mouseSystem() const {
    return *m_impl->m_mouseSystem;
}


Ogre::Root*
Engine::ogreRoot() const {
    return m_impl->m_graphics.root.get();
}

btDiscreteDynamicsWorld*
Engine::physicsWorld() const {
    return m_impl->m_physics.world.get();
}

Ogre::RenderWindow*
Engine::renderWindow() const {
    return m_impl->m_graphics.renderWindow;
}


Ogre::SceneManager*
Engine::sceneManager() const {
    return m_impl->m_graphics.sceneManager;
}


void
Engine::shutdown() {
    for (auto& system : m_impl->m_systems) {
        system->shutdown();
    }
    m_impl->shutdownInputManager();
    m_impl->m_graphics.renderWindow->destroy();
    m_impl->m_graphics.root.reset();
}


void
Engine::update(
    int milliSeconds
) {
    Ogre::WindowEventUtilities::messagePump();
    if (m_impl->quitRequested()) {
        Game::instance().quit();
    }
    for(auto& system : m_impl->m_systems) {
        system->update(milliSeconds);
    }
    m_impl->m_entityManager.processRemovals();
}

OgreViewportSystem&
Engine::viewportSystem() {
    return *(m_impl->m_viewportSystem);
}


