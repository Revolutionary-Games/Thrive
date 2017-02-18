#include "physical_world.h"

#include "scripting/luajit.h"

#include <btBulletDynamicsCommon.h>

using namespace thrive;


void PhysicalWorld::luaBindings(sol::state &lua){

    lua.new_usertype<PhysicalWorld>("PhysicalWorld",

        sol::constructors<sol::types<>>()
    );
    
}


struct PhysicalWorld::PhysicsConfiguration{

    std::unique_ptr<btBroadphaseInterface> broadphase;

    std::unique_ptr<btCollisionConfiguration> collisionConfiguration;

    std::unique_ptr<btDispatcher> dispatcher;

    std::unique_ptr<btConstraintSolver> solver;

    std::unique_ptr<btDiscreteDynamicsWorld> world;
};

PhysicalWorld::PhysicalWorld(){

    m_physics = std::make_unique<PhysicsConfiguration>();

    m_physics->collisionConfiguration.reset(new btDefaultCollisionConfiguration());
    m_physics->dispatcher.reset(new btCollisionDispatcher(
            m_physics->collisionConfiguration.get()
        ));
    m_physics->broadphase.reset(new btDbvtBroadphase());
    m_physics->solver.reset(new btSequentialImpulseConstraintSolver());
    m_physics->world.reset(new btDiscreteDynamicsWorld(
            m_physics->dispatcher.get(),
            m_physics->broadphase.get(),
            m_physics->solver.get(),
            m_physics->collisionConfiguration.get()
        ));
    m_physics->world->setGravity(btVector3(0,0,0));
}

