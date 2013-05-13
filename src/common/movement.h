#pragma once

#include "engine/component.h"
#include "engine/system.h"

#include <OgreVector3.h>
#include <OgreQuaternion.h>

namespace thrive {

/**
* @brief Has the power to move you
*/
class MovableComponent : public Component {
    COMPONENT(Movable)

public:

    /**
    * @brief Lua bindings
    *
    * This component exposes the following properties:
    * \arg \c velocity (Ogre.Vector3): The component's velocity in units per second
    *
    * @return 
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief The entity's velocity in units per second
    */
    Ogre::Vector3 m_velocity;

};


/**
* @brief Moves entities
*
* This system updates the TransformComponent of all entities that also have a
* MovableComponent.
*
*/
class MovementSystem : public System {
    
public:

    MovementSystem();

    ~MovementSystem();

    void init(Engine* engine) override;

    void shutdown() override;

    void update(int milliSeconds) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}

