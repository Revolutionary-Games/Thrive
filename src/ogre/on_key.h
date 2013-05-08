#pragma once

#include "engine/component.h"
#include "engine/system.h"
#include "scripting/luabind.h"

#include <memory>


namespace thrive {

class OnKeyComponent : public Component {
    COMPONENT(OnKey)

public:

    static luabind::scope
    luaBindings();

    luabind::object m_onPressedCallback;

    luabind::object m_onReleasedCallback;

};


class OnKeySystem : public System {

public:

    OnKeySystem();

    ~OnKeySystem();

    void init(Engine*) override;

    void shutdown() override;

    void update(int milliseconds) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}

