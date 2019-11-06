#pragma once

#include "engine/component_types.h"

#include "Entities/Component.h"
#include "Entities/System.h"


namespace thrive {


/**
 * @brief Component for entities with timed life
 */
class TimedLifeComponent : public Leviathan::Component {
public:
    TimedLifeComponent(float timeToLive);

    // void
    // load(
    //     const StorageContainer& storage
    // ) override;

    // StorageContainer
    // storage() const override;

    REFERENCE_HANDLE_UNCOUNTED_TYPE(TimedLifeComponent);

    static constexpr auto TYPE =
        componentTypeConvert(THRIVE_COMPONENT::TIMED_LIFE);

    /**
     * @brief The time until the owning entity despawns
     *
     * This is now in seconds (previously was in milliseconds)
     */
    float m_timeToLive = 0;
};


/**
 * @brief Despawns entities after they've reached their lifetime
 */
class TimedLifeSystem {
public:
    void
        Run(GameWorld& world,
            std::unordered_map<ObjectID, TimedLifeComponent*>& components,
            float elapsed);
};

} // namespace thrive
