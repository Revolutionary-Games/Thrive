#include "bullet/collision_system.h"

#include "scripting/luajit.h"

#include "bullet/collision_filter.h"
#include "bullet/physical_world.h"
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

void CollisionComponent::luaBindings(
    sol::state &lua
){
    lua.new_usertype<CollisionComponent>("CollisionComponent",

        sol::constructors<sol::types<>, sol::types<const std::string&>>(),

        sol::base_classes, sol::bases<Component>(),

        "ID", sol::var(lua.create_table_with("TYPE_ID", CollisionComponent::TYPE_ID)),
        "TYPE_NAME", &CollisionComponent::TYPE_NAME,

        "addCollisionGroup", &CollisionComponent::addCollisionGroup
    );
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

void Collision::luaBindings(
    sol::state &lua
){
    lua.new_usertype<Collision>("Collision",

        sol::constructors<sol::types<EntityId, EntityId, int>>(),
        
        "entityId1", sol::readonly(&Collision::entityId1),
        "entityId2", sol::readonly(&Collision::entityId2),
        "addedCollisionDuration", sol::readonly(&Collision::addedCollisionDuration)
    );
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

void CollisionSystem::luaBindings(
    sol::state &lua
){
    lua.new_usertype<CollisionSystem>("CollisionSystem",

        sol::constructors<sol::types<>>(),
        
        sol::base_classes, sol::bases<System>()
    );
}

void
CollisionSystem::init(
    GameStateData* gameState
) {
    System::initNamed("CollisionSystem", gameState);
    m_impl->m_world = gameState->physicalWorld()->physicsWorld();
}


void
CollisionSystem::shutdown() {
    System::shutdown();
    m_impl->m_world = nullptr;
}

void
CollisionSystem::update(
 int,
 int logicTime
) {
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
                                            System::gameState()->entityManager()->getComponent(entityId1, CollisionComponent::TYPE_ID)
                                        );
        CollisionComponent* collisionComponent2 = static_cast<CollisionComponent*>(
                                            System::gameState()->entityManager()->getComponent(entityId2, CollisionComponent::TYPE_ID)
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
                    Collision collision = Collision(entityId1, entityId2, logicTime);
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
