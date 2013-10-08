#pragma once

#include "engine/system.h"

#include <list>

namespace luabind {
    class scope;
}

namespace thrive {

/**
* @brief System for updating script-defined systems
*/
class ScriptSystemUpdater : public System {
    
public:

    /**
    * @brief Constructor
    */
    ScriptSystemUpdater();

    /**
    * @brief Destructor
    */
    ~ScriptSystemUpdater();

    /**
    * @brief Adds a system to update
    *
    * @param system
    */
    void
    addSystem(
        std::shared_ptr<System> system
    );

    /**
    * @brief Initializes the script systems
    *
    * @param engine
    */
    void
    initSystems(
        Engine* engine
    );

    /**
    * @brief Shuts the script systems down
    */
    void
    shutdownSystems();

    /**
    * @brief Returns a list of registered systems
    *
    */
    const std::list<std::shared_ptr<System>>&
    systems();

    /**
    * @brief Updates the system
    */
    void update(int) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
