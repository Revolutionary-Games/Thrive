#pragma once

#include "engine/system.h"

namespace thrive {

/**
* @brief System for rendering a single frame per update
*
*/
class RenderSystem : public System {

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - RenderSystem()
    *
    * @return 
    */
    static void luaBindings(sol::state &lua);

    /**
    * @brief Constructor
    */
    RenderSystem();

    /**
    * @brief Destructor
    */
    ~RenderSystem();

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
    * @brief Renders a single frame
    *
    * Calls OgreRoot::renderOneFrame(float).
    *
    * @param renderTime
    */
    void
    update(
        int renderTime,
        int
    ) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
