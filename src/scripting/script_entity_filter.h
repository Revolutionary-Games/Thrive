#pragma once

#include "engine/typedefs.h"

#include <memory>
#include <unordered_set>

class lua_State;

namespace luabind {
    class scope;
}

namespace thrive {

class ScriptEntityFilter {

public:

    static luabind::scope
    luaBindings();

    ScriptEntityFilter(
        lua_State* L
    );

    ~ScriptEntityFilter();

    std::unordered_set<EntityId>
    addedEntities() const;

    void
    clearChanges();

    bool
    containsEntity(
        EntityId id
    ) const;

    std::unordered_set<EntityId>
    entities() const;

    std::unordered_set<EntityId>
    removedEntities() const;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
    

};

}
