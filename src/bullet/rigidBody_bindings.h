#pragma once

#include "engine/component.h"
#include "engine/shared_data.h"
#include "engine/system.h"

#include <memory>
#include <btBulletCollisionCommon.h>
#include <btBulletDynamicsCommon.h>
#include <BulletDynamics/Dynamics/btRigidBody.h>

#include <iostream>

namespace luabind {
class scope;
}

namespace thrive {

/**
* @brief A component for a light
*/
class RigidBodyComponent : public Component {
    COMPONENT(RigidBody)

public:

    /**
    * @brief Properties
    */
    struct Properties {

        /**
        * @brief The body's shape .
        */
        std::shared_ptr<btCollisionShape> shape = nullptr;

        /**
        * @brief The position
        */
        btVector3 position {0,0,0};

        /**
        * @brief The rotation.
        */
        btQuaternion rotation {0,0,0,1};

        /**
        * @brief The linear velocity
        *
        * Makes the rigid body move .
        */
        btVector3 linearVelocity {0,0,0};

        /**
        * @brief The angular velocity
        *
        * Makes the rigid body spin .
        */
        btVector3 angularVelocity {0,0,0};

        /**
        * @brief The restitution factor
        *
        * The spring or bounciness of a rigid body
        */
        btScalar restitution = 0.f;

        /**
        * @brief Locks linear movement to specific axis
        */
        btVector3 linearFactor {0,0,0};

        /**
        * @brief Locks angular movement to specific axis
        */
        btVector3 angularFactor {0,0,0};

        /**
        * @brief The mass of the rigid body
        */
        btScalar mass = 1.f;

        /**
        * @brief The offset of the mass
        */
        btTransform comOffset =btTransform::getIdentity();

        /**
        * @brief The inertia of the rigid body
        */
        btVector3 inertia {0,0,0};

        /**
        * @brief The friction of the object
        */
        btScalar friction = 0.f;

        /**
        * @brief The friction when rolling
        *
        * The rollingFriction prevents rounded shapes, such as spheres, cylinders and capsules from rolling forever
        */
        btScalar rollingFriction = 0.f;

    };

    /**
    * @brief Lua bindings
    *
    * Exposes the following \ref shared_data_lua shared properties:
    * - \c shape (btColisionShape): Properties::shape
    * - \c linearVelocity (btVector3): Properties::linearVelocity
    * - \c angularVelocity (btVector3): Properties::angularVelocity
    * - \c restitution (btScalar): Properties::restitution
    * - \c linearFactor (btVector3): Properties::linearFactor
    * - \c angularFactor (btVector3): Properties::angularFactor
    * - \c mass (btScalar): Properties::mass
    * - \c inertia (btVector3): Properties::inertia
    * - \c friction (btScalar): Properties::friction
    * - \c rollingFriction (btScalar): Properties::rollingFriction
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Internal object, dont use this directly
    */
    btRigidBody* m_body = nullptr;

    /**
    * @brief Shared properties
    */
    RigidBodyData<Properties>
    m_properties;

};


/**
* @brief Creates rigid bodies and updates its properties
*/
class RigidBodySystem : public System {

public:

    /**
    * @brief Constructor
    */
    RigidBodySystem();

    /**
    * @brief Destructor
    */
    ~RigidBodySystem();

    /**
    * @brief Initializes the system
    *
    * @param engine
    *   Must be an BulletEngine
    */
    void init(Engine* engine) override;

    /**
    * @brief Shuts the system down
    */
    void shutdown() override;

    /**
    * @brief Updates the sky components
    */
    void update(int) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
