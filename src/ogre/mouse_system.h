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

    bool
    isButtonDown(
        OIS::MouseButtonID button
    ) const;

    Ogre::Vector3
    normalizedPosition() const;

    Ogre::Vector3
    position() const;

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

