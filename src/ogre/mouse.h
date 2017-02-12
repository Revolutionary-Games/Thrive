#pragma once

#include <list>
#include <memory>
#include <OISMouse.h>

namespace sol {
class state;
}

namespace Ogre {
class Vector3;
}

namespace OIS {
class InputManager;
}

namespace CEGUI {
    class InputAggregator;
}

namespace thrive {

/**
* @brief Handles mouse events
*/
class Mouse {

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - Mouse::isButtonDown
    * - Mouse::normalizedPosition
    * - Mouse::position
    *
    * @return
    */
    static void luaBindings(sol::state &lua);

    /**
    * @brief Constructor
    */
    Mouse();

    /**
    * @brief Destructor
    */
    ~Mouse();

    /**
    * @brief Initializes the mouse
    *
    * @param inputManager
    *   The input manager to use
    */
    void
    init(
        OIS::InputManager* inputManager,
        CEGUI::InputAggregator* aggregator
    );

    /**
    * @brief Checks whether a mouse button is pressed
    *  Note that unlike wasButtonPressed this is not affected by CEGUI
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
    * @brief Checks whether a mouse button was pressed since last update frame
    *  Note that this only returns true if CEGUI did not handle the event
    *
    * @param button
    *   The button to check for
    *
    * @return
    *   \c true if the mouse button was released since last update, \c false otherwise
    */
    bool
    wasButtonPressed(
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
    * @brief The scrollvalue since last update
    *
    * @return
    */
    int
    scrollChange() const;

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
    * @brief Shuts down the mouse
    */
    void
    shutdown();

    /**
    * @brief Updates the queue with new events
    */
    void
    update();

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}

