#pragma once

#include "engine/system.h"

#include <list>
#include <OISMouse.h>

namespace luabind {
class scope;
}

namespace Ogre {
class Vector3;
}

namespace thrive {

/**
* @brief Handles mouse events
*/
class MouseSystem : public System {

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - MouseSystem::isButtonDown
    * - MouseSystem::normalizedPosition
    * - MouseSystem::position
    *
    * @return 
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Constructor
    */
    MouseSystem();

    /**
    * @brief Destructor
    */
    ~MouseSystem();

    /**
    * @brief Initializes the system
    *
    * @param engine
    */
    void
    init(
        Engine* engine
    ) override;

    /**
    * @brief Checks whether a mouse button is pressed
    *
    * @param button
    *   The button to check for
    *
    * @return 
    *   \c true if the mouse button is pressed down, \c false otherwise
    */
    bool
    isButtonDown(
        OIS::MouseButtonID button
    ) const;

    /**
    * @brief The mouse position in coordinates ranging from 0.0 to 1.0
    *
    * @return 
    */
    Ogre::Vector3
    normalizedPosition() const;

    /**
    * @brief The mouse position in pixels, relative to the top-right window corner
    *
    * @return 
    */
    Ogre::Vector3
    position() const;

    /**
    * @brief Updates the window size
    *
    * Used for normalization
    *
    * @param width
    *   Window width in pixels
    *
    * @param height
    *   Window height in pixels
    */
    void
    setWindowSize(
        int width,
        int height
    );

    /**
    * @brief Shuts down the system
    */
    void
    shutdown() override;

    /**
    * @brief Updates the queue with new events
    */
    void
    update(
        int
    ) override;

private:
    
    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}

