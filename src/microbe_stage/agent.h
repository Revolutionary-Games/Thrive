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

using AgentId = unsigned int;

static const AgentId NULL_AGENT = 0;

AgentId
generateAgentId();

class AgentComponent : public Component {
    COMPONENT(Agent)

public:

    AgentId m_agentId = NULL_AGENT;

    float m_potency = 0.0f;

    Milliseconds m_timeToLive = 0;

    Ogre::Vector3 m_velocity = Ogre::Vector3::ZERO;

};


class AgentEmitterComponent : public Component {
    COMPONENT(AgentEmitter)

public:

    static luabind::scope
    luaBindings();

    AgentId m_agentId = NULL_AGENT;

    Ogre::Real m_emissionRadius = 0.0;

    Milliseconds m_emitInterval;

    Ogre::Real m_maxInitialSpeed = 0.0;

    Ogre::Real m_minInitialSpeed = 0.0;

    Ogre::Degree m_maxEmissionAngle;

    Ogre::Degree m_minEmissionAngle;

    Ogre::String m_meshName;

    unsigned int m_particlesPerEmission;

    Milliseconds m_particleLifeTime;

    Ogre::Vector3 m_particleScale = Ogre::Vector3(1, 1, 1);

    float m_potencyPerParticle = 1.0f;

    // For use by system

    Milliseconds m_timeSinceLastEmission = 0;

};


class AgentAbsorberComponent : public Component {
    COMPONENT(AgentAbsorber)

public:

    static luabind::scope
    luaBindings();

    std::unordered_map<AgentId, float> m_absorbedAgents;

    std::unordered_set<AgentId> m_canAbsorbAgent;

    float
    absorbedAgentAmount(
        AgentId id
    ) const;

    bool
    canAbsorbAgent(
        AgentId id
    ) const;

    void
    setAbsorbedAgentAmount(
        AgentId id,
        float amount
    );

    void
    setCanAbsorbAgent(
        AgentId id,
        bool canAbsorb
    );

};


class AgentLifetimeSystem : public System {
    
public:

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
    * @param engine
    */
    void init(Engine* engine) override;

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


class AgentMovementSystem : public System {
    
public:

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
    * @param engine
    */
    void init(Engine* engine) override;

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


class AgentEmitterSystem : public System {
    
public:

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
    * @param engine
    */
    void init(Engine* engine) override;

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


class AgentAbsorberSystem : public System {
    
public:

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
    * @param engine
    */
    void init(Engine* engine) override;

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
}

