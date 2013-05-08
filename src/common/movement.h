#pragma once

#include "engine/component.h"
#include "engine/system.h"

#include <OgreVector3.h>
#include <OgreQuaternion.h>

namespace thrive {

class MovableComponent : public Component {
    COMPONENT(Movable)

public:

    static luabind::scope
    luaBindings();

    Ogre::Vector3 m_velocity;

};


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

