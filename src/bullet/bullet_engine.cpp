#include "bullet/bullet_engine.h"

#include "game.h"
#include "engine/shared_data.h"
#include "bullet/debug_drawing.h"
#include "bullet/update_physics_system.h"
#include "bullet/rigid_body_system.h"
#include "scripting/luabind.h"

#include <btBulletDynamicsCommon.h>

using namespace thrive;

struct BulletEngine::Implementation{

    Implementation()
      : m_debugSystem(std::make_shared<BulletDebugSystem>())
    {
    }

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

    std::shared_ptr<BulletDebugSystem> m_debugSystem;

    std::unique_ptr<btDispatcher> m_dispatcher;

    std::unique_ptr<btConstraintSolver> m_solver;

    std::unique_ptr<btDiscreteDynamicsWorld> m_world;

};


luabind::scope
BulletEngine::luaBindings() {
    using namespace luabind;
    return class_<BulletEngine, Engine>("BulletEngine")
        .enum_("DebugDrawModes") [
            value("DBG_NoDebug", btIDebugDraw::DBG_NoDebug),
            value("DBG_DrawWireframe", btIDebugDraw::DBG_DrawWireframe),
            value("DBG_DrawAabb", btIDebugDraw::DBG_DrawAabb),
            value("DBG_DrawFeaturesText", btIDebugDraw::DBG_DrawFeaturesText),
            value("DBG_DrawContactPoints", btIDebugDraw::DBG_DrawContactPoints),
            value("DBG_NoDeactivation", btIDebugDraw::DBG_NoDeactivation),
            value("DBG_NoHelpText", btIDebugDraw::DBG_NoHelpText),
            value("DBG_DrawText", btIDebugDraw::DBG_DrawText),
            value("DBG_ProfileTimings", btIDebugDraw::DBG_ProfileTimings),
            value("DBG_EnableSatComparison", btIDebugDraw::DBG_EnableSatComparison),
            value("DBG_DisableBulletLCP", btIDebugDraw::DBG_DisableBulletLCP),
            value("DBG_EnableCCD", btIDebugDraw::DBG_EnableCCD),
            value("DBG_DrawConstraints", btIDebugDraw::DBG_DrawConstraints),
            value("DBG_DrawConstraintLimits", btIDebugDraw::DBG_DrawConstraintLimits),
            value("DBG_FastWireframe", btIDebugDraw::DBG_FastWireframe),
            value("DBG_DrawNormals", btIDebugDraw::DBG_DrawNormals)
        ]
        .def("setDebugMode", &BulletEngine::setDebugMode)
    ;
}


BulletEngine::BulletEngine()
  : Engine("Physics"),
    m_impl(new Implementation())
{
}


BulletEngine::~BulletEngine() {}


std::shared_ptr<BulletDebugSystem>
BulletEngine::debugSystem() const {
    return m_impl->m_debugSystem;
}


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
    this->addSystem(
        "debugSystem",
        100,
        m_impl->m_debugSystem
    );
}


void
BulletEngine::setDebugMode(
    int mode
) {
    m_impl->m_debugSystem->setDebugMode(mode);
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

