#include "microbe_stage/agent.h"

#include "bullet/on_collision.h"
#include "bullet/rigid_body_system.h"
#include "engine/engine.h"
#include "engine/entity_filter.h"
#include "ogre/entity_system.h"
#include "ogre/scene_node_system.h"
#include "scripting/luabind.h"
#include "util/random.h"

using namespace thrive;

luabind::scope
AgentEmitterComponent::luaBindings() {
    using namespace luabind;
    return class_<AgentEmitterComponent, Component>("AgentEmitterComponent")
        .scope [
            def("TYPE_NAME", &AgentEmitterComponent::TYPE_NAME),
            def("TYPE_ID", &AgentEmitterComponent::TYPE_ID)
        ]
        .def(constructor<>())
        .def_readwrite("effectCallback", &AgentEmitterComponent::m_effectCallback)
        .def_readwrite("emissionRadius", &AgentEmitterComponent::m_emissionRadius)
        .def_readwrite("emitInterval", &AgentEmitterComponent::m_emitInterval)
        .def_readwrite("maxInitialSpeed", &AgentEmitterComponent::m_maxInitialSpeed)
        .def_readwrite("minInitialSpeed", &AgentEmitterComponent::m_minInitialSpeed)
        .def_readwrite("minEmissionAngle", &AgentEmitterComponent::m_minEmissionAngle)
        .def_readwrite("maxEmissionAngle", &AgentEmitterComponent::m_maxEmissionAngle)
        .def_readwrite("meshName", &AgentEmitterComponent::m_meshName)
        .def_readwrite("particlesPerEmission", &AgentEmitterComponent::m_particlesPerEmission)
        .def_readwrite("particleLifeTime", &AgentEmitterComponent::m_particleLifeTime)
        .def_readwrite("particleScale", &AgentEmitterComponent::m_particleScale)
    ;
}

////////////////////////////////////////////////////////////////////////////////
// AgentLifetimeSystem
////////////////////////////////////////////////////////////////////////////////

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
    Engine* engine
) {
    System::init(engine);
    m_impl->m_entities.setEntityManager(&engine->entityManager());
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
            this->engine()->entityManager().removeEntity(value.first);
        }
    }
}


////////////////////////////////////////////////////////////////////////////////
// AgentMovementSystem
////////////////////////////////////////////////////////////////////////////////

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
    Engine* engine
) {
    System::init(engine);
    m_impl->m_entities.setEntityManager(&engine->entityManager());
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

struct AgentEmitterSystem::Implementation {

    EntityFilter<
        AgentEmitterComponent,
        OgreSceneNodeComponent
    > m_entities;
};


AgentEmitterSystem::AgentEmitterSystem()
  : m_impl(new Implementation())
{
}


AgentEmitterSystem::~AgentEmitterSystem() {}


void
AgentEmitterSystem::init(
    Engine* engine
) {
    System::init(engine);
    m_impl->m_entities.setEntityManager(&engine->entityManager());
}


void
AgentEmitterSystem::shutdown() {
    m_impl->m_entities.setEntityManager(nullptr);
    System::shutdown();
}


void
AgentEmitterSystem::update(int milliseconds) {
    EntityManager& entityManager = this->engine()->entityManager();
    for (auto& value : m_impl->m_entities) {
        AgentEmitterComponent* emitterComponent = std::get<0>(value.second);
        OgreSceneNodeComponent* sceneNodeComponent = std::get<1>(value.second);
        emitterComponent->m_timeSinceLastEmission += milliseconds;
        while (
            emitterComponent->m_emitInterval > 0 and
            emitterComponent->m_timeSinceLastEmission >= emitterComponent->m_emitInterval
        ) {
            emitterComponent->m_timeSinceLastEmission -= emitterComponent->m_emitInterval;
            for (unsigned int i = 0; i < emitterComponent->m_particlesPerEmission; ++i) {
                Ogre::Degree emissionAngle = randomFromRange(
                    emitterComponent->m_minEmissionAngle,
                    emitterComponent->m_maxEmissionAngle
                );
                Ogre::Real emissionSpeed = randomFromRange(
                    emitterComponent->m_minInitialSpeed,
                    emitterComponent->m_maxInitialSpeed
                );
                Ogre::Vector3 emissionVelocity(
                    emissionSpeed * Ogre::Math::Sin(emissionAngle),
                    emissionSpeed * Ogre::Math::Cos(emissionAngle),
                    0.0
                );
                Ogre::Vector3 emissionPosition(
                    emitterComponent->m_emissionRadius * Ogre::Math::Sin(emissionAngle),
                    emitterComponent->m_emissionRadius * Ogre::Math::Cos(emissionAngle),
                    0.0
                );
                EntityId agentEntityId = entityManager.generateNewId();
                // Scene Node
                auto agentSceneNodeComponent = make_unique<OgreSceneNodeComponent>();
                agentSceneNodeComponent->m_transform.scale = emitterComponent->m_particleScale; 
                // Collision Hull
                auto agentRigidBodyComponent = make_unique<RigidBodyComponent>(
                    btBroadphaseProxy::SensorTrigger,
                    btBroadphaseProxy::AllFilter & (~ btBroadphaseProxy::SensorTrigger)
                );
                agentRigidBodyComponent->m_properties.shape = std::make_shared<btSphereShape>(0.01);
                agentRigidBodyComponent->m_properties.hasContactResponse = false;
                agentRigidBodyComponent->m_properties.kinematic = true;
                agentRigidBodyComponent->m_dynamicProperties.position = sceneNodeComponent->m_transform.position + emissionPosition; 
                // On Collision
                auto agentOnCollisionComponent = make_unique<OnCollisionComponent>();
                agentOnCollisionComponent->onCollisionCallback = emitterComponent->m_effectCallback;
                // Agent Component
                auto agentComponent = make_unique<AgentComponent>();
                agentComponent->m_timeToLive = emitterComponent->m_particleLifeTime;
                agentComponent->m_velocity = emissionVelocity;
                // Build component list
                std::list<std::unique_ptr<Component>> components;
                components.emplace_back(std::move(agentSceneNodeComponent));
                components.emplace_back(std::move(agentComponent));
                components.emplace_back(make_unique<OgreEntityComponent>(emitterComponent->m_meshName));
                components.emplace_back(std::move(agentRigidBodyComponent));
                components.emplace_back(std::move(agentOnCollisionComponent));
                for (auto& component : components) {
                    entityManager.addComponent(
                        agentEntityId,
                        std::move(component)
                    );
                }
            }
        }
    }
}
