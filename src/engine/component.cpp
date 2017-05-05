#include "engine/component.h"

#include "engine/component_factory.h"
#include "engine/engine.h"
#include "engine/serialization.h"
#include "game.h"
#include "scripting/luajit.h"

using namespace thrive;

void Component::luaBindings(
    sol::state &lua
){
    lua.new_usertype<Component>("Component",

        "new", sol::no_constructor,
        
        "isVolatile", &Component::isVolatile,
        "load", &Component::load,
        "setVolatile", &Component::setVolatile,
        "storage", &Component::storage,
        "typeId", &Component::typeId,
        "typeName", &Component::typeName,
        "owner", &Component::owner
    );
}

Component::~Component() {}


bool
Component::isVolatile() const {
    return m_isVolatile;
}


void
Component::load(
    const StorageContainer& storage
) {
    m_owner = storage.get<EntityId>("owner");
}


void
Component::setVolatile(
    bool isVolatile
) {
    m_isVolatile = isVolatile;
}


StorageContainer
Component::storage() const {
    StorageContainer storage;
    storage.set<EntityId>("owner", m_owner);
    return storage;
}


