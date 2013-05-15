#include "ogre/on_key.h"

#include "engine/component_registry.h"
#include "engine/entity_filter.h"
#include "game.h"
#include "ogre/keyboard_system.h"
#include "ogre/ogre_engine.h"
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
            .enum_("KeyCode") [
                value("KC_UNASSIGNED", OIS::KC_UNASSIGNED),
                value("KC_ESCAPE", OIS::KC_ESCAPE),
                value("KC_1", OIS::KC_1),
                value("KC_2", OIS::KC_2),
                value("KC_3", OIS::KC_3),
                value("KC_4", OIS::KC_4),
                value("KC_5", OIS::KC_5),
                value("KC_6", OIS::KC_6),
                value("KC_7", OIS::KC_7),
                value("KC_8", OIS::KC_8),
                value("KC_9", OIS::KC_9),
                value("KC_0", OIS::KC_0),
                value("KC_MINUS", OIS::KC_MINUS),
                value("KC_EQUALS", OIS::KC_EQUALS),
                value("KC_BACK", OIS::KC_BACK),
                value("KC_TAB", OIS::KC_TAB),
                value("KC_Q", OIS::KC_Q),
                value("KC_W", OIS::KC_W),
                value("KC_E", OIS::KC_E),
                value("KC_R", OIS::KC_R),
                value("KC_T", OIS::KC_T),
                value("KC_Y", OIS::KC_Y),
                value("KC_U", OIS::KC_U),
                value("KC_I", OIS::KC_I),
                value("KC_O", OIS::KC_O),
                value("KC_P", OIS::KC_P),
                value("KC_LBRACKET", OIS::KC_LBRACKET),
                value("KC_RBRACKET", OIS::KC_RBRACKET),
                value("KC_RETURN", OIS::KC_RETURN),
                value("KC_LCONTROL", OIS::KC_LCONTROL),
                value("KC_A", OIS::KC_A),
                value("KC_S", OIS::KC_S),
                value("KC_D", OIS::KC_D),
                value("KC_F", OIS::KC_F),
                value("KC_G", OIS::KC_G),
                value("KC_H", OIS::KC_H),
                value("KC_J", OIS::KC_J),
                value("KC_K", OIS::KC_K),
                value("KC_L", OIS::KC_L),
                value("KC_SEMICOLON", OIS::KC_SEMICOLON),
                value("KC_APOSTROPHE", OIS::KC_APOSTROPHE),
                value("KC_GRAVE", OIS::KC_GRAVE),
                value("KC_LSHIFT", OIS::KC_LSHIFT),
                value("KC_BACKSLASH", OIS::KC_BACKSLASH),
                value("KC_Z", OIS::KC_Z),
                value("KC_X", OIS::KC_X),
                value("KC_C", OIS::KC_C),
                value("KC_V", OIS::KC_V),
                value("KC_B", OIS::KC_B),
                value("KC_N", OIS::KC_N),
                value("KC_M", OIS::KC_M),
                value("KC_COMMA", OIS::KC_COMMA),
                value("KC_PERIOD", OIS::KC_PERIOD),
                value("KC_SLASH", OIS::KC_SLASH),
                value("KC_RSHIFT", OIS::KC_RSHIFT),
                value("KC_MULTIPLY", OIS::KC_MULTIPLY),
                value("KC_LMENU", OIS::KC_LMENU),
                value("KC_SPACE", OIS::KC_SPACE),
                value("KC_CAPITAL", OIS::KC_CAPITAL),
                value("KC_F1", OIS::KC_F1),
                value("KC_F2", OIS::KC_F2),
                value("KC_F3", OIS::KC_F3),
                value("KC_F4", OIS::KC_F4),
                value("KC_F5", OIS::KC_F5),
                value("KC_F6", OIS::KC_F6),
                value("KC_F7", OIS::KC_F7),
                value("KC_F8", OIS::KC_F8),
                value("KC_F9", OIS::KC_F9),
                value("KC_F10", OIS::KC_F10)
                // To be continued (don't forget the comma above)
            ]
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

    std::shared_ptr<KeyboardSystem> m_keyboardSystem;

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
    m_impl->m_entities.setEngine(engine);
    m_impl->m_keyboardSystem = Game::instance().ogreEngine().keyboardSystem();
}


void
OnKeySystem::shutdown() {
    m_impl->m_entities.setEngine(nullptr);
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
            std::cout << "Processing event" << std::endl;
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




