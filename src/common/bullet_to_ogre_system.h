#pragma once

#include "engine/system.h"

namespace thrive {

/**
* @brief Updates OgreSceneNodeComponents with physics data
*
*/
class BulletToOgreSystem : public System {

public:

    /**
    * @brief Constructor
    */
    BulletToOgreSystem();

    /**
    * @brief Destructor
    */
    ~BulletToOgreSystem();

    /**
    * @brief Initializes the system
    *
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

