#pragma once

#include "engine/component.h"
#include "engine/system.h"
#include "scripting/luajit.h"

// Time between spawn cycles
#define SPAWN_INTERVAL 100

namespace sol {
class state;
}

namespace thrive {

/**
* @brief A component for a Spawn reactive entity
*/
class SpawnedComponent : public Component {
    COMPONENT(SpawnComponent)

public:
    /**
    * @brief Constructor
    *
    * @param SpawnGroup
    *  Initial Spawn group that the containing entity should belong to.
    *  Spawn groups determine which SpawnFilter objects are notified
    *  when a Spawn involving this object occours.
    *  More Spawn groups can be added with addSpawnGroup(group)
    */
    SpawnedComponent();

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - RigidBodyComponent()
    *
    * @return
    */
    static void luaBindings(sol::state &lua);

    void
    setSpawnRadius(
        double newSpawnRadius
    );

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

    double spawnRadiusSqr;
};

class SpawnSystem : public System {
public:
    /**
    * @brief Constructor
    */
    SpawnSystem();

    /**
    * @brief Destructor
    */
    ~SpawnSystem();

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - SpawnSystem
    * - init
    * - AddSpawnType
    * - RemoveSpawnType
    *
    * @return
    */
    static void luaBindings(sol::state &lua);

    /**
    * @brief Initializes the engine
    *
    * @param engine
    */
    void init(
        GameStateData* gameState
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
        int renderTime,
        int logicTime
    ) override;

    SpawnerTypeId addSpawnType(sol::protected_function factoryFunction, double spawnDensity, double spawnRadius);

    void removeSpawnType(SpawnerTypeId spawnId);

private:
    struct Implementation;
    std::unique_ptr<Implementation> m_impl;

    void doSpawnCycle();
};
}
