#include "engine/component_factory.h"

#include "scripting/luabind.h"

#include <luabind/class_info.hpp>

using namespace thrive;

using Registry = std::unordered_map<
    std::string, 
    std::pair<ComponentTypeId, ComponentFactory::ComponentLoader>
>;

struct ComponentFactory::Implementation {

    Registry m_registry;

};


static ComponentTypeId
generateTypeId() {
    static ComponentTypeId typeId = NULL_COMPONENT_TYPE;
    typeId++;
    return typeId;
}


static Registry&
globalRegistry() {
    static Registry registry;
    return registry;
}


static ComponentTypeId
ComponentFactory_registerComponentType(
    ComponentFactory* self,
    const std::string& name,
    luabind::object cls
) {
    lua_State* L = cls.interpreter();
    // DEBUG >>
    luabind::object(L, cls).push(L);
    luabind::argument argument(luabind::from_stack(L, lua_gettop(L)));
    luabind::class_info info = luabind::get_class_info(argument);
    lua_pop(L, 1);
    std::cout << "Class info says: " << info.name << std::endl;
    // << DEBUG
    auto type = luabind::type(cls);
    if (type != LUA_TUSERDATA) {
        std::string typeName(
            lua_typename(L, type)
        );
        throw std::runtime_error("Argument 2 must be class object, but is: " + typeName);
    }
    ComponentTypeId typeId = self->registerComponentType(
        name,
        [cls] (const StorageContainer& storage) {
            luabind::object classTable = cls;
            luabind::object obj = classTable();
            auto component = std::unique_ptr<Component>(luabind::object_cast<Component*>(obj));
            component->load(storage);
            return component;
        }
    );
    cls["TYPE_ID"] = typeId;
    return typeId;
}


luabind::scope
ComponentFactory::luaBindings() {
    using namespace luabind;
    return class_<ComponentFactory>("ComponentFactory")
        .def("registerComponentType", &ComponentFactory_registerComponentType)
    ;
}


ComponentTypeId
ComponentFactory::registerGlobalComponentType(
    const std::string& name,
    ComponentLoader loader
) {
    bool isNew = false;
    ComponentTypeId typeId = generateTypeId();
    std::tie(std::ignore, isNew) = globalRegistry().insert({
        name,
        std::make_pair(typeId, loader)
    });
    if (not isNew) {
        throw std::runtime_error("Duplicate component name: " + name);
    }
    return typeId;
}


ComponentFactory::ComponentFactory() 
  : m_impl(new Implementation())
{
}


ComponentFactory::~ComponentFactory() {}


ComponentTypeId
ComponentFactory::getTypeId(
    const std::string& name
) const {
    auto iter = globalRegistry().find(name);
    if (iter == globalRegistry().end()) {
        iter = m_impl->m_registry.find(name);
        if (iter == m_impl->m_registry.end()) {
            return NULL_COMPONENT_TYPE;
        }
    }
    return iter->second.first;
}


std::unique_ptr<Component>
ComponentFactory::load(
    const std::string& typeName,
    const StorageContainer& storage
) const {
    auto iter = globalRegistry().find(typeName);
    if (iter == globalRegistry().end()) {
        iter = m_impl->m_registry.find(typeName);
        if (iter == m_impl->m_registry.end()) {
            return nullptr;
        }
    }
    return iter->second.second(storage);
}


ComponentTypeId
ComponentFactory::registerComponentType(
    const std::string& name,
    ComponentLoader loader
) {
    bool isNew = false;
    ComponentTypeId typeId = generateTypeId();
    std::tie(std::ignore, isNew) = m_impl->m_registry.insert({
        name,
        std::make_pair(typeId, loader)
    });
    if (not isNew) {
        throw std::runtime_error("Duplicate component name: " + name);
    }
    return typeId;
}


void
ComponentFactory::unregisterComponentType(
    const std::string& name
) {
    m_impl->m_registry.erase(name);
}


