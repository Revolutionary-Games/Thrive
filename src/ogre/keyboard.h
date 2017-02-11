#pragma once

#include <list>
#include <memory>
#include <OISKeyboard.h>

namespace sol {
class state;
}

namespace OIS {
class InputManager;
}

namespace CEGUI {
    class InputAggregator;
}

namespace thrive {

/**
* @brief Handles keyboard events
*/
class Keyboard {

public:

    /**
    * @brief Represents a single key press or release
    */
    struct KeyEvent {

        /**
        * @brief The key that was pressed
        */
        const OIS::KeyCode key;

        /**
        * @brief \c true if the key was pressed, \c false if it was released
        */
        const bool pressed;

        /**
        * @brief \c true if the Alt modifier was pressed during the event
        */
        const bool alt;

        /**
        * @brief \c true if the Ctrl modifier was pressed during the event
        */
        const bool ctrl;

        /**
        * @brief \c true if the Shift modifier was pressed during the event
        */
        const bool shift;

    };

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - Keyboard::isKeydown
    * - Keyboard::KeyEvent
    *   - Keyboard::KeyEvent::key
    *   - Keyboard::KeyEvent::alt
    *   - Keyboard::KeyEvent::ctrl
    *   - Keyboard::KeyEvent::shift
    *   - Keyboard::KeyEvent::pressed
    * - <a href="http://code.joyridelabs.de/ois_api/OISKeyboard_8h_source.html#l00031">KeyCode</a>
    *
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Constructor
    */
    Keyboard();

    /**
    * @brief Destructor
    */
    ~Keyboard();

    /**
    * @brief A list of key events in the current frame
    *
    * The list is cleared and newly populated for each call to update().
    *
    * @return 
    */
    const std::list<KeyEvent>&
    eventQueue() const;

    /**
     * @brief Initializes the keyboard
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
    * @brief Checks whether a key is pressed down
    *
    * @param key
    *   The key to check for
    *
    * @return 
    *   \c true if the key is pressed down during this frame, \c false 
    *   otherwise
    */
    bool
    isKeyDown(
        OIS::KeyCode key
    ) const;

    /**
    * @brief Shuts down the keyboard
    */
    void
    shutdown();

    /**
    * @brief Updates the queue with new events
    */
    void
    update();

    /**
    * @brief Checks whether a key was pressed down since the last frame
    *
    * This function compares the state of the previous and this frame. If
    * \a key was not down in the last frame and down in this frame, it returns
    * \c true.
    *
    * @param key
    *   The key to check for
    *
    * @return \c true if \a key was pressed down
    */
    bool
    wasKeyPressed(
        OIS::KeyCode key
    ) const;

    /**
    * @brief Checks whether a key was released since the last frame
    *
    * This function compares the state of the previous and this frame. If
    * \a key was down in the last frame and not down in this frame, it returns
    * \c true.
    *
    * @param key
    *   The key to check for
    *
    * @return \c true if \a key was released
    */
    bool
    wasKeyReleased(
        OIS::KeyCode key
    ) const;

private:
    
    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
