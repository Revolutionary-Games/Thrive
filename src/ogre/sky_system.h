#pragma once

#include "engine/component.h"
#include "engine/shared_data.h"
#include "engine/system.h"

#include <memory>
#include <OgrePlane.h>
#include <OgreResourceGroupManager.h>


#include <iostream>

namespace luabind {
class scope;
}

namespace thrive {

class SkyPlaneComponent : public Component {
    COMPONENT(SkyPlane)

public:

    struct Properties {
        bool enabled = true;
        Ogre::Plane plane = {1, 1, 1, 1};
        Ogre::String materialName = "Background/Blue1";
        Ogre::Real scale = 1000;
        Ogre::Real tiling = 10;
        bool drawFirst = true;
        Ogre::Real bow = 0;
        int xsegments = 1;
        int ysegments = 1;
        Ogre::String groupName = Ogre::ResourceGroupManager::DEFAULT_RESOURCE_GROUP_NAME;
    };

    static luabind::scope
    luaBindings();

    RenderData<Properties>
    m_properties;

};


class SkySystem : public System {
    
public:

    SkySystem();

    ~SkySystem();

    void init(Engine* engine) override;

    void shutdown() override;

    void update(int milliSeconds) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
