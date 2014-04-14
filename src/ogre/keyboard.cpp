#include "ogre/keyboard.h"

#include "scripting/luabind.h"

#include <CEGUI/CEGUI.h>

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
        CEGUI::System::getSingleton().getDefaultGUIContext().injectKeyDown(static_cast<CEGUI::Key::Scan>(static_cast<int>(event.key)));
        CEGUI::System::getSingleton().getDefaultGUIContext().injectChar(event.text);
        return true;
    }

    bool
    keyReleased(
        const OIS::KeyEvent& event
    ) {
        this->queueEvent(event, false);
        CEGUI::System::getSingleton().getDefaultGUIContext().injectKeyUp(static_cast<CEGUI::Key::Scan>(static_cast<int>(event.key)));
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
            value("KC_F10", OIS::KC_F10),
            value("KC_NUMLOCK", OIS::KC_NUMLOCK),
            value("KC_SCROLL", OIS::KC_SCROLL),
            value("KC_NUMPAD7", OIS::KC_NUMPAD7),
            value("KC_NUMPAD8", OIS::KC_NUMPAD8),
            value("KC_NUMPAD9", OIS::KC_NUMPAD9),
            value("KC_SUBTRACT", OIS::KC_SUBTRACT),
            value("KC_NUMPAD4", OIS::KC_NUMPAD4),
            value("KC_NUMPAD5", OIS::KC_NUMPAD5),
            value("KC_NUMPAD6", OIS::KC_NUMPAD6),
            value("KC_ADD", OIS::KC_ADD),
            value("KC_NUMPAD1", OIS::KC_NUMPAD1),
            value("KC_NUMPAD2", OIS::KC_NUMPAD2),
            value("KC_NUMPAD3", OIS::KC_NUMPAD3),
            value("KC_NUMPAD0", OIS::KC_NUMPAD0),
            value("KC_DECIMAL", OIS::KC_DECIMAL),
            value("KC_OEM_102", OIS::KC_OEM_102),
            value("KC_F11", OIS::KC_F11),
            value("KC_F12", OIS::KC_F12),
            value("KC_F13", OIS::KC_F13),
            value("KC_F14", OIS::KC_F14),
            value("KC_F15", OIS::KC_F15),
            value("KC_KANA", OIS::KC_KANA),
            value("KC_ABNT_C1", OIS::KC_ABNT_C1),
            value("KC_CONVERT", OIS::KC_CONVERT),
            value("KC_NOCONVERT", OIS::KC_NOCONVERT),
            value("KC_YEN", OIS::KC_YEN),
            value("KC_ABNT_C2", OIS::KC_ABNT_C2),
            value("KC_NUMPADEQUALS", OIS::KC_NUMPADEQUALS),
            value("KC_PREVTRACK", OIS::KC_PREVTRACK),
            value("KC_AT", OIS::KC_AT),
            value("KC_COLON", OIS::KC_COLON),
            value("KC_UNDERLINE", OIS::KC_UNDERLINE),
            value("KC_KANJI", OIS::KC_KANJI),
            value("KC_STOP", OIS::KC_STOP),
            value("KC_AX", OIS::KC_AX),
            value("KC_UNLABELED", OIS::KC_UNLABELED),
            value("KC_NEXTTRACK", OIS::KC_NEXTTRACK),
            value("KC_NUMPADENTER", OIS::KC_NUMPADENTER),
            value("KC_RCONTROL", OIS::KC_RCONTROL),
            value("KC_MUTE", OIS::KC_MUTE),
            value("KC_CALCULATOR", OIS::KC_CALCULATOR),
            value("KC_PLAYPAUSE", OIS::KC_PLAYPAUSE),
            value("KC_MEDIASTOP", OIS::KC_MEDIASTOP),
            value("KC_VOLUMEDOWN", OIS::KC_VOLUMEDOWN),
            value("KC_VOLUMEUP", OIS::KC_VOLUMEUP),
            value("KC_WEBHOME", OIS::KC_WEBHOME),
            value("KC_NUMPADCOMMA", OIS::KC_NUMPADCOMMA),
            value("KC_DIVIDE", OIS::KC_DIVIDE),
            value("KC_SYSRQ", OIS::KC_SYSRQ),
            value("KC_RMENU", OIS::KC_RMENU),
            value("KC_PAUSE", OIS::KC_PAUSE),
            value("KC_HOME", OIS::KC_HOME),
            value("KC_UP", OIS::KC_UP),
            value("KC_PGUP", OIS::KC_PGUP),
            value("KC_LEFT", OIS::KC_LEFT),
            value("KC_RIGHT", OIS::KC_RIGHT),
            value("KC_END", OIS::KC_END),
            value("KC_DOWN", OIS::KC_DOWN),
            value("KC_PGDOWN", OIS::KC_PGDOWN),
            value("KC_INSERT", OIS::KC_INSERT),
            value("KC_DELETE", OIS::KC_DELETE),
            value("KC_LWIN", OIS::KC_LWIN),
            value("KC_RWIN", OIS::KC_RWIN),
            value("KC_APPS", OIS::KC_APPS),
            value("KC_POWER", OIS::KC_POWER),
            value("KC_SLEEP", OIS::KC_SLEEP),
            value("KC_WAKE", OIS::KC_WAKE),
            value("KC_WEBSEARCH", OIS::KC_WEBSEARCH),
            value("KC_WEBFAVORITES", OIS::KC_WEBFAVORITES),
            value("KC_WEBREFRESH", OIS::KC_WEBREFRESH),
            value("KC_WEBSTOP", OIS::KC_WEBSTOP),
            value("KC_WEBFORWARD", OIS::KC_WEBFORWARD),
            value("KC_WEBBACK", OIS::KC_WEBBACK),
            value("KC_MYCOMPUTER", OIS::KC_MYCOMPUTER),
            value("KC_MAIL", OIS::KC_MAIL),
            value("KC_MEDIASELECT", OIS::KC_MEDIASELECT)
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



