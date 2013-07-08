#pragma once

#include "engine/component.h"
#include "engine/system.h"

#include <OgreVector3.h>

namespace thrive {
class MicrobeMovementComponent : public Component {
    COMPONENT(MicrobeMovementComponent)

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - MicrobeMovementComponent()
    * - @link m_direction direction @endlink
    * - @link m_force force @endlink
    *
    * @return 
    */
    static luabind::scope
    luaBindings();

    Ogre::Vector3 m_direction = Ogre::Vector3::ZERO;

    float m_force = 0.0;

};


/**
* @brief Moves microbes around
*/
class MicrobeMovementSystem : public System {
    
public:

    /**
    * @brief Constructor
    */
    MicrobeMovementSystem();

    /**
    * @brief Destructor
    */
    ~MicrobeMovementSystem();

    /**
    * @brief Initializes the system
    *
    * @param engine
    */
    void init(Engine* engine) override;

    /**
    * @brief Shuts the system down
    */
    void shutdown() override;

    /**
    * @brief Updates the system
    */
    void update(int) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};


}
