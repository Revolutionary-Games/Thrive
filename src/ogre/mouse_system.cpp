#include "ogre/mouse_system.h"

#include "engine/engine.h"
#include "scripting/luabind.h"

#include <iostream>
#include <OgreVector3.h>
#include <OISInputManager.h>
#include <OISMouse.h>

using namespace thrive;

struct MouseSystem::Implementation {

    OIS::Mouse* m_mouse = nullptr;

    int m_windowWidth = 0;

    int m_windowHeight = 0;

};


luabind::scope
MouseSystem::luaBindings() {
    using namespace luabind;
    return class_<MouseSystem>("MouseSystem")
        .def("isButtonDown", &MouseSystem::isButtonDown)
        .def("normalizedPosition", &MouseSystem::normalizedPosition)
        .def("position", &MouseSystem::position)
    ;
}


MouseSystem::MouseSystem()
  : m_impl(new Implementation())
{
}


MouseSystem::~MouseSystem() {}


void
MouseSystem::init(
    Engine* engine
) {
    System::init(engine);
    assert(m_impl->m_mouse == nullptr && "Double init of mouse system");
    m_impl->m_mouse = static_cast<OIS::Mouse*>(
        engine->inputManager()->createInputObject(OIS::OISMouse, false)
    );
    this->setWindowSize(
        m_impl->m_windowWidth,
        m_impl->m_windowHeight
    );
}


bool
MouseSystem::isButtonDown(
    OIS::MouseButtonID button
) const {
    return m_impl->m_mouse->getMouseState().buttonDown(button);
}


Ogre::Vector3
MouseSystem::normalizedPosition() const {
    const OIS::MouseState& mouseState = m_impl->m_mouse->getMouseState();
    return Ogre::Vector3(
        double(mouseState.X.abs) / mouseState.width,
        double(mouseState.Y.abs) / mouseState.height,
        mouseState.Z.abs
    );
}


Ogre::Vector3
MouseSystem::position() const {
    return Ogre::Vector3(
        m_impl->m_mouse->getMouseState().X.abs,
        m_impl->m_mouse->getMouseState().Y.abs,
        m_impl->m_mouse->getMouseState().Z.abs
    );
}


void
MouseSystem::setWindowSize(
    int width,
    int height
) {
    if (m_impl->m_mouse) {
        m_impl->m_mouse->getMouseState().width = width;
        m_impl->m_mouse->getMouseState().height = height;
    }
    m_impl->m_windowWidth = width;
    m_impl->m_windowHeight = height;
}


void
MouseSystem::shutdown() {
    this->engine()->inputManager()->destroyInputObject(m_impl->m_mouse);
    m_impl->m_mouse = nullptr;
    System::shutdown();
}


void
MouseSystem::update(int) {
    m_impl->m_mouse->capture();
}




