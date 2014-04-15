#pragma once

#include <boost/range/adaptor/map.hpp>

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

static const CompoundId NULL_COMPOUND = 0;

CompoundId
generateCompoundId();

/**
* @brief Component for entities that act as compound particles
*/
class CompoundComponent : public Component {
    COMPONENT(Compound)

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - CompoundComponent()
    * - CompoundComponent::m_compoundId
    * - CompoundComponent::m_potency
    * - CompoundComponent::m_velocity
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief The compound id
    */
    CompoundId m_compoundId = NULL_COMPOUND;

    /**
    * @brief The potency of this particle
    */
    float m_potency = 0.0f;

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
    * - CompoundEmitterComponent::m_emissionRadius
    * - CompoundEmitterComponent::m_maxInitialSpeed
    * - CompoundEmitterComponent::m_minInitialSpeed
    * - CompoundEmitterComponent::m_minEmissionAngle
    * - CompoundEmitterComponent::m_maxEmissionAngle
    * - CompoundEmitterComponent::m_particleLifetime
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief How far away the particles are spawned
    */
    Ogre::Real m_emissionRadius = 0.0;

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
    Ogre::Degree m_maxEmissionAngle = Ogre::Degree(360);

    /**
    * @brief The minimum angle at which to emit particles
    *
    * Zero degrees is to the left, positive is counter-clockwise.
    */
    Ogre::Degree m_minEmissionAngle = Ogre::Degree(0);

    /**
    * @brief How long new particles will stay alive
    */
    Milliseconds m_particleLifetime = 1000;

    /**
    * @brief Emits an compound according to the set properties
    *
    * @param compoundId
    *   The compound type to emit
    *
    * @param amount
    *   How much of the chosen compound to emit
    *
    * @param emitterPosition
    *   The position of the entity emitting the compound.
    *   Compound position is calculated from the relative emissionPosition and
    *   the emitterPosition.
    */
    void
    emitCompound(
        CompoundId compoundId,
        double amount
    );

    void
    load(
        const StorageContainer& storage
    ) override;

    StorageContainer
    storage() const override;

private:

    friend class CompoundEmitterSystem;
    std::vector<std::pair<CompoundId, int>> m_compoundEmissions;

};


/**
* @brief Component for automatic timed compound emissions.
*/
class TimedCompoundEmitterComponent : public Component {
    COMPONENT(TimedCompoundEmitterComponent)

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - CompoundEmitterComponent()
    * - CompoundEmitterComponent::m_compoundId
    * - CompoundEmitterComponent::m_particlesPerEmission
    * - CompoundEmitterComponent::m_potencyPerParticle
    * - CompoundEmitterComponent::m_emitInterval
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief The compound id to emit
    */
    CompoundId m_compoundId = NULL_COMPOUND;

    /**
    * @brief The number of particles created per emission interval
    */
    uint16_t m_particlesPerEmission = 0;

    /**
    * @brief The potency new particles will receive
    */
    float m_potencyPerParticle = 1.0f;

    /**
    * @brief How often new particles are spawned
    */
    Milliseconds m_emitInterval = 1000;

    /**
    * @brief For use by TimedCompoundEmitterSystem
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
    * - CompoundAbsorberComponent::setAbsorbtionCapacity
    * - CompoundAbsorberComponent::enable
    * - CompoundAbsorberComponent::disable
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
    * @brief Whether anything can be absorbed
    */
    bool m_enabled = true;

    /**
    * @brief The amount of compound volume that can be absorbed
    */
    double m_absorbtionCapacity = 0;

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

    /**
    * @brief Sets the absorbtion capacity
    *
    * @param capacity
    *   The new capacity
    */
    void
    setAbsorbtionCapacity(
        double capacity
    );

    /**
    * Sets m_enabled to true
    */
    void
    enable();

    /**
    * Sets m_enabled to false
    */
    void
    disable();

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
    * - CompoundRegistry::getCompoundUnitVolume
    * - CompoundRegistry::getCompoundId
    * - CompoundRegistry::getCompoundList
    * - CompoundRegistry::getCompoundMeshName
    * - CompoundRegistry::getCompoundMeshScale
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
    * @param meshName
    *   Name of the model to use for this compound
    *
    * @param meshScale
    *   The relative size of the mesh
    *
	* @param unitVolume
    *   Size of the compound when stored and transported
	*
    * @return
    *   Id of new compound
    */
    static CompoundId
    registerCompoundType(
        const std::string& internalName,
        const std::string& displayName,
        const std::string& meshName,
        double meshScale,
		int unitVolume
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
    * @brief Obtains the size of a compound
    *
    * @param id
    *   Id of the compound to obtain size from
    *
    * @return
    *   Compound size for internal use
    */
    static int
    getCompoundUnitVolume(
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

    /**
    * @brief Obtains the IDs of all currently registered compounds
    *
    * @return
    *   Array of all registered compound IDs
    */
    static const boost::range_detail::select_second_mutable_range<std::unordered_map<std::string, CompoundId>>
    getCompoundList(
    );

	/**
    * @brief Obtains the name of the corresponding mesh
    *
    * @param compoundId
    *   The id of the compound to acquire the mesh name from
    *
    * @return
    *   A string containing the name of the compounds mesh.
    *   If compound is not registered an out_of_range exception is thrown.
    */
    static std::string
    getCompoundMeshName(
        CompoundId compoundId
    );

    /**
    * @brief Obtains the scale of the corresponding mesh
    *
    * @param agentId
    *   The id of the agent to acquire the mesh scale from
    *
    * @return
    *   A double equal to the scale of the model
    *   If agent is not registered an out_of_range exception is thrown.
    */
    static double
    getCompoundMeshScale(
        CompoundId compoundId
    );

    CompoundRegistry() = delete;

};

}

