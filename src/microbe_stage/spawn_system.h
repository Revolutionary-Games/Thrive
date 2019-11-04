#pragma once

#include "engine/component_types.h"
#include "engine/typedefs.h"

#include <Entities/System.h>

namespace thrive {

class CellStageWorld;

////////////////////////////////////////////////////////////////////////////////
// SpawnType
////////////////////////////////////////////////////////////////////////////////
struct SpawnType {
    // Why are doubles used here?
    double spawnRadius = 0.0;
    double spawnRadiusSqr = 0.0;
    double spawnFrequency = 0.0;
    std::function<ObjectID(CellStageWorld&, Float3)> factoryFunction;
    SpawnerTypeId id = 0;
};

/**
 * @brief A component for a Spawn reactive entity
 */
class SpawnedComponent : public Leviathan::Component {
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
    SpawnedComponent(double newSpawnRadiusSqr);

    REFERENCE_HANDLE_UNCOUNTED_TYPE(SpawnedComponent);

    // /**
    // * @brief Loads the component
    // *
    // * @param storage
    // */
    // void
    // load(
    //     const StorageContainer& storage
    // ) override;


    // /**
    // * @brief Serializes the component
    // *
    // * @return
    // */
    // StorageContainer
    // storage() const override;

    double spawnRadiusSqr;

    static constexpr auto TYPE =
        componentTypeConvert(THRIVE_COMPONENT::SPAWNED);
};

class SpawnSystem : public Leviathan::System<
                        std::tuple<SpawnedComponent&, Leviathan::Position&>> {
public:
    /**
     * @brief Constructor
     */
    SpawnSystem();

    /**
     * @brief Destructor
     */
    ~SpawnSystem();

    void
        Run(CellStageWorld& world, float elapsed);

    SpawnerTypeId
        addSpawnType(
            std::function<ObjectID(CellStageWorld&, Float3)> factoryFunction,
            double spawnDensity,
            double spawnRadius);

    void
        removeSpawnType(SpawnerTypeId spawnId);

    bool
        updateDensity(SpawnerTypeId spawnId, double spawnDensity);

    //! Called before shutdown to clear everything
    //! (called automatically when the world is released)
    void
        Release();

    void
        Clear();

    void
        CreateNodes(const std::vector<std::tuple<SpawnedComponent*, ObjectID>>&
                        firstdata,
            const std::vector<std::tuple<Leviathan::Position*, ObjectID>>&
                seconddata,
            const ComponentHolder<SpawnedComponent>& firstholder,
            const ComponentHolder<Leviathan::Position>& secondholder)
    {
        TupleCachedComponentCollectionHelper(
            CachedComponents, firstdata, seconddata, firstholder, secondholder);
    }

    void
        DestroyNodes(const std::vector<std::tuple<SpawnedComponent*, ObjectID>>&
                         firstdata,
            const std::vector<std::tuple<Leviathan::Position*, ObjectID>>&
                seconddata)
    {
        CachedComponents.RemoveBasedOnKeyTupleList(firstdata);
        CachedComponents.RemoveBasedOnKeyTupleList(seconddata);
    }

private:
    // Time between spawn cycles
    static constexpr float SPAWN_INTERVAL = 0.1f;

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};
} // namespace thrive
