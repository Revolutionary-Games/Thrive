#include "scripting/script_component.h"

#include "engine/component_factory.h"
#include "engine/engine.h"
#include "engine/serialization.h"
#include "game.h"
#include "scripting/luabind.h"

#include <luabind/class_info.hpp>
#include <unordered_map>

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


ScriptComponent::~ScriptComponent() {}
    

struct ScriptComponentWrapper : ScriptComponent, luabind::wrap_base {

    ScriptComponentWrapper(
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
        ScriptComponent* self, 
        const StorageContainer& storage
    ) {
        self->ScriptComponent::load(storage);
    }

    ComponentTypeId
    typeId() const override {
        return Game::instance().engine().componentFactory().getTypeId(this->typeName());
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
        ScriptComponent* self
    ) {
        return self->ScriptComponent::storage();
    }

    lua_State* m_luaState = nullptr;

};

luabind::scope
ScriptComponent::luaBindings() {
    using namespace luabind;
    return class_<ScriptComponent, ScriptComponentWrapper>("ScriptComponent")
        .def(constructor<lua_State*>())    
        .def("load", &ScriptComponent::load, &ScriptComponentWrapper::default_load)
        .def("storage", &ScriptComponent::storage, &ScriptComponentWrapper::default_storage)
    ;
}


