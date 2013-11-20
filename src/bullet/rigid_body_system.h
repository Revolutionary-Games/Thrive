#pragma once

#include "bullet/collision_shape.h"
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
        CollisionShape::Ptr shape {new EmptyShape()};

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
        * @brief Whether this rigid body reacts to collisions
        */
        bool hasContactResponse = true;

        /**
        * @brief Whether this body is kinematic
        */
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
    *   - Properties::hasContactResponse
    *   - Properties::kinematic
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Constructor
    *
    * @param collisionFilterGroup
    *   The collision group this body belongs to
    * @param collisionFilterMask
    *   The groups this body can collide with
    */
    RigidBodyComponent(
        short int collisionFilterGroup = btBroadphaseProxy::DefaultFilter,
        short int collisionFilterMask = btBroadphaseProxy::AllFilter
    ) : m_collisionFilterGroup(collisionFilterGroup),
        m_collisionFilterMask(collisionFilterMask)
    {
    }

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
    * @brief Applies a torque to the body
    *
    * @param torque
    *   The torque to apply
    */
    void
    applyTorque(
        const Ogre::Vector3& torque
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
    * @brief Loads the component
    *
    * @param storage
    */
    void
    load(
        const StorageContainer& storage
    ) override;

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
    * @brief Serializes the component
    *
    * @return
    */
    StorageContainer
    storage() const override;

    /**
    * @brief Internal object, dont use this directly
    */
    btRigidBody* m_body = nullptr;

    /**
    * @brief The body's collision group
    */
    const short int m_collisionFilterGroup;

    /**
    * @brief The groups this body can collide with
    */
    const short int m_collisionFilterMask;

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
    * @brief The torque that has been applied in this timestep
    */
    Ogre::Vector3 m_torque = Ogre::Vector3::ZERO;

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
    * @brief Lua bindings
    *
    * Exposes:
    * - RigidBodyInputSystem()
    *
    * @return 
    */
    static luabind::scope
    luaBindings();

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
    */
    void init(GameState* gameState) override;

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
    * @brief Lua bindings
    *
    * Exposes:
    * - RigidBodyOutputSystem()
    *
    * @return 
    */
    static luabind::scope
    luaBindings();

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
    */
    void init(GameState* gameState) override;

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
