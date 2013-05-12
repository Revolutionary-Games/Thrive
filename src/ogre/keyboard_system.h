#pragma once

#include "engine/shared_data.h"
#include "engine/system.h"

#include <OISKeyboard.h>

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
    * @brief Constructor
    */
    KeyboardSystem();

    /**
    * @brief Destructor
    */
    ~KeyboardSystem();

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
    * @brief A shared queue used for queueing up the key events
    */
    InputQueue<KeyEvent>&
    eventQueue();

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
