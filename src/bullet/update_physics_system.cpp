#include "bullet/update_physics_system.h"

#include "engine/engine.h"

#include <assert.h>
#include <btBulletDynamicsCommon.h>


using namespace thrive;

struct UpdatePhysicsSystem::Implementation {

    btDiscreteDynamicsWorld* m_world;

};


UpdatePhysicsSystem::UpdatePhysicsSystem()
  : m_impl(new Implementation())
{
}


UpdatePhysicsSystem::~UpdatePhysicsSystem() {}


void
UpdatePhysicsSystem::init(
    Engine* engine
) {
    System::init(engine);
    m_impl->m_world = engine->physicsWorld();
    assert(m_impl->m_world != nullptr && "World object is null. Initialize the Engine first.");
}


void
UpdatePhysicsSystem::shutdown() {
    m_impl->m_world = nullptr;
    System::shutdown();
}


void
UpdatePhysicsSystem::update(
    int milliSeconds
) {
    assert(m_impl->m_world != nullptr && "UpdatePhysicsSystem not initialized");
    m_impl->m_world->stepSimulation(milliSeconds/1000.f,10);
}

