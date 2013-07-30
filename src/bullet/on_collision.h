#pragma once

#include "engine/component.h"
#include "engine/system.h"
#include "scripting/luabind.h"

#include <memory>


namespace thrive {

/**
* @brief Notifies scripts of collision events
*/
class OnCollisionComponent : public Component {
    COMPONENT(OnCollision)

public:

    /**
    * @brief Called when a collision occured
    *
    * Arguments:
    * - thisEntity: The entity of this component
    * - otherEntity: The entity we collided with
    */
    luabind::object onCollisionCallback;

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - OnCollisionComponent::OnCollisionComponent()
    * - OnCollisionComponent::onCollisionCallback
    */
    static luabind::scope
    luaBindings();

};


/**
* @brief Calls the callbacks of OnCollisionComponents
*/
class OnCollisionSystem : public System {

public:

    /**
    * @brief Constructor
    */
    OnCollisionSystem();

    /**
    * @brief Destructor
    */
    ~OnCollisionSystem();

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
    * @brief Calls the components' callbacks for collision events
    */
    void update(int) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}


