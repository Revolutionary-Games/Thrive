#pragma once

#include "engine/component.h"
#include "engine/system.h"

#include <OgreVector3.h>
#include <OgreQuaternion.h>

namespace thrive {


/**
* @brief Moves entities
*
* This system updates the PhysicsTransformComponent of all entities that also have a
* RigidBodyComponent.
*
*/
class RigidBodyOutputSystem : public System {

public:

    RigidBodyOutputSystem();

    ~RigidBodyOutputSystem();

    void init(Engine* engine) override;

    void shutdown() override;

    void update(int milliSeconds) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}

