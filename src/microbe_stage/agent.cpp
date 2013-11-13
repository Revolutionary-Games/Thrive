#include "microbe_stage/agent.h"

#include "bullet/collision_filter.h"
#include "bullet/collision_system.h"
#include "bullet/rigid_body_system.h"
#include "engine/component_factory.h"
#include "engine/engine.h"
#include "engine/entity_filter.h"
#include "engine/game_state.h"
#include "engine/serialization.h"
#include "engine/rng.h"
#include "game.h"
#include "ogre/scene_node_system.h"
#include "scripting/luabind.h"
#include <OgreEntity.h>
#include <OgreSceneManager.h>
#include "util/make_unique.h"

using namespace thrive;

REGISTER_COMPONENT(AgentComponent)


luabind::scope
AgentComponent::luaBindings() {
    using namespace luabind;
    return class_<AgentComponent, Component>("AgentComponent")
        .enum_("ID") [
            value("TYPE_ID", AgentComponent::TYPE_ID)
        ]
        .scope [
            def("TYPE_NAME", &AgentComponent::TYPE_NAME)
        ]
        .def(constructor<>())
        .def_readwrite("agentId", &AgentComponent::m_agentId)
        .def_readwrite("potency", &AgentComponent::m_potency)
        .def_readwrite("timeToLive", &AgentComponent::m_timeToLive)
        .def_readwrite("velocity", &AgentComponent::m_velocity)
    ;
}


void
AgentComponent::load(
    const StorageContainer& storage
) {
    Component::load(storage);
    m_agentId = storage.get<AgentId>("agentId", NULL_AGENT);
    m_potency = storage.get<float>("potency");
    m_timeToLive = storage.get<Milliseconds>("timeToLive");
    m_velocity = storage.get<Ogre::Vector3>("velocity");
}


StorageContainer
AgentComponent::storage() const {
    StorageContainer storage = Component::storage();
    storage.set<AgentId>("agentId", m_agentId);
    storage.set<float>("potency", m_potency);
    storage.set<Milliseconds>("timeToLive", m_timeToLive);
    storage.set<Ogre::Vector3>("velocity", m_velocity);
    return storage;
}

////////////////////////////////////////////////////////////////////////////////
// AgentEmitterComponent
////////////////////////////////////////////////////////////////////////////////

luabind::scope
AgentEmitterComponent::luaBindings() {
    using namespace luabind;
    return class_<AgentEmitterComponent, Component>("AgentEmitterComponent")
        .enum_("ID") [
            value("TYPE_ID", AgentEmitterComponent::TYPE_ID)
        ]
        .scope [
            def("TYPE_NAME", &AgentEmitterComponent::TYPE_NAME)
        ]
        .def(constructor<>())
        .def("emitAgent", &AgentEmitterComponent::emitAgent)
        .def_readwrite("emissionRadius", &AgentEmitterComponent::m_emissionRadius)
        .def_readwrite("maxInitialSpeed", &AgentEmitterComponent::m_maxInitialSpeed)
        .def_readwrite("minInitialSpeed", &AgentEmitterComponent::m_minInitialSpeed)
        .def_readwrite("minEmissionAngle", &AgentEmitterComponent::m_minEmissionAngle)
        .def_readwrite("maxEmissionAngle", &AgentEmitterComponent::m_maxEmissionAngle)
        .def_readwrite("particleLifetime", &AgentEmitterComponent::m_particleLifetime)
    ;
}


void
AgentEmitterComponent::emitAgent(
    AgentId agentId,
    double amount
) {
    m_compoundEmissions.push_back(std::pair<AgentId, int>(agentId, amount));
}



void
AgentEmitterComponent::load(
    const StorageContainer& storage
) {
    Component::load(storage);
    m_emissionRadius = storage.get<Ogre::Real>("emissionRadius", 0.0);
    m_maxInitialSpeed = storage.get<Ogre::Real>("maxInitialSpeed", 0.0);
    m_minInitialSpeed = storage.get<Ogre::Real>("minInitialSpeed", 0.0);
    m_maxEmissionAngle = storage.get<Ogre::Degree>("maxEmissionAngle");
    m_minEmissionAngle = storage.get<Ogre::Degree>("minEmissionAngle");
    m_particleLifetime = storage.get<Milliseconds>("particleLifetime");
}

StorageContainer
AgentEmitterComponent::storage() const {
    StorageContainer storage = Component::storage();
    storage.set<Ogre::Real>("emissionRadius", m_emissionRadius);
    storage.set<Ogre::Real>("maxInitialSpeed", m_maxInitialSpeed);
    storage.set<Ogre::Real>("minInitialSpeed", m_minInitialSpeed);
    storage.set<Ogre::Degree>("maxEmissionAngle", m_maxEmissionAngle);
    storage.set<Ogre::Degree>("minEmissionAngle", m_minEmissionAngle);
    storage.set<Milliseconds>("particleLifetime", m_particleLifetime);
    return storage;
}

REGISTER_COMPONENT(AgentEmitterComponent)


////////////////////////////////////////////////////////////////////////////////
// TimedAgentEmitterComponent
////////////////////////////////////////////////////////////////////////////////

luabind::scope
TimedAgentEmitterComponent::luaBindings() {
    using namespace luabind;
    return class_<TimedAgentEmitterComponent, Component>("TimedAgentEmitterComponent")
        .enum_("ID") [
            value("TYPE_ID", TimedAgentEmitterComponent::TYPE_ID)
        ]
        .scope [
            def("TYPE_NAME", &TimedAgentEmitterComponent::TYPE_NAME)
        ]
        .def(constructor<>())
        .def_readwrite("emitInterval", &TimedAgentEmitterComponent::m_emitInterval)
        .def_readwrite("agentId", &TimedAgentEmitterComponent::m_agentId)
        .def_readwrite("particlesPerEmission", &TimedAgentEmitterComponent::m_particlesPerEmission)
        .def_readwrite("potencyPerParticle", &TimedAgentEmitterComponent::m_potencyPerParticle)
    ;
}


void
TimedAgentEmitterComponent::load(
    const StorageContainer& storage
) {
    Component::load(storage);
    m_agentId = storage.get<AgentId>("agentId", NULL_AGENT);
    m_particlesPerEmission = storage.get<uint16_t>("particlesPerEmission");
    m_potencyPerParticle = storage.get<float>("potencyPerParticle");
    m_emitInterval = storage.get<Milliseconds>("emitInterval", 1000);
    m_timeSinceLastEmission = storage.get<Milliseconds>("timeSinceLastEmission");
}


StorageContainer
TimedAgentEmitterComponent::storage() const {
    StorageContainer storage = Component::storage();
    storage.set<AgentId>("agentId", m_agentId);
    storage.set<uint16_t>("particlesPerEmission", m_particlesPerEmission);
    storage.set<float>("potencyPerParticle", m_potencyPerParticle);
    storage.set<Milliseconds>("emitInterval", m_emitInterval);
    storage.set<Milliseconds>("timeSinceLastEmission", m_timeSinceLastEmission);
    return storage;
}

REGISTER_COMPONENT(TimedAgentEmitterComponent)

////////////////////////////////////////////////////////////////////////////////
// AgentAbsorberComponent
////////////////////////////////////////////////////////////////////////////////

luabind::scope
AgentAbsorberComponent::luaBindings() {
    using namespace luabind;
    return class_<AgentAbsorberComponent, Component>("AgentAbsorberComponent")
        .enum_("ID") [
            value("TYPE_ID", AgentAbsorberComponent::TYPE_ID)
        ]
        .scope [
            def("TYPE_NAME", &AgentAbsorberComponent::TYPE_NAME)
        ]
        .def(constructor<>())
        .def("absorbedAgentAmount", &AgentAbsorberComponent::absorbedAgentAmount)
        .def("setAbsorbedAgentAmount", &AgentAbsorberComponent::setAbsorbedAgentAmount)
        .def("setCanAbsorbAgent", &AgentAbsorberComponent::setCanAbsorbAgent)
    ;
}


float
AgentAbsorberComponent::absorbedAgentAmount(
    AgentId id
) const {
    const auto& iter = m_absorbedAgents.find(id);
    if (iter != m_absorbedAgents.cend()) {
        return iter->second;
    }
    else {
        return 0.0f;
    }
}


bool
AgentAbsorberComponent::canAbsorbAgent(
    AgentId id
) const {
    return m_canAbsorbAgent.find(id) != m_canAbsorbAgent.end();
}


void
AgentAbsorberComponent::load(
    const StorageContainer& storage
) {
    Component::load(storage);
    StorageList agents = storage.get<StorageList>("agents");
    for (const StorageContainer& container : agents) {
        AgentId agentId = container.get<AgentId>("agentId");
        float amount = container.get<float>("amount");
        m_absorbedAgents[agentId] = amount;
        m_canAbsorbAgent.insert(agentId);
    }
}


void
AgentAbsorberComponent::setAbsorbedAgentAmount(
    AgentId id,
    float amount
) {
    m_absorbedAgents[id] = amount;
}


void
AgentAbsorberComponent::setCanAbsorbAgent(
    AgentId id,
    bool canAbsorb
) {
    if (canAbsorb) {
        m_canAbsorbAgent.insert(id);
    }
    else {
        m_canAbsorbAgent.erase(id);
    }
}


StorageContainer
AgentAbsorberComponent::storage() const {
    StorageContainer storage = Component::storage();
    StorageList agents;
    agents.reserve(m_canAbsorbAgent.size());
    for (AgentId agentId : m_canAbsorbAgent) {
        StorageContainer container;
        container.set<AgentId>("agentId", agentId);
        container.set<float>("amount", this->absorbedAgentAmount(agentId));
        agents.append(container);
    }
    storage.set<StorageList>("agents", agents);
    return storage;
}

REGISTER_COMPONENT(AgentAbsorberComponent)

////////////////////////////////////////////////////////////////////////////////
// AgentLifetimeSystem
////////////////////////////////////////////////////////////////////////////////

luabind::scope
AgentLifetimeSystem::luaBindings() {
    using namespace luabind;
    return class_<AgentLifetimeSystem, System>("AgentLifetimeSystem")
        .def(constructor<>())
    ;
}


struct AgentLifetimeSystem::Implementation {

    EntityFilter<
        AgentComponent
    > m_entities;
};


AgentLifetimeSystem::AgentLifetimeSystem()
  : m_impl(new Implementation())
{
}


AgentLifetimeSystem::~AgentLifetimeSystem() {}


void
AgentLifetimeSystem::init(
    GameState* gameState
) {
    System::init(gameState);
    m_impl->m_entities.setEntityManager(&gameState->entityManager());
}


void
AgentLifetimeSystem::shutdown() {
    m_impl->m_entities.setEntityManager(nullptr);
    System::shutdown();
}


void
AgentLifetimeSystem::update(int milliseconds) {
    for (auto& value : m_impl->m_entities) {
        AgentComponent* agentComponent = std::get<0>(value.second);
        agentComponent->m_timeToLive -= milliseconds;
        if (agentComponent->m_timeToLive <= 0) {
            this->entityManager()->removeEntity(value.first);
        }
    }
}


////////////////////////////////////////////////////////////////////////////////
// AgentMovementSystem
////////////////////////////////////////////////////////////////////////////////

luabind::scope
AgentMovementSystem::luaBindings() {
    using namespace luabind;
    return class_<AgentMovementSystem, System>("AgentMovementSystem")
        .def(constructor<>())
    ;
}


struct AgentMovementSystem::Implementation {

    EntityFilter<
        AgentComponent,
        RigidBodyComponent
    > m_entities;
};


AgentMovementSystem::AgentMovementSystem()
  : m_impl(new Implementation())
{
}


AgentMovementSystem::~AgentMovementSystem() {}


void
AgentMovementSystem::init(
    GameState* gameState
) {
    System::init(gameState);
    m_impl->m_entities.setEntityManager(&gameState->entityManager());
}


void
AgentMovementSystem::shutdown() {
    m_impl->m_entities.setEntityManager(nullptr);
    System::shutdown();
}


void
AgentMovementSystem::update(int milliseconds) {
    for (auto& value : m_impl->m_entities) {
        AgentComponent* agentComponent = std::get<0>(value.second);
        RigidBodyComponent* rigidBodyComponent = std::get<1>(value.second);
        Ogre::Vector3 delta = agentComponent->m_velocity * float(milliseconds) / 1000.0f;
        rigidBodyComponent->m_dynamicProperties.position += delta;
    }
}


////////////////////////////////////////////////////////////////////////////////
// AgentEmitterSystem
////////////////////////////////////////////////////////////////////////////////

luabind::scope
AgentEmitterSystem::luaBindings() {
    using namespace luabind;
    return class_<AgentEmitterSystem, System>("AgentEmitterSystem")
        .def(constructor<>())
    ;
}


struct AgentEmitterSystem::Implementation {

    EntityFilter<
        AgentEmitterComponent,
        OgreSceneNodeComponent,
        Optional<TimedAgentEmitterComponent>
    > m_entities;

    Ogre::SceneManager* m_sceneManager = nullptr;
};


AgentEmitterSystem::AgentEmitterSystem()
  : m_impl(new Implementation())
{
}


AgentEmitterSystem::~AgentEmitterSystem() {}


void
AgentEmitterSystem::init(
    GameState* gameState
) {
    System::init(gameState);
    m_impl->m_entities.setEntityManager(&gameState->entityManager());
    m_impl->m_sceneManager = gameState->sceneManager();
}


void
AgentEmitterSystem::shutdown() {
    m_impl->m_entities.setEntityManager(nullptr);
    m_impl->m_sceneManager = nullptr;
    System::shutdown();
}

// Helper function for AgentEmitterSystem to emit agents
static void
emitAgent(
    AgentId agentId,
    double amount,
    Ogre::Vector3 emittorPosition,
    AgentEmitterComponent* emitterComponent
) {

    Ogre::Vector3 emissionOffset(0,0,0);

    Ogre::Degree emissionAngle{static_cast<Ogre::Real>(Game::instance().engine().rng().getDouble(
        emitterComponent->m_minEmissionAngle.valueDegrees(),
        emitterComponent->m_maxEmissionAngle.valueDegrees()
    ))};
    Ogre::Real emissionSpeed = Game::instance().engine().rng().getDouble(
        emitterComponent->m_minInitialSpeed,
        emitterComponent->m_maxInitialSpeed
    );
    Ogre::Vector3 emissionVelocity(
        emissionSpeed * Ogre::Math::Sin(emissionAngle),
        emissionSpeed * Ogre::Math::Cos(emissionAngle),
        0.0
    );
    emissionOffset = Ogre::Vector3(
        emitterComponent->m_emissionRadius * Ogre::Math::Sin(emissionAngle),
        emitterComponent->m_emissionRadius * Ogre::Math::Cos(emissionAngle),
        0.0
    );
    EntityId agentEntityId = Game::instance().engine().currentGameState()->entityManager().generateNewId();
    // Scene Node
    auto agentSceneNodeComponent = make_unique<OgreSceneNodeComponent>();
    agentSceneNodeComponent->m_transform.scale = PARTICLE_SCALE;
    agentSceneNodeComponent->m_meshName = AgentRegistry::getAgentMeshName(agentId);
    // Collision Hull
    auto agentRigidBodyComponent = make_unique<RigidBodyComponent>(
        btBroadphaseProxy::SensorTrigger,
        btBroadphaseProxy::AllFilter & (~ btBroadphaseProxy::SensorTrigger)
    );
    agentRigidBodyComponent->m_properties.shape = std::make_shared<SphereShape>(0.01);
    agentRigidBodyComponent->m_properties.hasContactResponse = false;
    agentRigidBodyComponent->m_properties.kinematic = true;
    agentRigidBodyComponent->m_dynamicProperties.position = emittorPosition + emissionOffset;
    // Agent Component
    auto agentComponent = make_unique<AgentComponent>();
    agentComponent->m_timeToLive = emitterComponent->m_particleLifetime;
    agentComponent->m_velocity = emissionVelocity;
    agentComponent->m_agentId = agentId;
    agentComponent->m_potency = amount;
    auto collisionHandler = make_unique<CollisionComponent>();
    collisionHandler->addCollisionGroup("agent");
    // Build component list
    std::list<std::unique_ptr<Component>> components;
    components.emplace_back(std::move(agentSceneNodeComponent));
    components.emplace_back(std::move(agentComponent));
    components.emplace_back(std::move(agentRigidBodyComponent));
    components.emplace_back(std::move(collisionHandler));
    for (auto& component : components) {
        Game::instance().engine().currentGameState()->entityManager().addComponent(
            agentEntityId,
            std::move(component)
        );
    }
}



void
AgentEmitterSystem::update(int milliseconds) {
    for (auto& value : m_impl->m_entities) {
        AgentEmitterComponent* emitterComponent = std::get<0>(value.second);
        OgreSceneNodeComponent* sceneNodeComponent = std::get<1>(value.second);
        TimedAgentEmitterComponent* timedEmitterComponent = std::get<2>(value.second);

        for (auto emission : emitterComponent->m_compoundEmissions)
        {
            emitAgent(std::get<0>(emission), std::get<1>(emission), sceneNodeComponent->m_transform.position, emitterComponent);
        }
        emitterComponent->m_compoundEmissions.clear();
        if (timedEmitterComponent)
        {
            timedEmitterComponent->m_timeSinceLastEmission += milliseconds;
            while (
                timedEmitterComponent->m_emitInterval > 0 and
                timedEmitterComponent->m_timeSinceLastEmission >= timedEmitterComponent->m_emitInterval
            ) {
                timedEmitterComponent->m_timeSinceLastEmission -= timedEmitterComponent->m_emitInterval;
                for (unsigned int i = 0; i < timedEmitterComponent->m_particlesPerEmission; ++i) {
                     emitAgent(timedEmitterComponent->m_agentId, timedEmitterComponent->m_potencyPerParticle, sceneNodeComponent->m_transform.position, emitterComponent);
                }
            }
        }
    }
}


////////////////////////////////////////////////////////////////////////////////
// AgentAbsorberSystem
////////////////////////////////////////////////////////////////////////////////

luabind::scope
AgentAbsorberSystem::luaBindings() {
    using namespace luabind;
    return class_<AgentAbsorberSystem, System>("AgentAbsorberSystem")
        .def(constructor<>())
    ;
}

struct AgentAbsorberSystem::Implementation {

    Implementation()
      : m_agentCollisions("microbe", "agent")
    {
    }

    EntityFilter<
        AgentAbsorberComponent
    > m_absorbers;

    EntityFilter<
        AgentComponent
    > m_agents;

    btDiscreteDynamicsWorld* m_world = nullptr;

    CollisionFilter m_agentCollisions;

};


AgentAbsorberSystem::AgentAbsorberSystem()
  : m_impl(new Implementation())
{
}


AgentAbsorberSystem::~AgentAbsorberSystem() {}


void
AgentAbsorberSystem::init(
    GameState* gameState
) {
    System::init(gameState);
    m_impl->m_absorbers.setEntityManager(&gameState->entityManager());
    m_impl->m_agents.setEntityManager(&gameState->entityManager());
    m_impl->m_world = gameState->physicsWorld();
    m_impl->m_agentCollisions.init(gameState);
}


void
AgentAbsorberSystem::shutdown() {
    m_impl->m_absorbers.setEntityManager(nullptr);
    m_impl->m_agents.setEntityManager(nullptr);
    m_impl->m_world = nullptr;
    m_impl->m_agentCollisions.shutdown();
    System::shutdown();
}


void
AgentAbsorberSystem::update(int) {
    for (const auto& entry : m_impl->m_absorbers) {
        AgentAbsorberComponent* absorber = std::get<0>(entry.second);
        absorber->m_absorbedAgents.clear();
    }
    for (Collision collision : m_impl->m_agentCollisions)
    {
        EntityId entityA = collision.entityId1;
        EntityId entityB = collision.entityId2;

        AgentAbsorberComponent* absorber = nullptr;
        AgentComponent* agent = nullptr;
        if (
            m_impl->m_agents.containsEntity(entityA) and
            m_impl->m_absorbers.containsEntity(entityB)
        ) {
            agent = std::get<0>(
                m_impl->m_agents.entities().at(entityA)
            );
            absorber = std::get<0>(
                m_impl->m_absorbers.entities().at(entityB)
            );
        }
        else if (
            m_impl->m_absorbers.containsEntity(entityA) and
            m_impl->m_agents.containsEntity(entityB)
        ) {
            absorber = std::get<0>(
                m_impl->m_absorbers.entities().at(entityA)
            );
            agent = std::get<0>(
                m_impl->m_agents.entities().at(entityB)
            );
        }
        if (agent and absorber and agent->m_timeToLive > 0) {
            absorber->m_absorbedAgents[agent->m_agentId] += agent->m_potency;
            agent->m_timeToLive = 0;
        }
    }
    m_impl->m_agentCollisions.clearCollisions();
}


////////////////////////////////////////////////////////////////////////////////
// AgentRegistry
////////////////////////////////////////////////////////////////////////////////

luabind::scope
AgentRegistry::luaBindings() {
    using namespace luabind;
    return class_<AgentRegistry>("AgentRegistry")
        .scope
        [
            def("registerAgentType", &AgentRegistry::registerAgentType),
            def("getAgentDisplayName", &AgentRegistry::getAgentDisplayName),
            def("getAgentInternalName", &AgentRegistry::getAgentInternalName),
            def("getAgentId", &AgentRegistry::getAgentId),
            def("getAgentMeshName", &AgentRegistry::getAgentMeshName)
        ]
    ;
}


namespace {
    struct AgentRegistryEntry
    {
        std::string internalName;
        std::string displayName;
        std::string meshName;
    };
}

//Hidden methods for acquiring global variable
static std::vector<AgentRegistryEntry>&
agentRegistry() {
    static std::vector<AgentRegistryEntry> agentRegistry;
    return agentRegistry;
}
static std::unordered_map<std::string, AgentId>&
agentRegistryMap() {
    static std::unordered_map<std::string, AgentId> agentRegistryMap;
    return agentRegistryMap;
}

AgentId
AgentRegistry::registerAgentType(
    const std::string& internalName,
    const std::string& displayName,
    const std::string& meshName
) {
    if (agentRegistryMap().count(internalName) == 0)
    {
        AgentRegistryEntry entry;
        entry.internalName = internalName;
        entry.displayName = displayName;
        entry.meshName = meshName;
        agentRegistry().push_back(entry);
        agentRegistryMap().emplace(std::string(internalName), agentRegistry().size());
        return agentRegistry().size();
    }
    else
    {
        throw std::invalid_argument("Duplicate internalName not allowed.");
    }
}

std::string
AgentRegistry::getAgentDisplayName(
    AgentId id
) {
    if (static_cast<std::size_t>(id) > agentRegistry().size())
        throw std::out_of_range("Index of agent does not exist.");
    return agentRegistry()[id-1].displayName;
}

std::string
AgentRegistry::getAgentInternalName(
    AgentId id
) {
    if (static_cast<std::size_t>(id) > agentRegistry().size())
        throw std::out_of_range("Index of agent does not exist.");
    return agentRegistry()[id-1].internalName;
}

AgentId
AgentRegistry::getAgentId(
    const std::string& internalName
) {
    AgentId agentId;
    try
    {
        agentId = agentRegistryMap().at(internalName);
    }
    catch(std::out_of_range&)
    {
        throw std::out_of_range("Internal name of agent does not exist.");
    }
    return agentId;
}

std::string
AgentRegistry::getAgentMeshName(
    AgentId id
) {
    if (static_cast<std::size_t>(id) > agentRegistry().size())
        throw std::out_of_range("Index of agent does not exist.");
    return agentRegistry()[id-1].meshName;
}
