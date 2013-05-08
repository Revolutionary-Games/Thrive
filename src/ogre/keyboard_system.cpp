#include "ogre/keyboard_system.h"

#include "ogre/ogre_engine.h"

#include <iostream>
#include <OISInputManager.h>
#include <OISKeyboard.h>

using namespace thrive;

struct KeyboardSystem::Implementation : public OIS::KeyListener{

    bool
    keyPressed(
        const OIS::KeyEvent& event
    ) {
        std::cout << "Key pressed: " << int(event.key) << std::endl;
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
        m_queue.push(keyEvent);
    }

    OIS::Keyboard* m_keyboard;

    InputQueue<KeyEvent> m_queue;

};


KeyboardSystem::KeyboardSystem()
  : m_impl(new Implementation())
{
}


KeyboardSystem::~KeyboardSystem() {}


void
KeyboardSystem::init(
    Engine* engine
) {
    System::init(engine);
    assert(m_impl->m_keyboard == nullptr && "Double init of keyboard system");
    OgreEngine* ogreEngine = dynamic_cast<OgreEngine*>(engine);
    assert(ogreEngine != nullptr && "KeyboardSystem requires an OgreEngine");
    m_impl->m_keyboard = static_cast<OIS::Keyboard*>(
        ogreEngine->inputManager()->createInputObject(OIS::OISKeyboard, true)
    );
    m_impl->m_keyboard->setEventCallback(m_impl.get());
}


InputQueue<KeyboardSystem::KeyEvent>&
KeyboardSystem::eventQueue() {
    return m_impl->m_queue;
}


void
KeyboardSystem::shutdown() {
    OgreEngine* ogreEngine = dynamic_cast<OgreEngine*>(this->engine());
    ogreEngine->inputManager()->destroyInputObject(m_impl->m_keyboard);
    m_impl->m_keyboard = nullptr;
    System::shutdown();
}


void
KeyboardSystem::update(int) {
    m_impl->m_keyboard->capture();
}



