#pragma once

#include "engine/component.h"
#include "engine/shared_data.h"

#include <OgreVector3.h>
#include <OgreQuaternion.h>

namespace thrive {

class TransformComponent : public Component {
    COMPONENT(Transform)

public:

    struct Properties {
        Ogre::Quaternion orientation = Ogre::Quaternion::IDENTITY;
        Ogre::Vector3 position = {0, 0, 0};
        Ogre::Vector3 scale = {1, 1, 1};
    };

    static luabind::scope
    luaBindings();

    RenderData<Properties>
    m_properties;

};

}
