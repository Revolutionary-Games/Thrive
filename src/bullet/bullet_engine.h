#pragma once

#include "engine/engine.h"

#include <memory>

class btDiscreteDynamicsWorld;

namespace luabind {
class scope;
}

namespace thrive {

class BulletDebugSystem;

/**
* @brief Physics engine
*/
class BulletEngine : public Engine {

public:

    /**
    * @brief Lua bindings
    *
    * Exposes the following functions:
    *
    * - BulletEngine::setDebugMode
    *
    * @return 
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Constructor
    */
    BulletEngine();

    /**
    * @brief Destructor
    */
    ~BulletEngine();

    /**
    * @brief The debug system of the engine
    *
    * Use this in conjunction with BulletDebugScriptSystem and
    * BulletDebugRenderSystem to draw debug information.
    *
    * @return 
    */
    std::shared_ptr<BulletDebugSystem>
    debugSystem() const;

    /**
    * @brief Initializes the engine
    *
    * @param entityManager
    *   The entity manager to use
    */
    void init(
        EntityManager* entityManager
    ) override;

    /**
    * @brief Sets debug mode flags
    *
    * @param mode
    *   See btIDebugDraw::DebugDrawModes for available values
    */
    void
    setDebugMode(
        int mode
    );

    /**
    * @brief Shuts the engine down
    */
    void
    shutdown() override;

    /**
    * @brief Steps the simulation
    */
    void
    update() override;

    /**
    * @brief The physics world
    */
    btDiscreteDynamicsWorld*
    world() const;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
