#pragma once

#include "engine/system.h"

namespace thrive {

/**
* @brief System for rendering a single frame per update
*
* Requires an OgreEngine
*/
class RenderSystem : public System {

public:

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
    * @param engine
    *   Must be an OgreEngine
    */
    void
    init(
        Engine* engine
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
