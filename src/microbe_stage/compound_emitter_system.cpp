#include "compound_emitter_system.h"

#include "bullet/collision_system.h"
#include "bullet/rigid_body_system.h"
#include "engine/component_factory.h"
#include "engine/engine.h"
#include "engine/entity_filter.h"
#include "engine/game_state.h"
#include "engine/rng.h"
#include "engine/serialization.h"
#include "game.h"
#include "general/timed_life_system.h"
#include "ogre/scene_node_system.h"
#include "scripting/luabind.h"
#include "util/make_unique.h"
#include "microbe_stage/compound.h"
#include "microbe_stage/compound_registry.h"

#include <luabind/iterator_policy.hpp>
#include <OgreEntity.h>
#include <OgreSceneManager.h>
#include <stdexcept>

using namespace thrive;

////////////////////////////////////////////////////////////////////////////////
// CompoundEmitterComponent
////////////////////////////////////////////////////////////////////////////////

luabind::scope
CompoundEmitterComponent::luaBindings() {
    using namespace luabind;
    return class_<CompoundEmitterComponent, Component>("CompoundEmitterComponent")
        .enum_("ID") [
            value("TYPE_ID", CompoundEmitterComponent::TYPE_ID)
        ]
        .scope [
            def("TYPE_NAME", &CompoundEmitterComponent::TYPE_NAME)
        ]
        .def(constructor<>())
        .def("emitCompound", &CompoundEmitterComponent::emitCompound)
        .def_readwrite("emissionRadius", &CompoundEmitterComponent::m_emissionRadius)
        .def_readwrite("maxInitialSpeed", &CompoundEmitterComponent::m_maxInitialSpeed)
        .def_readwrite("minInitialSpeed", &CompoundEmitterComponent::m_minInitialSpeed)
        .def_readwrite("minEmissionAngle", &CompoundEmitterComponent::m_minEmissionAngle)
        .def_readwrite("maxEmissionAngle", &CompoundEmitterComponent::m_maxEmissionAngle)
        .def_readwrite("particleLifetime", &CompoundEmitterComponent::m_particleLifetime)
    ;
}


void
CompoundEmitterComponent::emitCompound(
    CompoundId compoundId,
    double amount,
    double angle,
    double radius
) {
    m_compoundEmissions.push_back(std::tuple<CompoundId, double, double, double>(compoundId, amount, angle, radius));
}



void
CompoundEmitterComponent::load(
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
CompoundEmitterComponent::storage() const {
    StorageContainer storage = Component::storage();
    storage.set<Ogre::Real>("emissionRadius", m_emissionRadius);
    storage.set<Ogre::Real>("maxInitialSpeed", m_maxInitialSpeed);
    storage.set<Ogre::Real>("minInitialSpeed", m_minInitialSpeed);
    storage.set<Ogre::Degree>("maxEmissionAngle", m_maxEmissionAngle);
    storage.set<Ogre::Degree>("minEmissionAngle", m_minEmissionAngle);
    storage.set<Milliseconds>("particleLifetime", m_particleLifetime);
    return storage;
}

REGISTER_COMPONENT(CompoundEmitterComponent)


////////////////////////////////////////////////////////////////////////////////
// TimedCompoundEmitterComponent
////////////////////////////////////////////////////////////////////////////////

luabind::scope
TimedCompoundEmitterComponent::luaBindings() {
    using namespace luabind;
    return class_<TimedCompoundEmitterComponent, Component>("TimedCompoundEmitterComponent")
        .enum_("ID") [
            value("TYPE_ID", TimedCompoundEmitterComponent::TYPE_ID)
        ]
        .scope [
            def("TYPE_NAME", &TimedCompoundEmitterComponent::TYPE_NAME)
        ]
        .def(constructor<>())
        .def_readwrite("emitInterval", &TimedCompoundEmitterComponent::m_emitInterval)
        .def_readwrite("compoundId", &TimedCompoundEmitterComponent::m_compoundId)
        .def_readwrite("particlesPerEmission", &TimedCompoundEmitterComponent::m_particlesPerEmission)
        .def_readwrite("potencyPerParticle", &TimedCompoundEmitterComponent::m_potencyPerParticle)
    ;
}


void
TimedCompoundEmitterComponent::load(
    const StorageContainer& storage
) {
    Component::load(storage);
    m_compoundId = storage.get<CompoundId>("compoundId", NULL_COMPOUND);
    m_particlesPerEmission = storage.get<uint16_t>("particlesPerEmission");
    m_potencyPerParticle = storage.get<float>("potencyPerParticle");
    m_emitInterval = storage.get<Milliseconds>("emitInterval", 1000);
    m_timeSinceLastEmission = storage.get<Milliseconds>("timeSinceLastEmission");
}


StorageContainer
TimedCompoundEmitterComponent::storage() const {
    StorageContainer storage = Component::storage();
    storage.set<CompoundId>("compoundId", m_compoundId);
    storage.set<uint16_t>("particlesPerEmission", m_particlesPerEmission);
    storage.set<float>("potencyPerParticle", m_potencyPerParticle);
    storage.set<Milliseconds>("emitInterval", m_emitInterval);
    storage.set<Milliseconds>("timeSinceLastEmission", m_timeSinceLastEmission);
    return storage;
}

REGISTER_COMPONENT(TimedCompoundEmitterComponent)


////////////////////////////////////////////////////////////////////////////////
// CompoundEmitterSystem
////////////////////////////////////////////////////////////////////////////////

luabind::scope
CompoundEmitterSystem::luaBindings() {
    using namespace luabind;
    return class_<CompoundEmitterSystem, System>("CompoundEmitterSystem")
        .def(constructor<>())
    ;
}


struct CompoundEmitterSystem::Implementation {

    EntityFilter<
        CompoundEmitterComponent,
        OgreSceneNodeComponent,
		Optional<TimedCompoundEmitterComponent>
    > m_entities;

    Ogre::SceneManager* m_sceneManager = nullptr;
};


CompoundEmitterSystem::CompoundEmitterSystem()
  : m_impl(new Implementation())
{
}


CompoundEmitterSystem::~CompoundEmitterSystem() {}


void
CompoundEmitterSystem::init(
    GameState* gameState
) {
    System::init(gameState);
    m_impl->m_entities.setEntityManager(&gameState->entityManager());
    m_impl->m_sceneManager = gameState->sceneManager();
}


void
CompoundEmitterSystem::shutdown() {
    m_impl->m_entities.setEntityManager(nullptr);
    m_impl->m_sceneManager = nullptr;
    System::shutdown();
}

// Helper function for CompoundEmitterSystem to emit compounds
static void
emitCompound(
    CompoundId compoundId,
    double amount,
    Ogre::Vector3 emittorPosition,
    double angle,
    double radius,
    CompoundEmitterComponent* emitterComponent,
    EntityId emittingEntityId
) {

    Ogre::Vector3 emissionOffset(0,0,0);


    Ogre::Degree emissionAngle{static_cast<Ogre::Real>(angle)};

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
        radius * Ogre::Math::Sin(emissionAngle),
        radius * Ogre::Math::Cos(emissionAngle),
        0.0
    );
    EntityId compoundEntityId = Game::instance().engine().currentGameState()->entityManager().generateNewId();
    // Scene Node
    auto compoundSceneNodeComponent = make_unique<OgreSceneNodeComponent>();
    auto meshScale = CompoundRegistry::getCompoundMeshScale(compoundId);
    compoundSceneNodeComponent->m_transform.scale = Ogre::Vector3(meshScale,meshScale,meshScale);
    compoundSceneNodeComponent->m_meshName = CompoundRegistry::getCompoundMeshName(compoundId);
    // Collision Hull
    auto compoundRigidBodyComponent = make_unique<RigidBodyComponent>(
        btBroadphaseProxy::SensorTrigger,
        btBroadphaseProxy::AllFilter & (~ btBroadphaseProxy::SensorTrigger)
    );
    compoundRigidBodyComponent->m_properties.shape = std::make_shared<SphereShape>(0.01);
    compoundRigidBodyComponent->m_properties.hasContactResponse = false;
    compoundRigidBodyComponent->m_properties.kinematic = true;
    compoundRigidBodyComponent->m_dynamicProperties.position = emittorPosition + emissionOffset;
    compoundRigidBodyComponent->m_pushbackEntity = emittingEntityId;
    compoundRigidBodyComponent->m_pushbackAngle = angle;
    // Compound Component
    auto compoundComponent = make_unique<CompoundComponent>();
    compoundComponent->m_velocity = emissionVelocity;
    compoundComponent->m_compoundId = compoundId;
    compoundComponent->m_potency = amount;
    auto timedLifeComponent = make_unique<TimedLifeComponent>();
    timedLifeComponent->m_timeToLive = emitterComponent->m_particleLifetime;
    auto collisionHandler = make_unique<CollisionComponent>();
    collisionHandler->addCollisionGroup("compound");
    // Build component list
    std::list<std::unique_ptr<Component>> components;
    components.emplace_back(std::move(compoundSceneNodeComponent));
    components.emplace_back(std::move(compoundComponent));
    components.emplace_back(std::move(timedLifeComponent));
    components.emplace_back(std::move(compoundRigidBodyComponent));
    components.emplace_back(std::move(collisionHandler));
    for (auto& component : components) {
        Game::instance().engine().currentGameState()->entityManager().addComponent(
            compoundEntityId,
            std::move(component)
        );
    }
}



void
CompoundEmitterSystem::update(int, int logicTime) {
    for (auto& value : m_impl->m_entities) {
        CompoundEmitterComponent* emitterComponent = std::get<0>(value.second);
        OgreSceneNodeComponent* sceneNodeComponent = std::get<1>(value.second);
        TimedCompoundEmitterComponent* timedEmitterComponent = std::get<2>(value.second);

        for (auto emission : emitterComponent->m_compoundEmissions)
        {
            emitCompound(std::get<0>(emission), std::get<1>(emission), sceneNodeComponent->m_transform.position, std::get<2>(emission), std::get<3>(emission), emitterComponent, value.first);
        }
        emitterComponent->m_compoundEmissions.clear();
        if (timedEmitterComponent)
        {
            timedEmitterComponent->m_timeSinceLastEmission += logicTime;
            while (
                timedEmitterComponent->m_emitInterval > 0 and
                timedEmitterComponent->m_timeSinceLastEmission >= timedEmitterComponent->m_emitInterval
            ) {
                timedEmitterComponent->m_timeSinceLastEmission -= timedEmitterComponent->m_emitInterval;
                for (unsigned int i = 0; i < timedEmitterComponent->m_particlesPerEmission; ++i) {
                    double angle = Game::instance().engine().rng().getDouble(
                        emitterComponent->m_minEmissionAngle.valueDegrees(),
                        emitterComponent->m_maxEmissionAngle.valueDegrees()
                    );
                    emitCompound(timedEmitterComponent->m_compoundId, timedEmitterComponent->m_potencyPerParticle, sceneNodeComponent->m_transform.position, angle,  emitterComponent->m_emissionRadius, emitterComponent, value.first);
                }
            }
        }
    }
}
