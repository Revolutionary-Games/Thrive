#include "ogre/keyboard.h"

#include "scripting/luajit.h"

#include <CEGUI/CEGUI.h>

#include <array>
#include <iostream>
#include <OISInputManager.h>
#include <OISKeyboard.h>

#include <CEGUI/InputAggregator.h>

using namespace thrive;

struct Keyboard::Implementation : public OIS::KeyListener{

    using KeyStates = std::array<char, 256>;

    Implementation()
      : m_currentKeyStates(&m_bufferA),
        m_previousKeyStates(&m_bufferB)
    {
        m_bufferA.fill('\0');
        m_bufferB.fill('\0');
        m_keysHeld.fill('\0');
    }

    bool
    keyPressed(
        const OIS::KeyEvent& event
    ) {
        // TODO: cache this for a single frame
        m_aggregator->setModifierKeys(
            m_keyboard->isModifierDown(OIS::Keyboard::Shift),
            m_keyboard->isModifierDown(OIS::Keyboard::Alt),
            m_keyboard->isModifierDown(OIS::Keyboard::Ctrl)
        );

        // Because we use CEGUI InputAggregator handling on key down this properly returns
        // true only when the input is actually used
        if(m_aggregator->injectKeyDown(static_cast<CEGUI::Key::Scan>(
                    static_cast<int>(event.key))))
        {

            return true;
        }

        if(m_aggregator->injectChar(event.text)){

            return true;
        }

        m_keysHeld.data()[event.key] = 1;
        m_previousKeyStates->data()[event.key] = 1;
        this->queueEvent(event, true);
        return true;
    }

    bool
    keyReleased(
        const OIS::KeyEvent& event
    ) {
        m_aggregator->setModifierKeys(
            m_keyboard->isModifierDown(OIS::Keyboard::Shift),
            m_keyboard->isModifierDown(OIS::Keyboard::Alt),
            m_keyboard->isModifierDown(OIS::Keyboard::Ctrl)
        );
        // Aggregator is not configured to handle keys in key up so the result can always
        // be ignored
        m_aggregator->injectKeyUp(static_cast<CEGUI::Key::Scan>(static_cast<int>(event.key)));


        m_keysHeld.data()[event.key] = 0;
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

    KeyStates m_keysHeld;

    KeyStates* m_currentKeyStates = nullptr;

    OIS::InputManager* m_inputManager = nullptr;

    OIS::Keyboard* m_keyboard = nullptr;

    KeyStates* m_previousKeyStates = nullptr;

    std::list<KeyEvent> m_queue;

    CEGUI::InputAggregator* m_aggregator;

};


void Keyboard::luaBindings(
    sol::state &lua
){
    lua.new_usertype<Keyboard::KeyEvent>("KeyboardKeyEvent",

        "key", sol::readonly(&Keyboard::KeyEvent::key),
        "alt", sol::readonly(&Keyboard::KeyEvent::alt),
        "ctrl", sol::readonly(&Keyboard::KeyEvent::ctrl),
        "shift", sol::readonly(&Keyboard::KeyEvent::shift),
        "pressed", sol::readonly(&Keyboard::KeyEvent::pressed)
    );
    
    lua.new_usertype<Keyboard>("Keyboard",

        "new", sol::no_constructor,

        "isKeyDown", &Keyboard::isKeyDown,
        "wasKeyPressed", &Keyboard::wasKeyPressed,
        "wasKeyReleased", &Keyboard::wasKeyReleased
    );

    // This cannot be a template because template recursion goes too deep
    auto table = lua.create_table();

    table.set("KC_UNASSIGNED", OIS::KC_UNASSIGNED);
    table.set("KC_ESCAPE", OIS::KC_ESCAPE);
    table.set("KC_1", OIS::KC_1);
    table.set("KC_2", OIS::KC_2);
    table.set("KC_3", OIS::KC_3);
    table.set("KC_4", OIS::KC_4);
    table.set("KC_5", OIS::KC_5);
    table.set("KC_6", OIS::KC_6);
    table.set("KC_7", OIS::KC_7);
    table.set("KC_8", OIS::KC_8);
    table.set("KC_9", OIS::KC_9);
    table.set("KC_0", OIS::KC_0);
    table.set("KC_MINUS", OIS::KC_MINUS);
    table.set("KC_EQUALS", OIS::KC_EQUALS);
    table.set("KC_BACK", OIS::KC_BACK);
    table.set("KC_TAB", OIS::KC_TAB);
    table.set("KC_Q", OIS::KC_Q);
    table.set("KC_W", OIS::KC_W);
    table.set("KC_E", OIS::KC_E);
    table.set("KC_R", OIS::KC_R);
    table.set("KC_T", OIS::KC_T);
    table.set("KC_Y", OIS::KC_Y);
    table.set("KC_U", OIS::KC_U);
    table.set("KC_I", OIS::KC_I);
    table.set("KC_O", OIS::KC_O);
    table.set("KC_P", OIS::KC_P);
    table.set("KC_LBRACKET", OIS::KC_LBRACKET);
    table.set("KC_RBRACKET", OIS::KC_RBRACKET);
    table.set("KC_RETURN", OIS::KC_RETURN);
    table.set("KC_LCONTROL", OIS::KC_LCONTROL);
    table.set("KC_A", OIS::KC_A);
    table.set("KC_S", OIS::KC_S);
    table.set("KC_D", OIS::KC_D);
    table.set("KC_F", OIS::KC_F);
    table.set("KC_G", OIS::KC_G);
    table.set("KC_H", OIS::KC_H);
    table.set("KC_J", OIS::KC_J);
    table.set("KC_K", OIS::KC_K);
    table.set("KC_L", OIS::KC_L);
    table.set("KC_SEMICOLON", OIS::KC_SEMICOLON);
    table.set("KC_APOSTROPHE", OIS::KC_APOSTROPHE);
    table.set("KC_GRAVE", OIS::KC_GRAVE);
    table.set("KC_LSHIFT", OIS::KC_LSHIFT);
    table.set("KC_BACKSLASH", OIS::KC_BACKSLASH);
    table.set("KC_Z", OIS::KC_Z);
    table.set("KC_X", OIS::KC_X);
    table.set("KC_C", OIS::KC_C);
    table.set("KC_V", OIS::KC_V);
    table.set("KC_B", OIS::KC_B);
    table.set("KC_N", OIS::KC_N);
    table.set("KC_M", OIS::KC_M);
    table.set("KC_COMMA", OIS::KC_COMMA);
    table.set("KC_PERIOD", OIS::KC_PERIOD);
    table.set("KC_SLASH", OIS::KC_SLASH);
    table.set("KC_RSHIFT", OIS::KC_RSHIFT);
    table.set("KC_MULTIPLY", OIS::KC_MULTIPLY);
    table.set("KC_LMENU", OIS::KC_LMENU);
    table.set("KC_SPACE", OIS::KC_SPACE);
    table.set("KC_CAPITAL", OIS::KC_CAPITAL);
    table.set("KC_F1", OIS::KC_F1);
    table.set("KC_F2", OIS::KC_F2);
    table.set("KC_F3", OIS::KC_F3);
    table.set("KC_F4", OIS::KC_F4);
    table.set("KC_F5", OIS::KC_F5);
    table.set("KC_F6", OIS::KC_F6);
    table.set("KC_F7", OIS::KC_F7);
    table.set("KC_F8", OIS::KC_F8);
    table.set("KC_F9", OIS::KC_F9);
    table.set("KC_F10", OIS::KC_F10);
    table.set("KC_NUMLOCK", OIS::KC_NUMLOCK);
    table.set("KC_SCROLL", OIS::KC_SCROLL);
    table.set("KC_NUMPAD7", OIS::KC_NUMPAD7);
    table.set("KC_NUMPAD8", OIS::KC_NUMPAD8);
    table.set("KC_NUMPAD9", OIS::KC_NUMPAD9);
    table.set("KC_SUBTRACT", OIS::KC_SUBTRACT);
    table.set("KC_NUMPAD4", OIS::KC_NUMPAD4);
    table.set("KC_NUMPAD5", OIS::KC_NUMPAD5);
    table.set("KC_NUMPAD6", OIS::KC_NUMPAD6);
    table.set("KC_ADD", OIS::KC_ADD);
    table.set("KC_NUMPAD1", OIS::KC_NUMPAD1);
    table.set("KC_NUMPAD2", OIS::KC_NUMPAD2);
    table.set("KC_NUMPAD3", OIS::KC_NUMPAD3);
    table.set("KC_NUMPAD0", OIS::KC_NUMPAD0);
    table.set("KC_DECIMAL", OIS::KC_DECIMAL);
    table.set("KC_OEM_102", OIS::KC_OEM_102);
    table.set("KC_F11", OIS::KC_F11);
    table.set("KC_F12", OIS::KC_F12);
    table.set("KC_F13", OIS::KC_F13);
    table.set("KC_F14", OIS::KC_F14);
    table.set("KC_F15", OIS::KC_F15);
    table.set("KC_KANA", OIS::KC_KANA);
    table.set("KC_ABNT_C1", OIS::KC_ABNT_C1);
    table.set("KC_CONVERT", OIS::KC_CONVERT);
    table.set("KC_NOCONVERT", OIS::KC_NOCONVERT);
    table.set("KC_YEN", OIS::KC_YEN);
    table.set("KC_ABNT_C2", OIS::KC_ABNT_C2);
    table.set("KC_NUMPADEQUALS", OIS::KC_NUMPADEQUALS);
    table.set("KC_PREVTRACK", OIS::KC_PREVTRACK);
    table.set("KC_AT", OIS::KC_AT);
    table.set("KC_COLON", OIS::KC_COLON);
    table.set("KC_UNDERLINE", OIS::KC_UNDERLINE);
    table.set("KC_KANJI", OIS::KC_KANJI);
    table.set("KC_STOP", OIS::KC_STOP);
    table.set("KC_AX", OIS::KC_AX);
    table.set("KC_UNLABELED", OIS::KC_UNLABELED);
    table.set("KC_NEXTTRACK", OIS::KC_NEXTTRACK);
    table.set("KC_NUMPADENTER", OIS::KC_NUMPADENTER);
    table.set("KC_RCONTROL", OIS::KC_RCONTROL);
    table.set("KC_MUTE", OIS::KC_MUTE);
    table.set("KC_CALCULATOR", OIS::KC_CALCULATOR);
    table.set("KC_PLAYPAUSE", OIS::KC_PLAYPAUSE);
    table.set("KC_MEDIASTOP", OIS::KC_MEDIASTOP);
    table.set("KC_VOLUMEDOWN", OIS::KC_VOLUMEDOWN);
    table.set("KC_VOLUMEUP", OIS::KC_VOLUMEUP);
    table.set("KC_WEBHOME", OIS::KC_WEBHOME);
    table.set("KC_NUMPADCOMMA", OIS::KC_NUMPADCOMMA);
    table.set("KC_DIVIDE", OIS::KC_DIVIDE);
    table.set("KC_SYSRQ", OIS::KC_SYSRQ);
    table.set("KC_RMENU", OIS::KC_RMENU);
    table.set("KC_PAUSE", OIS::KC_PAUSE);
    table.set("KC_HOME", OIS::KC_HOME);
    table.set("KC_UP", OIS::KC_UP);
    table.set("KC_PGUP", OIS::KC_PGUP);
    table.set("KC_LEFT", OIS::KC_LEFT);
    table.set("KC_RIGHT", OIS::KC_RIGHT);
    table.set("KC_END", OIS::KC_END);
    table.set("KC_DOWN", OIS::KC_DOWN);
    table.set("KC_PGDOWN", OIS::KC_PGDOWN);
    table.set("KC_INSERT", OIS::KC_INSERT);
    table.set("KC_DELETE", OIS::KC_DELETE);
    table.set("KC_LWIN", OIS::KC_LWIN);
    table.set("KC_RWIN", OIS::KC_RWIN);
    table.set("KC_APPS", OIS::KC_APPS);
    table.set("KC_POWER", OIS::KC_POWER);
    table.set("KC_SLEEP", OIS::KC_SLEEP);
    table.set("KC_WAKE", OIS::KC_WAKE);
    table.set("KC_WEBSEARCH", OIS::KC_WEBSEARCH);
    table.set("KC_WEBFAVORITES", OIS::KC_WEBFAVORITES);
    table.set("KC_WEBREFRESH", OIS::KC_WEBREFRESH);
    table.set("KC_WEBSTOP", OIS::KC_WEBSTOP);
    table.set("KC_WEBFORWARD", OIS::KC_WEBFORWARD);
    table.set("KC_WEBBACK", OIS::KC_WEBBACK);
    table.set("KC_MYCOMPUTER", OIS::KC_MYCOMPUTER);
    table.set("KC_MAIL", OIS::KC_MAIL);
    table.set("KC_MEDIASELECT", OIS::KC_MEDIASELECT);
    
    lua["KEYCODE"] = table;

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
    OIS::InputManager* inputManager,
    CEGUI::InputAggregator* aggregator
) {
    assert(m_impl->m_keyboard == nullptr && "Double init of keyboard system");
    m_impl->m_inputManager = inputManager;
    m_impl->m_aggregator = aggregator;
    m_impl->m_keyboard = static_cast<OIS::Keyboard*>(
        inputManager->createInputObject(OIS::OISKeyboard, true)
    );
    m_impl->m_keyboard->setEventCallback(m_impl.get());
}



bool
Keyboard::isKeyDown(
    OIS::KeyCode key
) const {
    return m_impl->m_keysHeld[key] == 1;
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
    m_impl->m_previousKeyStates->fill('\0');
    m_impl->m_keyboard->capture();
    std::swap(m_impl->m_currentKeyStates, m_impl->m_previousKeyStates);
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
