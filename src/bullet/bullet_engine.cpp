#include "bullet/bullet_engine.h"

#include "game.h"
#include "engine/shared_data.h"
#include "bullet/update_physics_system.h"
#include "bullet/rigid_body_system.h"

#include <btBulletDynamicsCommon.h>

using namespace thrive;

struct BulletEngine::Implementation{

    void
    setupWorld() {
        m_collisionConfiguration.reset(new btDefaultCollisionConfiguration());
        m_dispatcher.reset(new btCollisionDispatcher(
            m_collisionConfiguration.get()
        ));
        m_broadphase.reset(new btDbvtBroadphase());
        m_solver.reset(new btSequentialImpulseConstraintSolver());
        m_world.reset(new btDiscreteDynamicsWorld(
            m_dispatcher.get(),
            m_broadphase.get(),
            m_solver.get(),
            m_collisionConfiguration.get()
        ));
        m_world->setGravity(btVector3(0,0,0));
    }

    std::unique_ptr<btBroadphaseInterface> m_broadphase;

    std::unique_ptr<btCollisionConfiguration> m_collisionConfiguration;

    std::unique_ptr<btDispatcher> m_dispatcher;

    std::unique_ptr<btConstraintSolver> m_solver;

    std::unique_ptr<btDiscreteDynamicsWorld> m_world;

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
    m_impl->setupWorld();
    // Create essential systems
    this->addSystem(
        "rigidBodyInputSystem",
        -10,
        std::make_shared<RigidBodyInputSystem>()
    );
    this->addSystem(
        "updatePhysics",
        0,
        std::make_shared<UpdatePhysicsSystem>()
    );
    this->addSystem(
        "rigidBodyOutputSystem",
        10,
        std::make_shared<RigidBodyOutputSystem>()
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

