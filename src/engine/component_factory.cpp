#include "engine/component_factory.h"

#include "scripting/luajit.h"

#include "scripting/wrapper_classes.h"

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
    sol::object cls
) {
    
    auto type = cls.get_type();
    
    if (type != sol::type::userdata) {

        std::string typeName(lua_typename(cls.lua_state(), static_cast<int>(type)));
        
        throw std::runtime_error("Argument 2 must be class object, but is: " + typeName);
    }
    
    ComponentTypeId typeId = self->registerComponentType(
        name,
        [cls] (const StorageContainer& storage) {
            sol::object classTable = cls;
            sol::table obj = classTable.as<sol::table>().get<sol::function>("new")();
            auto component = std::unique_ptr<Component>(
                new ComponentWrapper(obj)
            );
            component->load(storage);
            return component;
        }
    );
    
    cls.as<sol::table>()["TYPE_ID"] = typeId;
    return typeId;
}

void ComponentFactory::luaBindings(
    sol::state &lua
){
    lua.new_usertype<ComponentFactory>("ComponentFactory",

        "new", sol::no_constructor,
        "registerComponentType", &ComponentFactory_registerComponentType
    );
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


std::string
ComponentFactory::getTypeName(
    ComponentTypeId typeId
) const {
    for (const auto& item : globalRegistry()) {
        if (item.second.first == typeId) {
            return item.first;
        }
    }
    for (const auto& item : m_impl->m_registry) {
        if (item.second.first == typeId) {
            return item.first;
        }
    }
    return "";
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
    std::unique_ptr<Component> component = iter->second.second(storage);
    return component;
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


