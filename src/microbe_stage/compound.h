#pragma once

#include "engine/component.h"
#include "engine/system.h"
#include "engine/touchable.h"
#include "scripting/luabind.h"

#include <memory>
#include <OgreCommon.h>
#include <OgreMath.h>
#include <OgreVector3.h>
#include <unordered_set>

namespace luabind {
class scope;
}


namespace thrive {

using CompoundId = uint16_t;

static const CompoundId NULL_AGENT = 0;

CompoundId
generateCompoundId();

/**
* @brief Component for entities that act as compound particles
*/
class CompoundComponent : public Component {
    COMPONENT(Compound)

public:

    /**
    * @brief The compound id
    */
    CompoundId m_compoundId = NULL_AGENT;

    /**
    * @brief The potency of this particle
    */
    float m_potency = 0.0f;

    /**
    * @brief The time until this particle despawns
    */
    Milliseconds m_timeToLive = 0;

    /**
    * @brief The current velocity of the particle
    */
    Ogre::Vector3 m_velocity = Ogre::Vector3::ZERO;

    void
    load(
        const StorageContainer& storage
    ) override;

    StorageContainer
    storage() const override;

};


/**
* @brief Emitter for compound particles
*/
class CompoundEmitterComponent : public Component {
    COMPONENT(CompoundEmitter)

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - CompoundEmitterComponent()
    * - CompoundEmitterComponent::m_compoundId
    * - CompoundEmitterComponent::m_emissionRadius
    * - CompoundEmitterComponent::m_emitInterval
    * - CompoundEmitterComponent::m_maxInitialSpeed
    * - CompoundEmitterComponent::m_minInitialSpeed
    * - CompoundEmitterComponent::m_minEmissionAngle
    * - CompoundEmitterComponent::m_maxEmissionAngle
    * - CompoundEmitterComponent::m_meshName
    * - CompoundEmitterComponent::m_particlesPerEmission
    * - CompoundEmitterComponent::m_particleLifetime
    * - CompoundEmitterComponent::m_particleScale
    * - CompoundEmitterComponent::m_potencyPerParticle
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief The compound id to emit
    */
    CompoundId m_compoundId = NULL_AGENT;

    /**
    * @brief How far away the particles are spawned
    */
    Ogre::Real m_emissionRadius = 0.0;

    /**
    * @brief How often new particles are spawned
    */
    Milliseconds m_emitInterval = 1000;

    /**
    * @brief The maximum initial speed of new particles
    */
    Ogre::Real m_maxInitialSpeed = 0.0;

    /**
    * @brief The minimum initial speed of new particles
    */
    Ogre::Real m_minInitialSpeed = 0.0;

    /**
    * @brief The maximum angle at which to emit particles
    *
    * Zero degrees is to the left, positive is counter-clockwise.
    */
    Ogre::Degree m_maxEmissionAngle;

    /**
    * @brief The minimum angle at which to emit particles
    *
    * Zero degrees is to the left, positive is counter-clockwise.
    */
    Ogre::Degree m_minEmissionAngle;

    /**
    * @brief The mesh that new particles are created with
    */
    Ogre::String m_meshName;

    /**
    * @brief The number of particles created per emission interval
    */
    uint16_t m_particlesPerEmission = 0;

    /**
    * @brief How long new particles will stay alive
    */
    Milliseconds m_particleLifetime = 1000;

    /**
    * @brief The scale of new particles
    */
    Ogre::Vector3 m_particleScale = Ogre::Vector3(1, 1, 1);

    /**
    * @brief The potency new particles will receive
    */
    float m_potencyPerParticle = 1.0f;

    /**
    * @brief For use by CompoundEmitterSystem
    */
    Milliseconds m_timeSinceLastEmission = 0;

    void
    load(
        const StorageContainer& storage
    ) override;

    StorageContainer
    storage() const override;

};


/**
* @brief Absorbs compound particles
*/
class CompoundAbsorberComponent : public Component {
    COMPONENT(CompoundAbsorber)

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - CompoundAbsorberComponent::absorbedCompoundAmount
    * - CompoundAbsorberComponent::canAbsorbCompound
    * - CompoundAbsorberComponent::setCanAbsorbCompound
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief The compounds absorbed in the last time step
    */
    std::unordered_map<CompoundId, float> m_absorbedCompounds;

    /**
    * @brief Whether a particular compound id can be absorbed
    */
    std::unordered_set<CompoundId> m_canAbsorbCompound;

    /**
    * @brief The absorbed amount in the last time step
    *
    * @param id
    *   The compound id to get the amount for
    *
    * @return
    */
    float
    absorbedCompoundAmount(
        CompoundId id
    ) const;

    /**
    * @brief Whether an compound can be absorbed
    *
    * @param id
    *   The compound id to check
    *
    * @return
    */
    bool
    canAbsorbCompound(
        CompoundId id
    ) const;

    void
    load(
        const StorageContainer& storage
    ) override;

    /**
    * @brief Sets the amount of absorbed compounds
    *
    * Use this for e.g. resetting the absorbed amount down
    * to zero.
    *
    * @param id
    *   The compound id to change the amount for
    * @param amount
    *   The new amount
    */
    void
    setAbsorbedCompoundAmount(
        CompoundId id,
        float amount
    );

    /**
    * @brief Sets whether an compound can be absorbed
    *
    * @param id
    *   The compound id to set the flag for
    * @param canAbsorb
    *   Whether to absorb the compound
    */
    void
    setCanAbsorbCompound(
        CompoundId id,
        bool canAbsorb
    );

    StorageContainer
    storage() const override;

};


/**
* @brief Despawns compound particles after they've reached their lifetime
*/
class CompoundLifetimeSystem : public System {

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - CompoundLifetimeSystem()
    *
    * @return 
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Constructor
    */
    CompoundLifetimeSystem();

    /**
    * @brief Destructor
    */
    ~CompoundLifetimeSystem();

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
    * @brief Updates the system
    */
    void update(int) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};


/**
* @brief Moves compound particles around
*/
class CompoundMovementSystem : public System {

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - CompoundMovementSystem()
    *
    * @return 
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Constructor
    */
    CompoundMovementSystem();

    /**
    * @brief Destructor
    */
    ~CompoundMovementSystem();

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
    * @brief Updates the system
    */
    void update(int) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};


/**
* @brief Spawns compound particles for CompoundEmitterComponent
*/
class CompoundEmitterSystem : public System {

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - CompoundEmitterSystem()
    *
    * @return 
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Constructor
    */
    CompoundEmitterSystem();

    /**
    * @brief Destructor
    */
    ~CompoundEmitterSystem();

    /**
    * @brief Initializes the system
    *
    * @param gameState
    */
    void init(GameState* gameState) override;

    /**
    * @brief Shuts the system down
    */
    void shutdown() override;

    /**
    * @brief Updates the system
    */
    void update(int) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};


/**
* @brief Despawns compounds for CompoundAbsorberComponent
*/
class CompoundAbsorberSystem : public System {

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - CompoundAbsorberSystem()
    *
    * @return 
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Constructor
    */
    CompoundAbsorberSystem();

    /**
    * @brief Destructor
    */
    ~CompoundAbsorberSystem();

    /**
    * @brief Initializes the system
    *
    * @param gameState
    */
    void init(GameState* gameState) override;

    /**
    * @brief Shuts the system down
    */
    void shutdown() override;

    /**
    * @brief Updates the system
    */
    void update(int) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};


/**
* @brief Static class keeping track of compounds, their Id's, internal and displayed names
*/
class CompoundRegistry final {

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - CompoundRegistry::registerCompoundType
    * - CompoundRegistry::getCompoundDisplayName
    * - CompoundRegistry::getCompoundInternalName
    * - CompoundRegistry::getCompoundId
    * @return
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Registers a new compound type
    *
    * @param internalName
    *   The name to be used internally for reference across game instances
    *
    * @param displayName
    *   Name to be displayed to users
    *
    * @return
    *   Id of new compound
    */
    static CompoundId
    registerCompoundType(
        const std::string& internalName,
        const std::string& displayName
    );

    /**
    * @brief Obtains the display name of an compound
    *
    * @param id
    *   Id of the compound to obtain display name from
    *
    * @return
    *   Compound name to display to users
    */
    static std::string
    getCompoundDisplayName(
        CompoundId id
    );

    /**
    * @brief Obtains the internal name of an compound
    *
    * @param id
    *   Id of the compound to obtain internal name from
    *
    * @return
    *   Compound name for internal use
    */
    static std::string
    getCompoundInternalName(
        CompoundId id
    );

    /**
    * @brief Obtains the Id of an internal name corresponding to a registered compound
    *
    * @param internalName
    *   The internal name of the compound. Must not already exist in collection or invalid_argument is thrown.
    *
    * @return
    *   CompoundId of the compound if it is registered. If compound is not registered an out_of_range exception is thrown.
    */
    static CompoundId
    getCompoundId(
        const std::string& internalName
    );

    CompoundRegistry() = delete;

};

}

