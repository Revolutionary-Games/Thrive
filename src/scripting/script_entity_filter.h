#pragma once

#include "engine/typedefs.h"
#include "scripting/luabind.h"

#include <luabind/object.hpp>
#include <memory>
#include <unordered_set>

namespace thrive {

class ScriptEntityFilter {

public:

    static luabind::scope
    luaBindings();

    ScriptEntityFilter(
        luabind::object componentTypes
    );

    ~ScriptEntityFilter();

    const std::unordered_set<EntityId>&
    addedEntities();

    void
    clearChanges();

    bool
    containsEntity(
        EntityId id
    ) const;

    const std::unordered_set<EntityId>&
    entities();

    const std::unordered_set<EntityId>&
    removedEntities();

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
    

};

}
