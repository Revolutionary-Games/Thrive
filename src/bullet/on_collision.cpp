#include "bullet/on_collision.h"

#include "engine/component_registry.h"
#include "engine/engine.h"
#include "engine/entity.h"
#include "engine/entity_filter.h"
#include "game.h"

#include <btBulletDynamicsCommon.h>

using namespace thrive;

luabind::scope
OnCollisionComponent::luaBindings() {
    using namespace luabind;
    return 
        class_<OnCollisionComponent, Component, std::shared_ptr<Component>>("OnCollisionComponent")
            .scope[
                def("TYPE_NAME", &OnCollisionComponent::TYPE_NAME),
                def("TYPE_ID", &OnCollisionComponent::TYPE_ID)
            ]
            .def(constructor<>())
            .def_readwrite("onCollision", &OnCollisionComponent::onCollisionCallback)
    ;
}

REGISTER_COMPONENT(OnCollisionComponent)

////////////////////////////////////////////////////////////////////////////////
// OnCollisionSystem
////////////////////////////////////////////////////////////////////////////////

struct OnCollisionSystem::Implementation {

    EntityManager* m_entityManager = nullptr;

    btDiscreteDynamicsWorld* m_world = nullptr;

};


OnCollisionSystem::OnCollisionSystem() 
  : m_impl(new Implementation())
{
}


OnCollisionSystem::~OnCollisionSystem() {}


void
OnCollisionSystem::init(
    Engine* engine
) {
    System::init(engine);
    m_impl->m_entityManager = &engine->entityManager();
    m_impl->m_world = engine->physicsWorld();
}


void
OnCollisionSystem::shutdown() {
    m_impl->m_entityManager = nullptr;
    m_impl->m_world = nullptr;
    System::shutdown();
}


void
OnCollisionSystem::update(
    int
) {
    auto dispatcher = m_impl->m_world->getDispatcher();
    int numManifolds = dispatcher->getNumManifolds();
    for (int i = 0; i < numManifolds; i++) {
        btPersistentManifold* contactManifold = dispatcher->getManifoldByIndexInternal(i);
        auto objectA = static_cast<const btCollisionObject*>(contactManifold->getBody0());
        auto objectB = static_cast<const btCollisionObject*>(contactManifold->getBody1());
        EntityId entityA = reinterpret_cast<size_t>(objectA->getUserPointer());
        EntityId entityB = reinterpret_cast<size_t>(objectB->getUserPointer());
        auto onCollisionA = m_impl->m_entityManager->getComponent<OnCollisionComponent>(entityA);
        auto onCollisionB = m_impl->m_entityManager->getComponent<OnCollisionComponent>(entityB);
        if (onCollisionA and onCollisionA->onCollisionCallback.is_valid()) {
            try {
                onCollisionA->onCollisionCallback(
                    Entity(entityA), 
                    Entity(entityB)
                );
            }
            catch(const luabind::error& e) {
                luabind::object error_msg(luabind::from_stack(
                    e.state(),
                    -1
                ));
                // TODO: Log error
                std::cerr << error_msg << std::endl;
            }
            catch(const std::exception& e) {
                std::cerr << "Unexpected exception during Lua callback:" << e.what() << std::endl;
            }
        }
        if (onCollisionB and onCollisionB->onCollisionCallback.is_valid()) {
            try {
                onCollisionB->onCollisionCallback(
                    Entity(entityB), 
                    Entity(entityA)
                );
            }
            catch(const luabind::error& e) {
                luabind::object error_msg(luabind::from_stack(
                    e.state(),
                    -1
                ));
                // TODO: Log error
                std::cerr << error_msg << std::endl;
            }
            catch(const std::exception& e) {
                std::cerr << "Unexpected exception during Lua callback:" << e.what() << std::endl;
            }
        }
    }
}





