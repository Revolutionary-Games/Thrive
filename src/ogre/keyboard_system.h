#pragma once

#include "engine/system.h"

#include <list>
#include <OISKeyboard.h>

namespace luabind {
class scope;
}

namespace thrive {

/**
* @brief Handles keyboard events
*/
class KeyboardSystem : public System {

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
    * Exposes the following functions to Lua:
    * - KeyboardSystem::isKeydown
    *
    * Also exposes the OIS::KeyCode enumeration
    *
    * @return 
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Constructor
    */
    KeyboardSystem();

    /**
    * @brief Destructor
    */
    ~KeyboardSystem();

    const std::list<KeyEvent>&
    eventQueue();

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
