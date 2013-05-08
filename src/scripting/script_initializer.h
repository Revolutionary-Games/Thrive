#pragma once

#include "engine/component.h"

#include <memory>

class lua_State;

namespace luabind {
class scope;
}

namespace thrive {

class ScriptInitializer {

public:

    static ScriptInitializer&
    instance();

    void
    initialize(
        lua_State* L
    );

    bool
    addBindings(
        luabind::scope bindings
    );

private:

    ScriptInitializer();

    ~ScriptInitializer();

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
