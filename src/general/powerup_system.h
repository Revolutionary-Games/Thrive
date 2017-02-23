#pragma once

#include "engine/component.h"
#include "engine/system.h"
#include "engine/touchable.h"
#include "engine/typedefs.h"

namespace sol {
class state;
}


namespace thrive {


/**
* @brief Component for entities that work as powerups
*/
class PowerupComponent : public Component {
    COMPONENT(PowerupComponent)

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - PowerupComponent()
    * - PowerupComponent::setEffect
    *
    * @return
    */
    static void luaBindings(sol::state &lua);

    /**
    * @brief Sets the effect to use upon activation of the powerup
    *
    * @param effect
    *  Function taking the entityId of the activating entity.
    */
    void
    setEffect(
        const std::string&
    );


    /**
    * @brief Sets the effect to use upon activation of the powerup
    *
    * @param effect
    *  Function taking the entityId of the activating entity.
    */
    void
    setEffect(
        std::function<bool(EntityId)>* effect
    );

    void
    load(
        const StorageContainer& storage
    ) override;

    StorageContainer
    storage() const override;

private:

    friend class PowerupSystem;

    /**
    * @brief The function to be called when the powerup is activated for an entity
    */
    std::function<bool(EntityId)>* m_effect;

    /**
    * @brief The name of the effect function that is defined in configs.lua
    */
    std::string effectName;

};


/**
* @brief System for handling powerups
*/
class PowerupSystem : public System {

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - PowerupSystem()
    *
    * @return
    */
    static void luaBindings(sol::state &lua);

    /**
    * @brief Constructor
    */
    PowerupSystem();

    /**
    * @brief Destructor
    */
    ~PowerupSystem();

    /**
    * @brief Initializes the system
    *
    */
    void init(GameStateData* gameState) override;

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

