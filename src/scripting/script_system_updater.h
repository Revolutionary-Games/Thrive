#pragma once

#include "engine/system.h"

#include <list>

namespace luabind {
    class scope;
}

namespace thrive {

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

    void
    addSystem(
        std::shared_ptr<System>
    );

    void
    initSystems(
        Engine* engine
    );

    void
    shutdownSystems();

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
