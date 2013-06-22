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
    * @brief Called when a key was pressed
    *
    * Arguments:
    * - EntityId: The entity of this component
    * - KeyboardSystem::KeyEvent: The key event
    */
    luabind::object onPressedCallback;

    /**
    * @brief Called when a key was released
    *
    * Arguments:
    * - EntityId: The entity of this component
    * - KeyboardSystem::KeyEvent: The key event
    */
    luabind::object onReleasedCallback;

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - OnKeyComponent::OnKeyComponent()
    * - OnKeyComponent::onPressedCallback
    * - OnKeyComponent::onReleasedCallback
    */
    static luabind::scope
    luaBindings();

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

