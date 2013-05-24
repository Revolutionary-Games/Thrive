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
