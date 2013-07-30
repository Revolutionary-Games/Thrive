#pragma once

#include "engine/component.h"
#include "engine/system.h"
#include "engine/touchable.h"

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
    * @brief Properties
    */
    struct Properties : public Touchable {

        /**
        * @brief The body's shape .
        */
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
        */
        Ogre::Vector3 forceApplied = Ogre::Vector3::ZERO;

        bool hasContactResponse = true;

        bool kinematic = false;

    };

    /**
    * @brief Dynamic properties (those the physics engine changes)
    */
    struct DynamicProperties : public Touchable {

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
    * Exposes:
    * - RigidBodyComponent()
    * - @link m_properties properties @endlink
    * - Properties
    *   - Properties::shape
    *   - Properties::position
    *   - Properties::rotation
    *   - Properties::linearVelocity
    *   - Properties::angularVelocity
    *   - Properties::restitution
    *   - Properties::linearFactor
    *   - Properties::angularFactor
    *   - Properties::mass
    *   - Properties::friction
    *   - Properties::rollingFriction
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Applies an impulse to the center of mass
    *
    * @param impulse
    *   The impulse
    */
    void
    applyCentralImpulse(
        const Ogre::Vector3& impulse
    );

    /**
    * @brief Applies an impulse
    *
    * @param impulse
    *   The impulse
    * @param relativePosition
    *   The attack point, relative to the center of mass 
    */
    void
    applyImpulse(
        const Ogre::Vector3& impulse,
        const Ogre::Vector3& relativePosition
    );

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
    * @brief Overrides the physics engine
    *
    * @warning
    *   May introduce instabilities. Use applyImpulse() if you want to move
    *   the body.
    *
    * @param position
    *   New position
    * @param orientation
    *   New orientation
    * @param linearVelocity
    *   New velocity
    * @param angularVelocity
    *   New rotation
    */
    void
    setDynamicProperties(
        const Ogre::Vector3& position,
        const Ogre::Quaternion& orientation,
        const Ogre::Vector3& linearVelocity,
        const Ogre::Vector3& angularVelocity
    );

    /**
    * @brief Internal object, dont use this directly
    */
    btRigidBody* m_body = nullptr;

    /**
    * @brief Dynamic properties
    */
    DynamicProperties
    m_dynamicProperties;

    /**
    * @brief Queue of impulses since the last frame
    */
    std::list<
        std::pair<Ogre::Vector3, Ogre::Vector3>
    > m_impulseQueue;

    /**
    * @brief Properties
    */
    Properties
    m_properties;
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
