#pragma once

#include "engine/system.h"

namespace thrive {

/**
* @brief Steps the physics simulation
*
* Requires a BulletEngine
*/
class UpdatePhysicsSystem : public System {

public:

    /**
    * @brief Constructor
    */
    UpdatePhysicsSystem();

    /**
    * @brief Destructor
    */
    ~UpdatePhysicsSystem();

    /**
    * @brief Initializes the system
    *
    * @param engine
    *   Must be a BulletEngine
    */
    void
    init(
        Engine* engine
    ) override;

    /**
    * @brief Shuts down the system
    */
    void
    shutdown() override;

    /**
    * @brief Updates the system
    *
    * @param milliSeconds
    */
    void
    update(
        int milliSeconds
    ) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
