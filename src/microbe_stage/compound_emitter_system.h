#pragma once

#include <boost/range/adaptor/map.hpp>

#include "engine/component.h"
#include "engine/system.h"
#include "engine/touchable.h"
#include "scripting/luabind.h"
#include "engine/typedefs.h"
#include "microbe_stage/compound.h"

#include <luabind/object.hpp>
#include <memory>
#include <OgreCommon.h>
#include <OgreMath.h>
#include <OgreVector3.h>
#include <unordered_set>


namespace thrive {

using BoostCompoundMapIterator = boost::range_detail::select_second_mutable_range<std::unordered_map<std::string, CompoundId>>;
using BoostAbsorbedMapIterator = boost::range_detail::select_first_range<std::unordered_map<CompoundId, float>>;

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
        double amount,
        double angle,
        double radius
    );

    void
    load(
        const StorageContainer& storage
    ) override;

    StorageContainer
    storage() const override;

private:

    friend class CompoundEmitterSystem;
    std::vector<std::tuple<CompoundId, double, double, double>> m_compoundEmissions;

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
    CompoundId m_compoundId;// = NULL_COMPOUND;

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
    void update(int, int) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
