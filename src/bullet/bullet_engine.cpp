#include "bullet/bullet_engine.h"

#include "game.h"
#include "engine/shared_data.h"
#include "bullet/update_physics_system.h"
#include "bullet/rigid_body_bindings.h"

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
    m_impl->setupBroadphase();
    m_impl->setupColisions();
    m_impl->setupSolver();
    m_impl->setupWorld();
    // Create essential systems
    this->addSystem(
        "updatePhysics",
        -1000,
        std::make_shared<UpdatePhysicsSystem>()
    );
    this->addSystem(
        "rigidBodyBindings",
        1,
        std::make_shared<RigidBodyInputSystem>()
    );
}


void
BulletEngine::shutdown() {
    Engine::shutdown();
}


void
BulletEngine::update() {
    // Lock shared state
    StateLock<PhysicsOutputState, StateBuffer::WorkingCopy> physicsOutputLock;
    StateLock<PhysicsInputState, StateBuffer::Stable> physicsInputLock;
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

