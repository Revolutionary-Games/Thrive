#pragma once

#include "engine/system.h"

namespace thrive {

/**
* @brief Steps the physics simulation
*
* Requires a BulletEngine
*/
class UpdatePhysicsSystem : public System {

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - UpdatePhysicsSystem()
    *
    * @return 
    */
    static void luaBindings(sol::state &lua);

    /**
    * @brief Constructor
    */
    UpdatePhysicsSystem();

    /**
    * @brief Destructor
    */
    ~UpdatePhysicsSystem();

    /**
    * @brief Initializes the system
    *
    */
    void
    init(
        GameStateData* gameState
    ) override;

    /**
    * @brief Shuts down the system
    */
    void
    shutdown() override;

    /**
    * @brief Updates the system
    *
    * @param milliSeconds
    */
    void
    update(int, int) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
