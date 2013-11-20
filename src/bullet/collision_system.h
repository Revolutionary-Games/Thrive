#pragma once

#include "engine/component.h"
#include "engine/system.h"
#include "engine/typedefs.h"

#include <iostream>
#include <utility>
#include <vector>

namespace luabind {
class scope;
}

namespace thrive {

class CollisionFilter;

/**
* @brief A component for a collision reactive entity
*/
class CollisionComponent : public Component {
    COMPONENT(CollisionComponent)

public:

    /**
    * @brief Constructor
    */
    CollisionComponent();

    /**
    * @brief Constructor
    *
    * @param collisionGroup
    *  Initial collision group that the containing entity should belong to.
    *  Collision groups determine which CollisionFilter objects are notified
    *  when a collision involving this object occours.
    *  More collision groups can be added with addCollisionGroup(group)
    */
    CollisionComponent(
        const std::string& collisionGroup
    );

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - RigidBodyComponent()
    * - m_collisionCallbackKey
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Add a collision group
    *   Collision groups determine which CollisionFilter objects are notified
    *   when a collision involving this object occours.
    *
    * @param group
    *   The collision group to add.
    */
    void
    addCollisionGroup(
        const std::string& group
    );

    /**
    * @brief Remove a collision group
    *
    * @param group
    *   The collision group to remove.
    */
    void
    removeCollisionGroup(
        const std::string& group
    );

    const std::vector<std::string>&
    getCollisionGroups();

    /**
    * @brief Loads the component
    *
    * @param storage
    */
    void
    load(
        const StorageContainer& storage
    ) override;


    /**
    * @brief Serializes the component
    *
    * @return
    */
    StorageContainer
    storage() const override;

private:

    std::vector<std::string> m_collisionGroups;

};


struct Collision {

    /**
    * @brief Constructor
    */
    Collision(
        EntityId entityId1,
        EntityId entityId2,
        int addedCollisionDuration
    );


    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - Collision::entityId1
    * - Collision::entityId2
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief First collided entity
    */
    EntityId entityId1;

    /**
    * @brief Second collided entity
    */
    EntityId entityId2;

    /**
    * @brief The amount of time the collision represent
    *
    *   An iteration of the main game loop may differ in time, and so users
    *   may want to know how long a period of time this collision represent.
    *   Inaccuracies may occour as the collision may not have been active for
    *   the entirety of this duration which must be taken into account or ignored.
    *   This time will keep accumalating until clearCollisions() is called.
    */
    int addedCollisionDuration;

};

}//namespace thrive
namespace std {
    using namespace thrive;
    template<>
    struct hash<std::pair<EntityId, EntityId>> {
        std::size_t
        operator() (
            const std::pair<EntityId, EntityId>& pair
        ) const;
    };
    template <>
    struct equal_to<std::pair<EntityId, EntityId>> //Comparator for Collision
    {
        bool operator()(
            const std::pair<EntityId, EntityId>& collisionKey1,
            const std::pair<EntityId, EntityId>& collisionKey2
        ) const;
    };
}

namespace std {
    using namespace thrive;

}
namespace thrive{


class CollisionSystem : public System {

public:

    /**
    * @brief Constructor
    */
    CollisionSystem();

    /**
    * @brief Destructor
    */
    ~CollisionSystem();

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - CollisionSystem()
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Initializes the engine
    *
    * @param engine
    */
    void init(
        GameState* gameState
    ) override;

    /**
    * @brief Shuts the system down
    */
    void shutdown() override;

    /**
    * @brief Updates the system
    *
    * @param milliSeconds
    */
    void update(
        int milliSeconds
    ) override;

    /**
    * @brief Register a collision filter.
    *
    *  Once a collision filter is registered it will automatically receive new relevant collisions.
    *
    * @param collisionFilter
    *   The filter to register
    */
    void
    registerCollisionFilter(
        CollisionFilter& collisionFilter
    );

    /**
    * @brief Unregisters a collision filter.
    *
    * Necessary if CollisionSystem might be deleted before CollisionFilter
    *
    * @param collisionFilter
    *   The filter to unregister
    */
    void
    unregisterCollisionFilter(
        CollisionFilter& collisionFilter
    );

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;

};

}
