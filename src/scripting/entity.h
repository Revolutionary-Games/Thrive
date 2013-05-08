#pragma once

namespace thrive {

class Entity {

public:

    static luabind::scope
    luaBindings();

    Entity(
        EntityId id
    );
};

}
