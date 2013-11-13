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

using AgentId = uint16_t;

static const AgentId NULL_AGENT = 0;
static const Ogre::Vector3 PARTICLE_SCALE(0.3,0.3,0.3);

AgentId
generateAgentId();

/**
* @brief Component for entities that act as agent particles
*/
class AgentComponent : public Component {
    COMPONENT(Agent)

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - AgentComponent()
    * - AgentComponent::m_agentId
    * - AgentComponent::m_potency
    * - AgentComponent::m_timeToLive
    * - AgentComponent::m_velocity
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief The agent id
    */
    AgentId m_agentId = NULL_AGENT;

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
* @brief Emitter for agent particles
*/
class AgentEmitterComponent : public Component {
    COMPONENT(AgentEmitter)

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - AgentEmitterComponent()
    * - AgentEmitterComponent::m_emissionRadius
    * - AgentEmitterComponent::m_maxInitialSpeed
    * - AgentEmitterComponent::m_minInitialSpeed
    * - AgentEmitterComponent::m_minEmissionAngle
    * - AgentEmitterComponent::m_maxEmissionAngle
    * - AgentEmitterComponent::m_particleLifetime
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
    * @brief Emits an agent according to the set properties
    *
    * @param agentId
    *   The agent type to emit
    *
    * @param amount
    *   How much of the chosen agent to emit
    *
    * @param emitterPosition
    *   The position of the entity emitting the agent.
    *   Agent position is calculated from the relative emissionPosition and
    *   the emitterPosition.
    */
    void
    emitAgent(
        AgentId agentId,
        double amount
    );

    void
    load(
        const StorageContainer& storage
    ) override;

    StorageContainer
    storage() const override;

private:

    friend class AgentEmitterSystem;
    std::vector<std::pair<AgentId, int>> m_compoundEmissions;

};


/**
* @brief Component for automatic timed compound emissions.
*/
class TimedAgentEmitterComponent : public Component {
    COMPONENT(TimedAgentEmitterComponent)

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - AgentEmitterComponent()
    * - AgentEmitterComponent::m_agentId
    * - AgentEmitterComponent::m_particlesPerEmission
    * - AgentEmitterComponent::m_potencyPerParticle
    * - AgentEmitterComponent::m_emitInterval
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief The agent id to emit
    */
    AgentId m_agentId = NULL_AGENT;

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
    * @brief For use by TimedAgentEmitterSystem
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
* @brief Absorbs agent particles
*/
class AgentAbsorberComponent : public Component {
    COMPONENT(AgentAbsorber)

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - AgentAbsorberComponent::absorbedAgentAmount
    * - AgentAbsorberComponent::canAbsorbAgent
    * - AgentAbsorberComponent::setCanAbsorbAgent
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief The agents absorbed in the last time step
    */
    std::unordered_map<AgentId, float> m_absorbedAgents;

    /**
    * @brief Whether a particular agent id can be absorbed
    */
    std::unordered_set<AgentId> m_canAbsorbAgent;

    /**
    * @brief The absorbed amount in the last time step
    *
    * @param id
    *   The agent id to get the amount for
    *
    * @return
    */
    float
    absorbedAgentAmount(
        AgentId id
    ) const;

    /**
    * @brief Whether an agent can be absorbed
    *
    * @param id
    *   The agent id to check
    *
    * @return
    */
    bool
    canAbsorbAgent(
        AgentId id
    ) const;

    void
    load(
        const StorageContainer& storage
    ) override;

    /**
    * @brief Sets the amount of absorbed agents
    *
    * Use this for e.g. resetting the absorbed amount down
    * to zero.
    *
    * @param id
    *   The agent id to change the amount for
    * @param amount
    *   The new amount
    */
    void
    setAbsorbedAgentAmount(
        AgentId id,
        float amount
    );

    /**
    * @brief Sets whether an agent can be absorbed
    *
    * @param id
    *   The agent id to set the flag for
    * @param canAbsorb
    *   Whether to absorb the agent
    */
    void
    setCanAbsorbAgent(
        AgentId id,
        bool canAbsorb
    );

    StorageContainer
    storage() const override;

};


/**
* @brief Despawns agent particles after they've reached their lifetime
*/
class AgentLifetimeSystem : public System {

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - AgentLifetimeSystem()
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Constructor
    */
    AgentLifetimeSystem();

    /**
    * @brief Destructor
    */
    ~AgentLifetimeSystem();

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
* @brief Moves agent particles around
*/
class AgentMovementSystem : public System {

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - AgentMovementSystem()
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Constructor
    */
    AgentMovementSystem();

    /**
    * @brief Destructor
    */
    ~AgentMovementSystem();

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
* @brief Spawns agent particles for AgentEmitterComponent
*/
class AgentEmitterSystem : public System {

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - AgentEmitterSystem()
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Constructor
    */
    AgentEmitterSystem();

    /**
    * @brief Destructor
    */
    ~AgentEmitterSystem();

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
* @brief Despawns agents for AgentAbsorberComponent
*/
class AgentAbsorberSystem : public System {

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - AgentAbsorberSystem()
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Constructor
    */
    AgentAbsorberSystem();

    /**
    * @brief Destructor
    */
    ~AgentAbsorberSystem();

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
* @brief Static class keeping track of agents, their Id's, internal and displayed names
*/
class AgentRegistry final {

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - AgentRegistry::registerAgentType
    * - AgentRegistry::getAgentDisplayName
    * - AgentRegistry::getAgentInternalName
    * - AgentRegistry::getAgentId
    * - AgentRegistry::getAgentMeshName
    * @return
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Registers a new agent type
    *
    * @param internalName
    *   The name to be used internally for reference across game instances
    *
    * @param displayName
    *   Name to be displayed to users
    *
    * @param meshName
    *   Name of the model to use for this agent
    *
    * @return
    *   Id of new agent
    */
    static AgentId
    registerAgentType(
        const std::string& internalName,
        const std::string& displayName,
        const std::string& meshName
    );

    /**
    * @brief Obtains the display name of an agent
    *
    * @param id
    *   Id of the agent to obtain display name from
    *
    * @return
    *   Agent name to display to users
    */
    static std::string
    getAgentDisplayName(
        AgentId id
    );

    /**
    * @brief Obtains the internal name of an agent
    *
    * @param id
    *   Id of the agent to obtain internal name from
    *
    * @return
    *   Agent name for internal use
    */
    static std::string
    getAgentInternalName(
        AgentId id
    );

    /**
    * @brief Obtains the Id of an internal name corresponding to a registered agent
    *
    * @param internalName
    *   The internal name of the agent. Must not already exist in collection or invalid_argument is thrown.
    *
    * @return
    *   AgentId of the agent if it is registered. If agent is not registered an out_of_range exception is thrown.
    */
    static AgentId
    getAgentId(
        const std::string& internalName
    );

    /**
    * @brief Obtains the name of the corresponding mesh
    *
    * @param agentId
    *   The id of the agent to acquire the mesh name from
    *
    * @return
    *   A string containing the name of the agents mesh.
    *   If agent is not registered an out_of_range exception is thrown.
    */
    static std::string
    getAgentMeshName(
        AgentId agentId
    );

    AgentRegistry() = delete;

};

}

