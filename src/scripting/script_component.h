#pragma once

#include "engine/component.h"

namespace luabind {
    class scope;
}

namespace thrive {

class ScriptComponent : public Component {

public:

    static luabind::scope
    luaBindings();

    virtual ~ScriptComponent() = 0;

};

}
