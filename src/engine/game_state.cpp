#include "engine/game_state.h"

#include <btBulletDynamicsCommon.h>

#include "engine/engine.h"
#include "engine/entity_manager.h"
#include "engine/serialization.h"
#include "engine/system.h"

#include "gui/CEGUIWindow.h"


#include <OgreRoot.h>

using namespace thrive;

struct GameState::Implementation {

    Implementation(
        Engine& engine,
        std::string name,
        std::vector<std::unique_ptr<System>> systems,
        Initializer initializer,
        std::string guiLayoutName
    ) : m_engine(engine),
        m_initializer(initializer),
        m_name(name),
        m_systems(std::move(systems)),
        m_guiLayoutName(guiLayoutName),
        m_guiWindow(nullptr)
    {
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
    setupSceneManager() {
#ifdef USE_OGRE2
        // TODO: configure the number of worker threads, currently always 2
        m_sceneManager = m_engine.ogreRoot()->createSceneManager(
            Ogre::ST_GENERIC, 2, Ogre::INSTANCING_CULLING_THREADED,
            m_name
        );
#else
            m_sceneManager = m_engine.ogreRoot()->createSceneManager(
                Ogre::ST_GENERIC,
                m_name
            );
#endif //USE_OGRE2

        m_sceneManager->setAmbientLight(
            Ogre::ColourValue(0.5, 0.5, 0.5)
        );
    }

    Engine& m_engine;

    EntityManager m_entityManager;

    Initializer m_initializer;

    std::string m_name;

    Ogre::SceneManager* m_sceneManager = nullptr;

    struct Physics {

        std::unique_ptr<btBroadphaseInterface> broadphase;

        std::unique_ptr<btCollisionConfiguration> collisionConfiguration;

        std::unique_ptr<btDispatcher> dispatcher;

        std::unique_ptr<btConstraintSolver> solver;

        std::unique_ptr<btDiscreteDynamicsWorld> world;

    } m_physics;

    std::vector<std::unique_ptr<System>> m_systems;

    std::string m_guiLayoutName;

    CEGUIWindow m_guiWindow;

};


luabind::scope
GameState::luaBindings() {
    using namespace luabind;
    return class_<GameState>("GameState")
        .def("name", &GameState::name)
        .def("rootGUIWindow", &GameState::rootGUIWindow)
        .def("entityManager", static_cast<EntityManager&(GameState::*)()>(&GameState::entityManager))
    ;
}


GameState::GameState(
    Engine& engine,
    std::string name,
    std::vector<std::unique_ptr<System>> systems,
    Initializer initializer,
    std::string guiLayoutName
) : m_impl(new Implementation(engine, name, std::move(systems), initializer, guiLayoutName))
{
}


GameState::~GameState() {}


void
GameState::activate() {
    m_impl->m_guiWindow.show();
    CEGUIWindow::getRootWindow().addChild(&m_impl->m_guiWindow);
    for (const auto& system : m_impl->m_systems) {
        system->activate();
    }
}


void
GameState::deactivate() {
    for (const auto& system : m_impl->m_systems) {
        system->deactivate();
    }
    m_impl->m_guiWindow.hide();
    CEGUIWindow::getRootWindow().removeChild(&m_impl->m_guiWindow);
}


Engine&
GameState::engine() {
    return m_impl->m_engine;
}


const Engine&
GameState::engine() const {
    return m_impl->m_engine;
}


EntityManager&
GameState::entityManager() {
    return m_impl->m_entityManager;
}


void
GameState::init() {
    m_impl->m_guiWindow = CEGUIWindow(m_impl->m_guiLayoutName);
    m_impl->setupPhysics();
    m_impl->setupSceneManager();
    for (const auto& system : m_impl->m_systems) {
        system->init(this);
    }
    m_impl->m_initializer();
}

const std::vector<std::unique_ptr<System>>&
GameState::systems() const {
    return m_impl->m_systems;
}

void
GameState::load(
    const StorageContainer& storage
) {
    StorageContainer entities = storage.get<StorageContainer>("entities");
    m_impl->m_entityManager.clear();
    try {
        m_impl->m_entityManager.restore(
            entities,
            m_impl->m_engine.componentFactory()
        );
    }
    catch (const luabind::error& e) {
        luabind::object error_msg(luabind::from_stack(
            e.state(),
            -1
        ));
        // TODO: Log error
        std::cerr << error_msg << std::endl;
        throw;
    }
}


std::string
GameState::name() const {
    return m_impl->m_name;
}


btDiscreteDynamicsWorld*
GameState::physicsWorld() const {
    return m_impl->m_physics.world.get();
}

CEGUIWindow
GameState::rootGUIWindow(){
    return m_impl->m_guiWindow;
}




Ogre::SceneManager*
GameState::sceneManager() const {
    return m_impl->m_sceneManager;
}


void
GameState::shutdown() {
    for (const auto& system : m_impl->m_systems) {
        system->shutdown();
    }
    m_impl->m_physics.world.reset();
    m_impl->m_engine.ogreRoot()->destroySceneManager(
        m_impl->m_sceneManager
    );
}


StorageContainer
GameState::storage() const {
    StorageContainer storage;
    StorageContainer entities;
    try {
        entities = m_impl->m_entityManager.storage(
            m_impl->m_engine.componentFactory()
        );
    }
    catch (const luabind::error& e) {
        luabind::object error_msg(luabind::from_stack(
            e.state(),
            -1
        ));
        // TODO: Log error
        std::cerr << error_msg << std::endl;
        throw;
    }
    storage.set("entities", std::move(entities));
    return storage;
}


void
GameState::update(
    int renderTime,
    int logicTime
) {
    for(auto& system : m_impl->m_systems) {
        if (system->enabled()) {
            system->update(renderTime, logicTime);
        }
    }
    m_impl->m_entityManager.processRemovals();
}
