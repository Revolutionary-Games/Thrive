#include "wrapper_classes.h"

#include "engine/component_factory.h"
#include "engine/engine.h"
#include "engine/serialization.h"
#include "game.h"

#include <CEGUI/CEGUI.h>
#include <CEGUI/views/StandardItemModel.h>


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
    m_luaObject.get<sol::protected_function>("load")(storage);
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
    return m_luaObject[sol::metatable_key]["__name"];
}

StorageContainer ComponentWrapper::storage() const
{
    const auto result = m_luaObject.get<sol::protected_function>("storage")();

    if(!result.valid())
        throw std::runtime_error("lua component failed to return storage object");

    return result.get<StorageContainer>();
}

StorageContainer ComponentWrapper::default_storage(
    Component* self
) {
    return self->Component::storage();
}


// ------------------------------------ //
// StandardItemWrapper
// ------------------------------------ //
StandardItemWrapper::StandardItemWrapper(
    const std::string &text,
    int id) :
    m_attached(false)
{
        
    m_item = new CEGUI::StandardItem(text, id);
}

StandardItemWrapper::~StandardItemWrapper(){

    if(!m_attached && m_item){

        delete m_item;
        m_item = nullptr;
    }
}

CEGUI::StandardItem*
    StandardItemWrapper::getItem(){

    return m_item;
}

void
    StandardItemWrapper::markAttached(){

    m_attached = true;
}

