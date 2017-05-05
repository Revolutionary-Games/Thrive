#pragma once

#include <memory>

struct btDiscreteDynamicsWorld;

namespace sol{

class state;
}

namespace thrive{

//! \brief Wrapper for holding a Bullet configuration in lua
class PhysicalWorld{

    struct PhysicsConfiguration;

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - PhysicalWorld()
    *
    * @return 
    */
    static void luaBindings(sol::state &lua);
    

    //! \brief Creates and sets up all the physics objects
    PhysicalWorld();


    btDiscreteDynamicsWorld*
    physicsWorld();


private:

    std::unique_ptr<PhysicsConfiguration> m_physics;
};


}
