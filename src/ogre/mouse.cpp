#include "ogre/mouse.h"

#include "scripting/luabind.h"

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
#ifdef CEGUI_USE_NEW
        m_aggregator->injectMousePosition(e.state.X.abs, e.state.Y.abs);

        m_aggregator->injectMouseWheelChange(e.state.Z.rel/100);
#else
        CEGUI::System::getSingleton().getDefaultGUIContext().injectMousePosition(e.state.X.abs, e.state.Y.abs );

        CEGUI::System::getSingleton().getDefaultGUIContext().injectMouseWheelChange(e.state.Z.rel/100);
#endif //CEGUI_USE_NEW

        
        return true;
    }
    bool mousePressed (const OIS::MouseEvent&, OIS::MouseButtonID id){

        switch(id){
        case OIS::MB_Left:
#ifdef CEGUI_USE_NEW
            m_aggregator->injectMouseButtonDown(CEGUI::MouseButton::LeftButton);
#else
            CEGUI::System::getSingleton().getDefaultGUIContext().injectMouseButtonDown(CEGUI::MouseButton::LeftButton);
#endif //CEGUI_USE_NEW            
            break;
        case OIS::MB_Right:
#ifdef CEGUI_USE_NEW
            m_aggregator->injectMouseButtonDown(CEGUI::MouseButton::RightButton);
#else
            CEGUI::System::getSingleton().getDefaultGUIContext().injectMouseButtonDown(CEGUI::MouseButton::RightButton);
#endif //CEGUI_USE_NEW
            break;
        case OIS::MB_Middle:
#ifdef CEGUI_USE_NEW
            m_aggregator->injectMouseButtonDown(CEGUI::MouseButton::MiddleButton);
#else
            CEGUI::System::getSingleton().getDefaultGUIContext().injectMouseButtonDown(CEGUI::MouseButton::MiddleButton);
#endif //CEGUI_USE_NEW
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
#ifdef CEGUI_USE_NEW
            if(!m_aggregator->injectMouseButtonUp(CEGUI::MouseButton::LeftButton)){

                m_nextClickedStates |= 0x1;
            }
#else
            if (not CEGUI::System::getSingleton().getDefaultGUIContext().injectMouseButtonUp(CEGUI::MouseButton::LeftButton))
            {

                //Activate the bit for this button only if CEGUI did not handle the click
                m_nextClickedStates |= 0x1;
            }
#endif //CEGUI_USE_NEW

            break;
        case OIS::MB_Right:
#ifdef CEGUI_USE_NEW
            if(!m_aggregator->injectMouseButtonUp(CEGUI::MouseButton::RightButton)){

                m_nextClickedStates |= 0x2;
            }
#else
            if (not CEGUI::System::getSingleton().getDefaultGUIContext().injectMouseButtonUp(CEGUI::MouseButton::RightButton))
            {

                m_nextClickedStates |= 0x2;
            }
#endif //CEGUI_USE_NEW
            break;
        case OIS::MB_Middle:
#ifdef CEGUI_USE_NEW
            if(!m_aggregator->injectMouseButtonUp(CEGUI::MouseButton::MiddleButton)){

                m_nextClickedStates |= 0x4;
            }
#else
            if (not CEGUI::System::getSingleton().getDefaultGUIContext().injectMouseButtonUp(CEGUI::MouseButton::MiddleButton))
            {

                m_nextClickedStates |= 0x4;
            }
#endif //CEGUI_USE_NEW
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

#ifdef CEGUI_USE_NEW
    CEGUI::InputAggregator* m_aggregator;
#endif //CEGUI_USE_NEW
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
        .def("wasButtonPressed", &Mouse::wasButtonPressed)
        .def("normalizedPosition", &Mouse::normalizedPosition)
        .def("scrollChange", &Mouse::scrollChange)
        .def("position", &Mouse::position)
    ;
}


Mouse::Mouse()
  : m_impl(new Implementation())
{
}


Mouse::~Mouse() {}

#ifdef CEGUI_USE_NEW
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
#else
void
Mouse::init(
    OIS::InputManager* inputManager
) {
    assert(m_impl->m_mouse == nullptr && "Double init of mouse system");
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
#endif //CEGUI_USE_NEW

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

