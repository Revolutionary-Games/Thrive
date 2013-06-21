#pragma once

#include "engine/component.h"
#include "engine/system.h"
#include "scripting/luabind.h"

#include <memory>


namespace thrive {

/**
* @brief Notifies scripts of key events
*/
class OnKeyComponent : public Component {
    COMPONENT(OnKey)

public:

    /**
    * @brief Lua bindings
    *
    * Exposes the following properties:
    * - \c onPressed: A function that takes a KeyEvent for pressed keys
    * - \c onReleased: A function that takes a KeyEvent for released keys
    *
    * Exposes the type \c KeyEvent with the following properties:
    * - \c key: KeyboardSystem::KeyEvent::key
    * - \c alt: KeyboardSystem::KeyEvent::alt
    * - \c ctrl: KeyboardSystem::KeyEvent::ctrl
    * - \c shift: KeyboardSystem::KeyEvent::shift
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Called when a key was pressed
    */
    luabind::object m_onPressedCallback;

    /**
    * @brief Called when a key was released
    */
    luabind::object m_onReleasedCallback;

};


/**
* @brief Calls the callbacks of OnKeyComponents
*/
class OnKeySystem : public System {

public:

    /**
    * @brief Constructor
    */
    OnKeySystem();

    /**
    * @brief Destructor
    */
    ~OnKeySystem();

    /**
    * @brief Initializes the system
    *
    * @param engine
    */
    void init(Engine* engine) override;

    /**
    * @brief Shuts down the system
    */
    void shutdown() override;

    /**
    * @brief Calls the components' callbacks for key events
    */
    void update(int) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}

