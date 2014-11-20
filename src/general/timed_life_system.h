#pragma once

#include "engine/component.h"
#include "engine/system.h"
#include "engine/touchable.h"
#include "scripting/luabind.h"


namespace luabind {
class scope;
}


namespace thrive {


/**
* @brief Component for entities with timed life
*/
class TimedLifeComponent : public Component {
    COMPONENT(TimedLifeComponent)

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - TimedLifeComponent()
    * - TimedLifeComponent::m_timeToLive
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief The time until the owning entity despawns
    */
    Milliseconds m_timeToLive = 0;

    void
    load(
        const StorageContainer& storage
    ) override;

    StorageContainer
    storage() const override;

};


/**
* @brief Despawns entities after they've reached their lifetime
*/
class TimedLifeSystem : public System {

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - TimedLifeSystem()
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Constructor
    */
    TimedLifeSystem();

    /**
    * @brief Destructor
    */
    ~TimedLifeSystem();

    /**
    * @brief Initializes the system
    *
    */
    void init(GameState* gameState) override;

    /**
    * @brief Shuts the system down
    */
    void shutdown() override;

    /**
    * @brief Updates the system
    */
    void update(int, int) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}

