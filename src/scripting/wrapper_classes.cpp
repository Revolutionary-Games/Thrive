#include "wrapper_classes.h"

#include "engine/component_factory.h"
#include "engine/engine.h"
#include "engine/serialization.h"
#include "engine/system.h"
#include "game.h"

using namespace thrive;

void
ComponentWrapper::luaBindings(sol::state &lua){

    lua.new_usertype<ComponentWrapper>("ComponentWrapper",

        sol::constructors<sol::types<sol::table>>()

        
    );
}

ComponentWrapper::ComponentWrapper(
    sol::table obj
) : ScriptWrapper(obj)
{
}

void
ComponentWrapper::load(
    const StorageContainer& storage
) {
    auto func = m_luaObject.get<sol::protected_function>("load");

    Component::load(storage);

    if(!func){

        return;
    }
    
    func(m_luaObject, &storage);
}

ComponentTypeId
ComponentWrapper::typeId(
) const{
    
    return Game::instance().engine().componentFactory().getTypeId(
        this->typeName()
    );
}

std::string
ComponentWrapper::typeName(
) const{
    
    // This needs to be set on the Lua side for each Component
    return m_luaObject["TYPE_NAME"];
}

StorageContainer
ComponentWrapper::storage(
) const{
    
    auto func = m_luaObject.get<sol::protected_function>("storage");

    auto stored = Component::storage();

    if(!func){

        return stored;
    }
    
    const auto result = func(m_luaObject, &stored);

    if(!result.valid())
        throw std::runtime_error("lua component failed to return storage object");

    return stored;
}


