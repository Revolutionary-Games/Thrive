#include "ogre/on_key.h"

#include "engine/component_registry.h"
#include "engine/engine.h"
#include "engine/entity_filter.h"
#include "game.h"
#include "ogre/keyboard_system.h"
#include "scripting/luabind.h"


using namespace thrive;

luabind::scope
OnKeyComponent::luaBindings() {
    using namespace luabind;
    return 
        class_<OnKeyComponent, Component, std::shared_ptr<Component>>("OnKeyComponent")
            .scope[
                def("TYPE_NAME", &OnKeyComponent::TYPE_NAME),
                def("TYPE_ID", &OnKeyComponent::TYPE_ID)
            ]
            .def(constructor<>())
            .def_readwrite("onPressed", &OnKeyComponent::m_onPressedCallback)
            .def_readwrite("onReleased", &OnKeyComponent::m_onReleasedCallback)
        ,
        class_<KeyboardSystem::KeyEvent>("KeyEvent")
            .def_readonly("key", &KeyboardSystem::KeyEvent::key)
            .def_readonly("alt", &KeyboardSystem::KeyEvent::alt)
            .def_readonly("ctrl", &KeyboardSystem::KeyEvent::ctrl)
            .def_readonly("shift", &KeyboardSystem::KeyEvent::shift)
    ;
}

REGISTER_COMPONENT(OnKeyComponent)

////////////////////////////////////////////////////////////////////////////////
// OnKeySystem
////////////////////////////////////////////////////////////////////////////////

struct OnKeySystem::Implementation {

    EntityFilter<OnKeyComponent> m_entities;

    KeyboardSystem* m_keyboardSystem = nullptr;

};


OnKeySystem::OnKeySystem() 
  : m_impl(new Implementation())
{
}


OnKeySystem::~OnKeySystem() {}


void
OnKeySystem::init(
    Engine* engine
) {
    System::init(engine);
    m_impl->m_entities.setEntityManager(&engine->entityManager());
    m_impl->m_keyboardSystem = &(Game::instance().engine().keyboardSystem());
}


void
OnKeySystem::shutdown() {
    m_impl->m_entities.setEntityManager(nullptr);
    m_impl->m_keyboardSystem = nullptr;
    System::shutdown();
}


void
OnKeySystem::update(
    int
) {
    for (auto& value : m_impl->m_entities.entities()) {
        OnKeyComponent* component = std::get<0>(value.second);
        luabind::object& onPressed = component->m_onPressedCallback;
        luabind::object& onReleased = component->m_onReleasedCallback;
        EntityId entityId = value.first;
        for (const KeyboardSystem::KeyEvent& keyEvent : m_impl->m_keyboardSystem->eventQueue()) {
            try {
                if (keyEvent.pressed) {
                    onPressed(entityId, keyEvent);
                }
                else {
                    onReleased(entityId, keyEvent);
                }
            }
            catch(luabind::error& e) {
                luabind::object error_msg(luabind::from_stack(
                    e.state(),
                    -1
                ));
                // TODO: Log error
                std::cerr << error_msg << std::endl;
            }
        }
    }
}




