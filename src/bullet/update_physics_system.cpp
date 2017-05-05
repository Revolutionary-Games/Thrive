#include "bullet/update_physics_system.h"


#include "bullet/physical_world.h"
#include "engine/game_state.h"
#include "scripting/luajit.h"

#include <assert.h>
#include <btBulletDynamicsCommon.h>


using namespace thrive;

void UpdatePhysicsSystem::luaBindings(
    sol::state &lua
){
    lua.new_usertype<UpdatePhysicsSystem>("UpdatePhysicsSystem",

        sol::constructors<sol::types<>>(),
        
        sol::base_classes, sol::bases<System>(),

        "init", &UpdatePhysicsSystem::init
    );
}

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
    GameStateData* gameState
) {
    System::initNamed("UpdatePhysicsSystem", gameState);
    m_impl->m_world = gameState->physicalWorld()->physicsWorld();
    assert(m_impl->m_world != nullptr && "World object is null. Initialize the Engine first.");
}


void
UpdatePhysicsSystem::shutdown() {
    m_impl->m_world = nullptr;
    System::shutdown();
}


void
UpdatePhysicsSystem::update(
    int,
    int logicTime
) {
    assert(m_impl->m_world != nullptr && "UpdatePhysicsSystem not initialized");
    m_impl->m_world->stepSimulation(logicTime/1000.f,10);
}

