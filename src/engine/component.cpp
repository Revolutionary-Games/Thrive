#include "engine/component.h"

#include "engine/component_factory.h"
#include "engine/engine.h"
#include "engine/serialization.h"
#include "game.h"
#include "scripting/luabind.h"

#include <luabind/class_info.hpp>

using namespace thrive;

template<typename T>
static std::string
getLuaClassName(
    lua_State* L,
    T* obj
) {
    luabind::object(L, obj).push(L);
    luabind::argument argument(luabind::from_stack(L, lua_gettop(L)));
    luabind::class_info info = luabind::get_class_info(argument);
    std::string name = info.name;
    lua_pop(L, 1);
    return name;
}

/**
* @brief Wrapper class to enable subclassing Component in Lua
*
* \cond
*/
struct ComponentWrapper : Component, luabind::wrap_base {

    ComponentWrapper(
        lua_State* L
    ) : m_luaState(L)
    {
    }

    void
    load(
        const StorageContainer& storage
    ) override {
        call<void>("load", storage);
    }

    static void default_load(
        Component* self, 
        const StorageContainer& storage
    ) {
        self->Component::load(storage);
    }

    ComponentTypeId
    typeId() const override {
        return Game::instance().engine().componentFactory().getTypeId(
            this->typeName()
        );
    }

    std::string
    typeName() const override {
        return getLuaClassName(m_luaState, this);
    }

    StorageContainer
    storage() const override {
        return call<StorageContainer>("storage");
    }

    static StorageContainer
    default_storage(
        Component* self
    ) {
        return self->Component::storage();
    }

    lua_State* m_luaState = nullptr;

};

/**
 * \endcond
 */

luabind::scope
Component::luaBindings() {
    using namespace luabind;
    return class_<Component, ComponentWrapper>("Component")
        .def(constructor<lua_State*>())    
        .def("isVolatile", &Component::isVolatile)
        .def("load", &Component::load, &ComponentWrapper::default_load)
        .def("setVolatile", &Component::setVolatile)
        .def("storage", &Component::storage, &ComponentWrapper::default_storage)
        .def("typeId", &Component::typeId)
        .def("typeName", &Component::typeName)
        .def("owner", &Component::owner)
    ;
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


