#include "ogre/keyboard.h"

#include "scripting/luabind.h"

#include <array>
#include <iostream>
#include <OISInputManager.h>
#include <OISKeyboard.h>

using namespace thrive;

struct Keyboard::Implementation : public OIS::KeyListener{

    using KeyStates = std::array<char, 256>;

    Implementation()
      : m_currentKeyStates(&m_bufferA),
        m_previousKeyStates(&m_bufferB)
    {
        m_bufferA.fill('\0');
        m_bufferB.fill('\0');
    }

    bool
    keyPressed(
        const OIS::KeyEvent& event
    ) {
        this->queueEvent(event, true);
        return true;
    }

    bool
    keyReleased(
        const OIS::KeyEvent& event
    ) {
        this->queueEvent(event, false);
        return true;
    }

    void
    queueEvent(
        const OIS::KeyEvent& event,
        bool pressed
    ) {
        bool alt = m_keyboard->isModifierDown(OIS::Keyboard::Alt);
        bool ctrl = m_keyboard->isModifierDown(OIS::Keyboard::Ctrl);
        bool shift = m_keyboard->isModifierDown(OIS::Keyboard::Shift);
        KeyEvent keyEvent = {event.key, pressed, alt, ctrl, shift};
        m_queue.push_back(keyEvent);
    }

    KeyStates m_bufferA;

    KeyStates m_bufferB;

    KeyStates* m_currentKeyStates = nullptr;

    OIS::InputManager* m_inputManager = nullptr;

    OIS::Keyboard* m_keyboard = nullptr;

    KeyStates* m_previousKeyStates = nullptr;

    std::list<KeyEvent> m_queue;

};


luabind::scope
Keyboard::luaBindings() {
    using namespace luabind;
    return class_<Keyboard>("Keyboard")
        .def("isKeyDown", &Keyboard::isKeyDown)
        .def("wasKeyPressed", &Keyboard::wasKeyPressed)
        .def("wasKeyReleased", &Keyboard::wasKeyReleased)
        .scope [
            class_<Keyboard::KeyEvent>("KeyEvent")
                .def_readonly("key", &Keyboard::KeyEvent::key)
                .def_readonly("alt", &Keyboard::KeyEvent::alt)
                .def_readonly("ctrl", &Keyboard::KeyEvent::ctrl)
                .def_readonly("shift", &Keyboard::KeyEvent::shift)
                .def_readonly("pressed", &Keyboard::KeyEvent::pressed)
        ]
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
    ;
}


Keyboard::Keyboard()
  : m_impl(new Implementation())
{
}


Keyboard::~Keyboard() {}


const std::list<Keyboard::KeyEvent>&
Keyboard::eventQueue() const {
    return m_impl->m_queue;
}


void
Keyboard::init(
    OIS::InputManager* inputManager
) {
    assert(m_impl->m_keyboard == nullptr && "Double init of keyboard system");
    m_impl->m_inputManager = inputManager;
    m_impl->m_keyboard = static_cast<OIS::Keyboard*>(
        inputManager->createInputObject(OIS::OISKeyboard, true)
    );
    m_impl->m_keyboard->setEventCallback(m_impl.get());
}


bool
Keyboard::isKeyDown(
    OIS::KeyCode key
) const {
    return m_impl->m_currentKeyStates->at(key) == 1;
}


void
Keyboard::shutdown() {
    m_impl->m_inputManager->destroyInputObject(m_impl->m_keyboard);
    m_impl->m_inputManager = nullptr;
    m_impl->m_keyboard = nullptr;
}


void
Keyboard::update() {
    m_impl->m_queue.clear();
    m_impl->m_keyboard->capture();
    std::swap(m_impl->m_currentKeyStates, m_impl->m_previousKeyStates);
    m_impl->m_keyboard->copyKeyStates(m_impl->m_currentKeyStates->data());
}


bool
Keyboard::wasKeyPressed(
    OIS::KeyCode key
) const {
    bool previous = m_impl->m_previousKeyStates->at(key);
    bool current = m_impl->m_currentKeyStates->at(key);
    return not previous and current;
}


bool
Keyboard::wasKeyReleased(
    OIS::KeyCode key
) const {
    bool previous = m_impl->m_previousKeyStates->at(key);
    bool current = m_impl->m_currentKeyStates->at(key);
    return previous and not current;
}



