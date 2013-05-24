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
class RigidBodyComponent : public Component, public btMotionState {
    COMPONENT(RigidBody)

public:

    /**
    * @brief Properties that can be set individually
    */
    struct StaticProperties {

        /**
        * @brief The body's shape .
        */
        //btCollisionShape* shape = new btSphereShape(1);
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
        * @brief The friction of the object
        */
        btScalar friction = 0.f;

        /**
        * @brief The velocity dampening
        */
        btScalar linearDamping = 0.0f;

        /**
        * @brief Rotation dampening
        */
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

    /**
    * @brief Properties that are simulated by the physics engine
    *
    * If you do want to set those (after initialization), be aware that it 
    * might introduce instabilities into the simulation.
    */
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
    * Exposes the following \ref shared_data_lua "shared properties":
    * - \c StaticProperties::shape
    * - \c StaticProperties::position
    * - \c StaticProperties::rotation
    * - \c StaticProperties::linearVelocity
    * - \c StaticProperties::angularVelocity
    * - \c StaticProperties::restitution
    * - \c StaticProperties::linearFactor
    * - \c StaticProperties::angularFactor
    * - \c StaticProperties::mass
    * - \c StaticProperties::friction
    * - \c StaticProperties::rollingFriction
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Reimplemented from btMotionState
    *
    * @param[out] transform
    *   The rigid body's position and orientation
    */
    void
    getWorldTransform(
        btTransform& transform
    ) const override;

    /**
    * @brief Reimplemented from btMotionState
    *
    * @param transform
    *   The rigid body's position and orientation
    */
    void
    setWorldTransform(
        const btTransform& transform
    ) override;

    /**
    * @brief Internal object, dont use this directly
    */
    btRigidBody* m_body = nullptr;

    /**
    * @brief Shared properties
    */
    PhysicsInputData<StaticProperties>
    m_staticProperties;

    /**
    * @brief Dynamic properties pushed to the physics engine
    *
    * Change these if you want to manually set position, orientation or
    * velocity of the rigid body. They are applied before the next physics
    * step.
    */
    PhysicsInputData<DynamicProperties>
    m_dynamicInputProperties;

    /**
    * @brief Dynamic properties read from the physics engine
    *
    * The values pulled from the physics engine after the last simulation 
    * step.
    */
    PhysicsOutputData<DynamicProperties>
    m_dynamicOutputProperties;

    /**
    * @brief Queue of impulses applied from external sources
    *
    * The first vector is the impulse itself, the second one is the attack 
    * point, relative to the rigid body's origin.
    */
    PhysicsInputQueue<
        std::pair<Ogre::Vector3, Ogre::Vector3>
    > m_impulseQueue;
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
    *   Must be a BulletEngine
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
* @brief Updates the RigidBodyComponent with new data from the simulation
*
* Copies the data from the simulation into 
* RigidBodyComponent::m_dynamicOutputProperties.
*
*/
class RigidBodyOutputSystem : public System {

public:

    /**
    * @brief Constructor
    */
    RigidBodyOutputSystem();

    /**
    * @brief Destructor
    */
    ~RigidBodyOutputSystem();

    /**
    * @brief Initializes the engine
    *
    * @param engine
    *   Must be a BulletEngine
    */
    void init(Engine* engine) override;

    /**
    * @brief Shuts the system down
    */
    void shutdown() override;

    /**
    * @brief Updates the system
    *
    * @param milliSeconds
    */
    void update(int milliSeconds) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
