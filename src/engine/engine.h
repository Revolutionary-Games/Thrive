#pragma once

#include "engine/typedefs.h"

#include <memory>

namespace luabind {
class scope;
}

namespace thrive {

class EntityManager;
class System;

class Engine {

public:

    /**
    * @brief Constructor
    */
    Engine(
        EntityManager& entityManager
    );

    /**
    * @brief Non-copyable
    *
    */
    Engine(const Engine& other) = delete;

    virtual ~Engine() = 0;

    void
    addSystem(
        std::shared_ptr<System> system
    );

    EntityManager&
    entityManager();

    virtual void
    init();

    virtual void
    shutdown();

    virtual void 
    update(
        int milliseconds
    );

private:
    
    struct Implementation;
    std::unique_ptr<Implementation> m_impl;

};

}
