#pragma once

#include "engine/system.h"

namespace sol {
class state;
}

namespace thrive {

/**
* @brief Updates OgreSceneNodeComponents with physics data
*
*/
class BulletToOgreSystem : public System {

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - BulletToOgreSystem()
    *
    * @return 
    */
    static void luaBindings(sol::state &lua);

    /**
    * @brief Constructor
    */
    BulletToOgreSystem();

    /**
    * @brief Destructor
    */
    ~BulletToOgreSystem();

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
    * @param renderTime
    *
    * @param logicTime
    */
    void
    update(
        int renderTime,
        int logicTime
    ) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}

