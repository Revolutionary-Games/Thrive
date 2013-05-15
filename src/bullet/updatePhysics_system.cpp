#include "bullet/updatePhysics_system.h"

#include "bullet/bullet_engine.h"

#include <assert.h>


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
    BulletEngine* bulletEngine = dynamic_cast<BulletEngine*>(engine);
    assert(bulletEngine != nullptr && "UpdatePhysicsSystem requires a BulletEngine");
    m_impl->m_world = bulletEngine->world();
    assert(m_impl->m_world != nullptr && "World object is null. Initialize the BulletEngine first.");
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

