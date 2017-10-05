#pragma once

#include "engine/system.h"

#define MICROBE_CAMERA_NAME "camera"

#define INITIAL_CAMERA_HEIGHT 70

namespace sol {
class state;
}

namespace thrive {

/**
* @brief The camera for the microbe stage.
*
*/
class MicrobeCameraSystem : public System {
public:
    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - MicrobeCameraSystem()
    *
    * @return
    */
    static void luaBindings(sol::state &lua);

    /**
    * @brief Constructor
    */
    MicrobeCameraSystem();

    /**
    * @brief Activates the system
    *
    */
    void
    activate() override;

    /**
    * @brief Initializes the system
    *
    */
    void
    init(
        GameStateData* gameState
    ) override;

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
