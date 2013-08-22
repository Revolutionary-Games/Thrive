#pragma once

#include "engine/component.h"
#include "engine/system.h"
#include "scripting/luabind.h"

#include <memory>


namespace thrive {

/**
* @brief Exposes a Lua callback that is called in every frame
*/
class OnUpdateComponent : public Component {
    COMPONENT(OnUpdate)

public:

    /**
    * @brief Lua bindings
    *
    * Exposes a single property:
    * - \c callback(entityId, milliSeconds): A function that takes 
    *   an entity id (number) and the time passed since the last frame
    *   (number).
    *
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief The Lua callback
    */
    luabind::object m_callback;

};


/**
* @brief Calls the callback of all OnUpdateComponents
*/
class OnUpdateSystem : public System {

public:

    /**
    * @brief Constructor
    */
    OnUpdateSystem();

    /**
    * @brief Destructor
    */
    ~OnUpdateSystem();

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
    * @brief Calls all OnUpdate callbacks
    *
    * @param milliseconds
    *   Time elapsed since last frame
    */
    void update(int milliseconds) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
