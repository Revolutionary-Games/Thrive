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
    static luabind::scope
    luaBindings();

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
        GameState* gameState
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
    * @param milliSeconds
    */
    void
    update(
        int milliSeconds
    ) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
