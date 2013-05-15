#include "bullet/bullet_engine.h"

#include "game.h"
#include "engine/shared_data.cpp"

#include <iostream>

using namespace thrive;

struct BulletEngine::Implementation{

    void
    setupBroadphase() {
        m_broadphase = new btDbvtBroadphase();
    }

    void
    setupColisions() {
        m_collisionConfiguration = new btDefaultCollisionConfiguration();
        m_dispatcher = new btCollisionDispatcher(m_collisionConfiguration);
    }

    void
    setupSolver() {
        m_solver = new btSequentialImpulseConstraintSolver;
    }

    void
    setupWorld() {
        m_world.reset(new btDiscreteDynamicsWorld(m_dispatcher,m_broadphase,m_solver,m_collisionConfiguration));
    }

    std::unique_ptr<btDiscreteDynamicsWorld> m_world;

    btSequentialImpulseConstraintSolver* m_solver = nullptr;

    btCollisionDispatcher* m_dispatcher = nullptr;

    btDefaultCollisionConfiguration* m_collisionConfiguration = nullptr;

    btBroadphaseInterface* m_broadphase = nullptr;
};


BulletEngine::BulletEngine()
  : Engine(),
    m_impl(new Implementation())
{
}


BulletEngine::~BulletEngine() {}


void
BulletEngine::init(
    EntityManager* entityManager
) {
    Engine::init(entityManager);

    // Create essential systems
    /*this->addSystem(
        "keyboard",
        -100,
        m_impl->m_keyboardSystem
    );*/
}


void
BulletEngine::shutdown() {
    Engine::shutdown();
}


void
BulletEngine::update() {
    // Lock shared state
    StateLock<PhysicUpdateState, StateBuffer::WorkingCopy> physicsUpdateLock;
    // Handle events

    // Update systems
    Engine::update();
    // Release shared state
}

btDiscreteDynamicsWorld*
BulletEngine::world() const {
    return m_impl->m_world.get();
}

btSequentialImpulseConstraintSolver*
BulletEngine::solver() const {
    return m_impl->m_solver;
}

btCollisionDispatcher*
BulletEngine::dispatcher() const {
    return m_impl->m_dispatcher;
}

btDefaultCollisionConfiguration*
BulletEngine::collisionConfiguration() const {
    return m_impl->m_collisionConfiguration;
}

btBroadphaseInterface*
BulletEngine::broadphase() const {
    return m_impl->m_broadphase;
}

