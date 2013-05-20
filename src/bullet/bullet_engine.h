#pragma once

#include "engine/engine.h"

#include <memory>

class btDiscreteDynamicsWorld;

namespace thrive {

/**
* @brief Physics engine
*/
class BulletEngine : public Engine {

public:

    /**
    * @brief Constructor
    */
    BulletEngine();

    /**
    * @brief Destructor
    */
    ~BulletEngine();

    /**
    * @brief Initializes the engine
    *
    * 1. Loads the resources
    * 2. Loads the configuration
    * 3. Creates a render window
    * 4. Sets up basic lighting
    * 5. Sets up user input
    * 6. Creates essential systems
    *
    * @param entityManager
    *   The entity manager to use
    */
    void init(
        EntityManager* entityManager
    ) override;

    /**
    * @brief Shuts the engine down
    */
    void
    shutdown() override;

    /**
    * @brief Renders a frame
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
