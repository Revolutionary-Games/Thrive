#pragma once

#include "engine/component.h"
#include "engine/system.h"
#include "engine/touchable.h"
#include "engine/typedefs.h"
#include "scripting/luabind.h"

#include <luabind/object.hpp>

namespace luabind {
class scope;
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
    static luabind::scope
    luaBindings();


    /**
    * @brief Sets the effect to use upon activation of the powerup
    *
    * @param effect
    *  Function taking the entityId of the activating entity.
    */
    void
    setEffect(
        std::function<bool(EntityId)> effect
    );

    /**
    * @brief Sets the effect to use upon activation of the powerup
    *  The effect does not get saved when the game is saved.
    *
    * @param effect
    *  Lua function taking the entityId of the activating entity.
    */
    void
    setEffect(
        const luabind::object& effect
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
    std::function<bool(EntityId)> m_effect;

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
    static luabind::scope
    luaBindings();

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
    void init(GameState* gameState) override;

    /**
    * @brief Shuts the system down
    */
    void shutdown() override;

    /**
    * @brief Updates the system
    */
    void update(int) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}

