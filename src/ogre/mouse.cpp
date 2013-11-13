#include "ogre/mouse.h"

#include "scripting/luabind.h"

#include <iostream>
#include <OgreVector3.h>
#include <OISInputManager.h>
#include <OISMouse.h>

using namespace thrive;

struct Mouse::Implementation {

    OIS::InputManager* m_inputManager = nullptr;

    OIS::Mouse* m_mouse = nullptr;

    int m_windowWidth = 0;

    int m_windowHeight = 0;

};


luabind::scope
Mouse::luaBindings() {
    using namespace luabind;
    return class_<Mouse>("Mouse")
        .enum_("MouseButton") [
            value("MB_Left", OIS::MB_Left),
            value("MB_Right", OIS::MB_Right),
            value("MB_Middle", OIS::MB_Middle),
            value("MB_Button3", OIS::MB_Button3),
            value("MB_Button4", OIS::MB_Button4),
            value("MB_Button5", OIS::MB_Button5),
            value("MB_Button6", OIS::MB_Button6),
            value("MB_Button7", OIS::MB_Button7)
        ]
        .def("isButtonDown", &Mouse::isButtonDown)
        .def("normalizedPosition", &Mouse::normalizedPosition)
        .def("position", &Mouse::position)
    ;
}


Mouse::Mouse()
  : m_impl(new Implementation())
{
}


Mouse::~Mouse() {}


void
Mouse::init(
    OIS::InputManager* inputManager
) {
    assert(m_impl->m_mouse == nullptr && "Double init of mouse system");
    m_impl->m_mouse = static_cast<OIS::Mouse*>(
        inputManager->createInputObject(OIS::OISMouse, false)
    );
    m_impl->m_inputManager = inputManager;
    this->setWindowSize(
        m_impl->m_windowWidth,
        m_impl->m_windowHeight
    );
}


bool
Mouse::isButtonDown(
    OIS::MouseButtonID button
) const {
    return m_impl->m_mouse->getMouseState().buttonDown(button);
}


Ogre::Vector3
Mouse::normalizedPosition() const {
    const OIS::MouseState& mouseState = m_impl->m_mouse->getMouseState();
    return Ogre::Vector3(
        double(mouseState.X.abs) / mouseState.width,
        double(mouseState.Y.abs) / mouseState.height,
        mouseState.Z.abs
    );
}


Ogre::Vector3
Mouse::position() const {
    return Ogre::Vector3(
        m_impl->m_mouse->getMouseState().X.abs,
        m_impl->m_mouse->getMouseState().Y.abs,
        m_impl->m_mouse->getMouseState().Z.abs
    );
}


void
Mouse::setWindowSize(
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
Mouse::shutdown() {
    m_impl->m_inputManager->destroyInputObject(m_impl->m_mouse);
    m_impl->m_mouse = nullptr;
    m_impl->m_inputManager = nullptr;
}


void
Mouse::update() {
    m_impl->m_mouse->capture();
}




