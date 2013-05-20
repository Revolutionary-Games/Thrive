#pragma once

#include "engine/component.h"
#include "engine/shared_data.h"
#include "engine/system.h"

#include <btBulletCollisionCommon.h>
#include <btBulletDynamicsCommon.h>
#include <memory>
#include <OgreQuaternion.h>
#include <OgreVector3.h>

#include <iostream>

namespace luabind {
class scope;
}

class btCollisionShape;

namespace thrive {

/**
* @brief A component for a rigid body
*/
class RigidBodyComponent : public Component {
    COMPONENT(RigidBody)

public:

    /**
    * @brief Properties that can be set individually
    */
    struct StaticProperties {

        /**
        * @brief The body's shape .
        */
        //btCollisionShape* s = new btSphereShape(1);
        std::shared_ptr<btCollisionShape> shape {new btSphereShape(1)};

        /**
        * @brief The restitution factor
        *
        * The spring or bounciness of a rigid body
        */
        btScalar restitution = 0.f;

        /**
        * @brief Locks linear movement to specific axis
        */
        Ogre::Vector3 linearFactor {1,1,1};

        /**
        * @brief Locks angular movement to specific axis
        */
        Ogre::Vector3 angularFactor {1,1,1};

        /**
        * @brief Inertia
        */
        btVector3 localInertia {0,0,0};


        /**
        * @brief The mass of the rigid body
        */
        btScalar mass = 1.f;

        /**
        * @brief The offset of the mass
        */
        btTransform comOffset = btTransform::getIdentity();

        /**
        * @brief The friction of the object
        */
        btScalar friction = 0.f;

        /**
        * @brief The friction of the object
        */
        btScalar linearDamping = 0.1f;
        btScalar angularDamping = 1.f;

        /**
        * @brief The friction when rolling
        *
        * The rollingFriction prevents rounded shapes, such as spheres, cylinders and capsules from rolling forever
        */
        btScalar rollingFriction = 0.f;

        /**
        *@brief The force currently applied to the body
        *
        *
        */
        Ogre::Vector3 forceApplied = Ogre::Vector3::ZERO;
    };

    struct DynamicProperties {
        /**
        * @brief The position
        */
        Ogre::Vector3 position {0,0,0};

        /**
        * @brief The rotation.
        */
        Ogre::Quaternion rotation = Ogre::Quaternion::IDENTITY;

        /**
        * @brief The linear velocity
        *
        * Makes the rigid body move .
        */
        Ogre::Vector3 linearVelocity {0,0,0};

        /**
        * @brief The angular velocity
        *
        * Makes the rigid body spin .
        */
        Ogre::Vector3 angularVelocity {0,0,0};
    };

    /**
    * @brief Lua bindings
    *
    * Exposes the following \ref shared_data_lua shared properties:
    * - \c shape (btColisionShape): Properties::shape
    * - \c position (btVector3): Properties::position
    * - \c rotation (btVector3): Properties::rotation
    * - \c linearVelocity (btVector3): Properties::linearVelocity
    * - \c angularVelocity (btVector3): Properties::angularVelocity
    * - \c restitution (btScalar): Properties::restitution
    * - \c linearFactor (btVector3): Properties::linearFactor
    * - \c angularFactor (btVector3): Properties::angularFactor
    * - \c mass (btScalar): Properties::mass
    * - \c comOffset (btTransform): Properties::comOffset
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

    std::unique_ptr<btMotionState>
    m_motionState;

    /**
    * @brief Shared properties
    */
    PhysicsInputData<StaticProperties>
    m_staticProperties;

    PhysicsInputData<DynamicProperties>
    m_dynamicProperties;
};


/**
* @brief Creates rigid bodies and updates its properties
*/
class RigidBodyInputSystem : public System {

public:

    /**
    * @brief Constructor
    */
    RigidBodyInputSystem();

    /**
    * @brief Destructor
    */
    ~RigidBodyInputSystem();

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

////////////////////////////////////////////////////////////////////////////////
// RigidBodyOutputSystem
////////////////////////////////////////////////////////////////////////////////


/**
* @brief Moves entities
*
* This system updates the PhysicsTransformComponent of all entities that also have a
* RigidBodyComponent.
*
*/
class RigidBodyOutputSystem : public System {

public:

    RigidBodyOutputSystem();

    ~RigidBodyOutputSystem();

    void init(Engine* engine) override;

    void shutdown() override;

    void update(int milliSeconds) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
