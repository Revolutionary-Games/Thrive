#include "wrapper_classes.h"

#include "engine/component_factory.h"
#include "engine/engine.h"
#include "engine/serialization.h"
#include "game.h"

using namespace thrive;

// ------------------------------------ //
// ComponentWrapper
// ------------------------------------ //

ComponentWrapper::ComponentWrapper(
    sol::table obj
) : ScriptWrapper(obj)
{
}

void ComponentWrapper::load(
        const StorageContainer& storage
) {
    auto func = m_luaObject.get<sol::protected_function>("load");

    if(!func){

        default_load(this, storage);
        return;
    }
    
    func(storage);
}

void ComponentWrapper::default_load(
    Component* self, 
    const StorageContainer& storage
) {
    self->Component::load(storage);
}

ComponentTypeId ComponentWrapper::typeId() const
{
    return Game::instance().engine().componentFactory().getTypeId(
        this->typeName()
    );
}

std::string ComponentWrapper::typeName() const
{
    // TODO: make sure this is correct
    // TODOSOL: this actually needs to be set maually in the Lua side
    return m_luaObject[sol::metatable_key]["__name"];
}

StorageContainer ComponentWrapper::storage() const
{
    auto func = m_luaObject.get<sol::protected_function>("storage");

    if(!func){

        return default_storage(this);
    }
    
    const auto result = func();

    if(!result.valid())
        throw std::runtime_error("lua component failed to return storage object");

    return result.get<StorageContainer>();
}

StorageContainer ComponentWrapper::default_storage(
    const Component* self
) {
    return self->Component::storage();
}
// ------------------------------------ //
// SystemWrapper
// ------------------------------------ //
SystemWrapper::SystemWrapper(sol::table obj) : ScriptWrapper(obj){
    
}

void SystemWrapper::init(
    GameState* gameState
) {
    System::init(gameState);
    m_luaObject.get<sol::protected_function>("init")(gameState);
}

void SystemWrapper::initNamed(
    const std::string &name,
    GameState* gameState
) {
    System::initNamed(name, gameState);

    auto func =  m_luaObject.get<sol::protected_function>("initNamed");

    if(!func){

        default_initNamed(this, name, gameState);
        return;
    }
    
    func(name, gameState);
}

void SystemWrapper::shutdown(){

    System::shutdown();

    auto func = m_luaObject.get<sol::protected_function>("shutdown");

    if(!func){

        default_shutdown(this);
        return;
    }
    
    func();
}

void SystemWrapper::activate(){

    auto func = m_luaObject.get<sol::protected_function>("activate");

    if(!func){

        default_activate(this);
        return;
    }
    
    func();
}

void SystemWrapper::deactivate(){

    auto func = m_luaObject.get<sol::protected_function>("deactivate");

    if(!func){

        default_deactivate(this);
        return;
    }
    
    func();
}

void SystemWrapper::update(
    int renderTime,
    int logicTime
) {

    auto func = m_luaObject.get<sol::protected_function>("update");

    if(!func){

        System::update(renderTime, logicTime);
        return;
    }
    
    func(renderTime, logicTime);
}

