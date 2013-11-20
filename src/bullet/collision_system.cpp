#include "bullet/collision_system.h"

#include "scripting/luabind.h"

#include "bullet/collision_filter.h"
#include "engine/component_factory.h"
#include "engine/engine.h"
#include "engine/entity.h"
#include "engine/entity_manager.h"
#include "engine/serialization.h"
#include "bullet/rigid_body_system.h"
#include <unordered_map>

#include "util/pair_hash.h"


using namespace thrive;

////////////////////////////////////////////////////////////////////////////////
// CollisionComponent
////////////////////////////////////////////////////////////////////////////////

CollisionComponent::CollisionComponent(){}


CollisionComponent::CollisionComponent(
    const std::string& collisionGroup
) : m_collisionGroups({collisionGroup})
{
}

luabind::scope
CollisionComponent::luaBindings() {
    using namespace luabind;
    return class_<CollisionComponent, Component>("CollisionComponent")
        .enum_("ID") [
            value("TYPE_ID", CollisionComponent::TYPE_ID)
        ]
        .scope [
            def("TYPE_NAME", &CollisionComponent::TYPE_NAME)
        ]
        .def(constructor<>())
        .def(constructor<const std::string&>())
        .def("addCollisionGroup", &CollisionComponent::addCollisionGroup)
    ;
}

void
CollisionComponent::addCollisionGroup(
    const std::string& group
) {
    m_collisionGroups.push_back(group);
}

void
CollisionComponent::removeCollisionGroup(
    const std::string& group
) {
    m_collisionGroups.erase(std::remove(m_collisionGroups.begin(), m_collisionGroups.end(), group), m_collisionGroups.end());
}

const std::vector<std::string>&
CollisionComponent::getCollisionGroups() {
    return m_collisionGroups;
}

void
CollisionComponent::load(
    const StorageContainer& storage
) {
    Component::load(storage);
    StorageList collisionGroups = storage.get<StorageList>("collisionGroups");
    m_collisionGroups.reserve(collisionGroups.size());
    for (const StorageContainer& container : collisionGroups) {
        std::string collisionGroup = container.get<std::string>("collisionGroup");
        m_collisionGroups.push_back(collisionGroup);
    }
}


StorageContainer
CollisionComponent::storage() const {
    StorageContainer storage = Component::storage();

    StorageList collisionGroups;
    collisionGroups.reserve(m_collisionGroups.size());
    for (std::string collisionGroup : m_collisionGroups) {
        StorageContainer container;
        container.set<std::string>("collisionGroup", collisionGroup);
        collisionGroups.append(container);
    }
    storage.set<StorageList>("collisionGroups", collisionGroups);
    return storage;
}

REGISTER_COMPONENT(CollisionComponent)


////////////////////////////////////////////////////////////////////////////////
// Collision
////////////////////////////////////////////////////////////////////////////////


Collision::Collision(
    EntityId entityId1,
    EntityId entityId2,
    int addedCollisionDuration
) : entityId1(entityId1),
    entityId2(entityId2),
    addedCollisionDuration(addedCollisionDuration)
{
}

luabind::scope
Collision::luaBindings() {
    using namespace luabind;
    return class_<Collision>("Collision")
        .def(constructor<EntityId, EntityId, int>())
        .def_readonly("entityId1", &Collision::entityId1)
        .def_readonly("entityId2", &Collision::entityId2)
        .def_readonly("addedCollisionDuration", &Collision::addedCollisionDuration)
    ;
}


////////////////////////////////////////////////////////////////////////////////
// CollisionSystem
////////////////////////////////////////////////////////////////////////////////


struct CollisionSystem::Implementation {

    btDiscreteDynamicsWorld* m_world = nullptr;

    std::unordered_multimap<CollisionFilter::Signature, CollisionFilter&>  m_collisionFilterMap;

};


CollisionSystem::CollisionSystem()
  : m_impl(new Implementation())
{
}


CollisionSystem::~CollisionSystem() {}


luabind::scope
CollisionSystem::luaBindings() {
    using namespace luabind;
    return class_<CollisionSystem, System>("CollisionSystem")
        .def(constructor<>())
    ;
}


void
CollisionSystem::init(
    GameState* gameState
) {
    System::init(gameState);
    m_impl->m_world = gameState->physicsWorld();
}


void
CollisionSystem::shutdown() {
    System::shutdown();
    m_impl->m_world = nullptr;
}

void
CollisionSystem::update(int milliseconds) {
    auto dispatcher = m_impl->m_world->getDispatcher();
    int numManifolds = dispatcher->getNumManifolds();

    for (int i=0;i<numManifolds;i++)
    {

        btPersistentManifold* contactManifold = dispatcher->getManifoldByIndexInternal(i);
        auto objectA = static_cast<const btCollisionObject*>(contactManifold->getBody0());
        auto objectB = static_cast<const btCollisionObject*>(contactManifold->getBody1());
        EntityId entityId1 = (reinterpret_cast<uintptr_t>(objectA->getUserPointer()));
        EntityId entityId2 = (reinterpret_cast<uintptr_t>(objectB->getUserPointer()));
        CollisionComponent* collisionComponent1 = static_cast<CollisionComponent*>(
                                            System::gameState()->entityManager().getComponent(entityId1, CollisionComponent::TYPE_ID)
                                        );
        CollisionComponent* collisionComponent2 = static_cast<CollisionComponent*>(
                                            System::gameState()->entityManager().getComponent(entityId2, CollisionComponent::TYPE_ID)
                                        );
        if (collisionComponent1 && collisionComponent2)
        {
            std::vector<std::string> collisionGroups1 = collisionComponent1->getCollisionGroups();
            std::vector<std::string> collisionGroups2 = collisionComponent2->getCollisionGroups();
            std::vector<CollisionFilter::Signature> collisionGroupCombinations(collisionGroups1.size() * collisionGroups2.size());
            for(std::string collisionGroup1 : collisionGroups1)
            {
                for(std::string collisionGroup2 : collisionGroups2)
                {
                    collisionGroupCombinations.emplace_back(
                        collisionGroup1,
                        collisionGroup2
                    );
                }
            }
            for(CollisionFilter::Signature collisionSignature : collisionGroupCombinations)
            {
                auto filterIterators = m_impl->m_collisionFilterMap.equal_range(collisionSignature);//Get iterators for group of relevant CollisionFilters
                for(auto it = filterIterators.first; it != filterIterators.second; ++it)        // Foreach CollisionFilter object
                {
                    Collision collision = Collision(entityId1, entityId2, milliseconds);
                    it->second.addCollision(collision);
                }
            }
        }
        contactManifold->clearManifold();
    }
}


void
CollisionSystem::registerCollisionFilter(
    CollisionFilter& collisionFilter
) {
    m_impl->m_collisionFilterMap.emplace(
        collisionFilter.getCollisionSignature(),
        collisionFilter
    );
}

void
CollisionSystem::unregisterCollisionFilter(
    CollisionFilter& collisionFilter
) {
    m_impl->m_collisionFilterMap.erase(collisionFilter.getCollisionSignature());
}
