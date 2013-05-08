#pragma once

#include "engine/component.h"
#include "engine/system.h"
#include "scripting/luabind.h"

#include <memory>


namespace thrive {

class OnUpdateComponent : public Component {
    COMPONENT(OnUpdate)

public:

    static luabind::scope
    luaBindings();

    luabind::object m_callback;

};


class OnUpdateSystem : public System {

public:

    OnUpdateSystem();

    ~OnUpdateSystem();

    void init(Engine*) override;

    void shutdown() override;

    void update(int milliseconds) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
