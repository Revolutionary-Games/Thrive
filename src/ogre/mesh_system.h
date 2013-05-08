#pragma once

#include "engine/component.h"
#include "engine/shared_data.h"
#include "engine/system.h"

#include <memory>
#include <OgreString.h>


namespace thrive {

class MeshComponent : public Component {
    COMPONENT(Mesh)

public:

    static luabind::scope
    luaBindings();

    struct Properties {
        Ogre::String meshName = "";
    };

    RenderData<Properties>
    m_properties;

};


class MeshSystem : public System {

public:

    MeshSystem();

    ~MeshSystem();

    void init(Engine*) override;

    void shutdown() override;

    void update(int milliseconds) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}

