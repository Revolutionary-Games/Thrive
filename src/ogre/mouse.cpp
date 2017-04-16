#include "ogre/mouse.h"

#include "scripting/luajit.h"

#include <CEGUI/CEGUI.h>

#include <iostream>
#include <OgreVector3.h>
#include <OISInputManager.h>
#include <OISMouse.h>

// W
#pragma GCC diagnostic ignored "-Wswitch-enum"

using namespace thrive;

struct Mouse::Implementation : public OIS::MouseListener {

    bool mouseMoved (const OIS::MouseEvent& e){
        m_aggregator->injectMousePosition(e.state.X.abs, e.state.Y.abs);

        m_aggregator->injectMouseWheelChange(e.state.Z.rel/100);

        
        return true;
    }
    bool mousePressed (const OIS::MouseEvent&, OIS::MouseButtonID id){

        switch(id){
        case OIS::MB_Left:
            m_aggregator->injectMouseButtonDown(CEGUI::MouseButton::Left);
            break;
        case OIS::MB_Right:
            m_aggregator->injectMouseButtonDown(CEGUI::MouseButton::Right);
            break;
        case OIS::MB_Middle:
            m_aggregator->injectMouseButtonDown(CEGUI::MouseButton::Middle);
            break;
        default:
            break;
        }
        // TODO: Consider adding extra mouse buttons
        return true;
    }

    bool mouseReleased (const OIS::MouseEvent&, OIS::MouseButtonID id){
        switch(id){
        case OIS::MB_Left:
            if(!m_aggregator->injectMouseButtonUp(CEGUI::MouseButton::Left)){

                m_nextClickedStates |= 0x1;
            }
            break;
        case OIS::MB_Right:
            if(!m_aggregator->injectMouseButtonUp(CEGUI::MouseButton::Right)){

                m_nextClickedStates |= 0x2;
            }
            break;
        case OIS::MB_Middle:
            if(!m_aggregator->injectMouseButtonUp(CEGUI::MouseButton::Middle)){

                m_nextClickedStates |= 0x4;
            }
            break;
        default:
            break;
        }
        return true;
    }

    OIS::InputManager* m_inputManager = nullptr;

    OIS::Mouse* m_mouse = nullptr;

    uint8_t m_wasClickedStates = 0x0;

    uint8_t m_nextClickedStates = 0x0;

    int m_windowWidth = 0;

    int m_windowHeight = 0;

    int m_scrollChange = 0;
    int m_lastMouseZ = 0;

    CEGUI::InputAggregator* m_aggregator;
};


void Mouse::luaBindings(
    sol::state &lua
){
    lua.new_usertype<Mouse>("Mouse",

        "new", sol::no_constructor,

        // MB enum
        "MB_Left", sol::var(OIS::MB_Left),
        "MB_Right", sol::var(OIS::MB_Right),
        "MB_Middle", sol::var(OIS::MB_Middle),
        "MB_Button3", sol::var(OIS::MB_Button3),
        "MB_Button4", sol::var(OIS::MB_Button4),
        "MB_Button5", sol::var(OIS::MB_Button5),
        "MB_Button6", sol::var(OIS::MB_Button6),
        "MB_Button7", sol::var(OIS::MB_Button7),
        
        "isButtonDown", &Mouse::isButtonDown,
        "wasButtonPressed", &Mouse::wasButtonPressed,
        "normalizedPosition", &Mouse::normalizedPosition,
        "scrollChange", &Mouse::scrollChange,
        "position", &Mouse::position
    );
}

Mouse::Mouse()
  : m_impl(new Implementation())
{
}


Mouse::~Mouse() {}

void
Mouse::init(
    OIS::InputManager* inputManager,
    CEGUI::InputAggregator* aggregator
) {
    assert(m_impl->m_mouse == nullptr && "Double init of mouse system");
    m_impl->m_aggregator = aggregator;
    m_impl->m_mouse = static_cast<OIS::Mouse*>(
        inputManager->createInputObject(OIS::OISMouse, true)
    );
    m_impl->m_inputManager = inputManager;
    this->setWindowSize(
        m_impl->m_windowWidth,
        m_impl->m_windowHeight
    );
    m_impl->m_mouse->setEventCallback(m_impl.get());
}

bool
Mouse::isButtonDown(
    OIS::MouseButtonID button
) const {
    return m_impl->m_mouse->getMouseState().buttonDown(button);
}

bool
Mouse::wasButtonPressed(
    OIS::MouseButtonID button
) const {
    switch(button) {
    case OIS::MB_Left:
        return m_impl->m_wasClickedStates & 0x1;
        break;
    case OIS::MB_Right:
        return m_impl->m_wasClickedStates & 0x2;
        break;
    case OIS::MB_Middle:
        return m_impl->m_wasClickedStates & 0x4;
        break;
    default:
        return false;
    }
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

int
Mouse::scrollChange() const {
    return m_impl->m_scrollChange;
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
    int newScrollValue = m_impl->m_mouse->getMouseState().Z.abs;
    m_impl->m_scrollChange = m_impl->m_lastMouseZ - newScrollValue;
    m_impl->m_lastMouseZ = newScrollValue;
    std::swap(m_impl->m_wasClickedStates, m_impl->m_nextClickedStates);
    m_impl->m_nextClickedStates = 0x0;
}

