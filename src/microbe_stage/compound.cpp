#include "microbe_stage/compound.h"

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
#include <luabind/iterator_policy.hpp>
#include "ogre/scene_node_system.h"
#include "scripting/luabind.h"
#include <OgreEntity.h>
#include <OgreSceneManager.h>
#include "util/make_unique.h"

using namespace thrive;

REGISTER_COMPONENT(CompoundComponent)


luabind::scope
CompoundComponent::luaBindings() {
    using namespace luabind;
    return class_<CompoundComponent, Component>("CompoundComponent")
        .enum_("ID") [
            value("TYPE_ID", CompoundComponent::TYPE_ID)
        ]
        .scope [
            def("TYPE_NAME", &CompoundComponent::TYPE_NAME)
        ]
        .def(constructor<>())
        .def_readwrite("compoundId", &CompoundComponent::m_compoundId)
        .def_readwrite("potency", &CompoundComponent::m_potency)
        .def_readwrite("timeToLive", &CompoundComponent::m_timeToLive)
        .def_readwrite("velocity", &CompoundComponent::m_velocity)
    ;
}


void
CompoundComponent::load(
    const StorageContainer& storage
) {
    Component::load(storage);
    m_compoundId = storage.get<CompoundId>("compoundId", NULL_COMPOUND);
    m_potency = storage.get<float>("potency");
    m_timeToLive = storage.get<Milliseconds>("timeToLive");
    m_velocity = storage.get<Ogre::Vector3>("velocity");
}


StorageContainer
CompoundComponent::storage() const {
    StorageContainer storage = Component::storage();
    storage.set<CompoundId>("compoundId", m_compoundId);
    storage.set<float>("potency", m_potency);
    storage.set<Milliseconds>("timeToLive", m_timeToLive);
    storage.set<Ogre::Vector3>("velocity", m_velocity);
    return storage;
}

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
    double amount
) {
    m_compoundEmissions.push_back(std::pair<CompoundId, int>(compoundId, amount));
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
// CompoundAbsorberComponent
////////////////////////////////////////////////////////////////////////////////

luabind::scope
CompoundAbsorberComponent::luaBindings() {
    using namespace luabind;
    return class_<CompoundAbsorberComponent, Component>("CompoundAbsorberComponent")
        .enum_("ID") [
            value("TYPE_ID", CompoundAbsorberComponent::TYPE_ID)
        ]
        .scope [
            def("TYPE_NAME", &CompoundAbsorberComponent::TYPE_NAME)
        ]
        .def(constructor<>())
        .def("absorbedCompoundAmount", &CompoundAbsorberComponent::absorbedCompoundAmount)
        .def("setAbsorbedCompoundAmount", &CompoundAbsorberComponent::setAbsorbedCompoundAmount)
        .def("setCanAbsorbCompound", &CompoundAbsorberComponent::setCanAbsorbCompound)
        .def("setAbsorbtionCapacity", &CompoundAbsorberComponent::setAbsorbtionCapacity)
        .def("enable", &CompoundAbsorberComponent::enable)
        .def("disable", &CompoundAbsorberComponent::disable)
    ;
}


float
CompoundAbsorberComponent::absorbedCompoundAmount(
    CompoundId id
) const {
    const auto& iter = m_absorbedCompounds.find(id);
    if (iter != m_absorbedCompounds.cend()) {
        return iter->second;
    }
    else {
        return 0.0f;
    }
}


bool
CompoundAbsorberComponent::canAbsorbCompound(
    CompoundId id
) const {
    return m_canAbsorbCompound.find(id) != m_canAbsorbCompound.end();
}

void
CompoundAbsorberComponent::setAbsorbtionCapacity(
    double capacity
) {
    m_absorbtionCapacity = capacity;
}

void
CompoundAbsorberComponent::enable(){
    m_enabled = true;
}

void
CompoundAbsorberComponent::disable(){
    m_enabled = false;
}

void
CompoundAbsorberComponent::load(
    const StorageContainer& storage
) {
    Component::load(storage);
    StorageList compounds = storage.get<StorageList>("compounds");
    for (const StorageContainer& container : compounds) {
        CompoundId compoundId = container.get<CompoundId>("compoundId");
        float amount = container.get<float>("amount");
        m_absorbedCompounds[compoundId] = amount;
        m_canAbsorbCompound.insert(compoundId);
    }
    m_enabled = storage.get<bool>("enabled");
}


void
CompoundAbsorberComponent::setAbsorbedCompoundAmount(
    CompoundId id,
    float amount
) {
    m_absorbedCompounds[id] = amount;
}


void
CompoundAbsorberComponent::setCanAbsorbCompound(
    CompoundId id,
    bool canAbsorb
) {
    if (canAbsorb) {
        m_canAbsorbCompound.insert(id);
    }
    else {
        m_canAbsorbCompound.erase(id);
    }
}


StorageContainer
CompoundAbsorberComponent::storage() const {
    StorageContainer storage = Component::storage();
    StorageList compounds;
    compounds.reserve(m_canAbsorbCompound.size());
    for (CompoundId compoundId : m_canAbsorbCompound) {
        StorageContainer container;
        container.set<CompoundId>("compoundId", compoundId);
        container.set<float>("amount", this->absorbedCompoundAmount(compoundId));
        compounds.append(container);
    }
    storage.set<StorageList>("compounds", compounds);
    storage.set<bool>("enabled", m_enabled);
    return storage;
}

REGISTER_COMPONENT(CompoundAbsorberComponent)

////////////////////////////////////////////////////////////////////////////////
// CompoundLifetimeSystem
////////////////////////////////////////////////////////////////////////////////

luabind::scope
CompoundLifetimeSystem::luaBindings() {
    using namespace luabind;
    return class_<CompoundLifetimeSystem, System>("CompoundLifetimeSystem")
        .def(constructor<>())
    ;
}


struct CompoundLifetimeSystem::Implementation {

    EntityFilter<
        CompoundComponent
    > m_entities;
};


CompoundLifetimeSystem::CompoundLifetimeSystem()
  : m_impl(new Implementation())
{
}


CompoundLifetimeSystem::~CompoundLifetimeSystem() {}


void
CompoundLifetimeSystem::init(
    GameState* gameState
) {
    System::init(gameState);
    m_impl->m_entities.setEntityManager(&gameState->entityManager());
}


void
CompoundLifetimeSystem::shutdown() {
    m_impl->m_entities.setEntityManager(nullptr);
    System::shutdown();
}


void
CompoundLifetimeSystem::update(int milliseconds) {
    for (auto& value : m_impl->m_entities) {
        CompoundComponent* compoundComponent = std::get<0>(value.second);
        compoundComponent->m_timeToLive -= milliseconds;
        if (compoundComponent->m_timeToLive <= 0) {
            this->entityManager()->removeEntity(value.first);
        }
    }
}


////////////////////////////////////////////////////////////////////////////////
// CompoundMovementSystem
////////////////////////////////////////////////////////////////////////////////

luabind::scope
CompoundMovementSystem::luaBindings() {
    using namespace luabind;
    return class_<CompoundMovementSystem, System>("CompoundMovementSystem")
        .def(constructor<>())
    ;
}


struct CompoundMovementSystem::Implementation {

    EntityFilter<
        CompoundComponent,
        RigidBodyComponent
    > m_entities;
};


CompoundMovementSystem::CompoundMovementSystem()
  : m_impl(new Implementation())
{
}


CompoundMovementSystem::~CompoundMovementSystem() {}


void
CompoundMovementSystem::init(
    GameState* gameState
) {
    System::init(gameState);
    m_impl->m_entities.setEntityManager(&gameState->entityManager());
}


void
CompoundMovementSystem::shutdown() {
    m_impl->m_entities.setEntityManager(nullptr);
    System::shutdown();
}


void
CompoundMovementSystem::update(int milliseconds) {
    for (auto& value : m_impl->m_entities) {
        CompoundComponent* compoundComponent = std::get<0>(value.second);
        RigidBodyComponent* rigidBodyComponent = std::get<1>(value.second);
        Ogre::Vector3 delta = compoundComponent->m_velocity * float(milliseconds) / 1000.0f;
        rigidBodyComponent->m_dynamicProperties.position += delta;
    }
}


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
    CompoundEmitterComponent* emitterComponent
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
    // Compound Component
    auto compoundComponent = make_unique<CompoundComponent>();
    compoundComponent->m_timeToLive = emitterComponent->m_particleLifetime;
    compoundComponent->m_velocity = emissionVelocity;
    compoundComponent->m_compoundId = compoundId;
    compoundComponent->m_potency = amount;
    auto collisionHandler = make_unique<CollisionComponent>();
    collisionHandler->addCollisionGroup("compound");
    // Build component list
    std::list<std::unique_ptr<Component>> components;
    components.emplace_back(std::move(compoundSceneNodeComponent));
    components.emplace_back(std::move(compoundComponent));
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
CompoundEmitterSystem::update(int milliseconds) {
    for (auto& value : m_impl->m_entities) {
        CompoundEmitterComponent* emitterComponent = std::get<0>(value.second);
        OgreSceneNodeComponent* sceneNodeComponent = std::get<1>(value.second);
        TimedCompoundEmitterComponent* timedEmitterComponent = std::get<2>(value.second);

        for (auto emission : emitterComponent->m_compoundEmissions)
        {
            emitCompound(std::get<0>(emission), std::get<1>(emission), sceneNodeComponent->m_transform.position, emitterComponent);
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
                     emitCompound(timedEmitterComponent->m_compoundId, timedEmitterComponent->m_potencyPerParticle, sceneNodeComponent->m_transform.position, emitterComponent);
                }
            }
        }
    }
}


////////////////////////////////////////////////////////////////////////////////
// CompoundAbsorberSystem
////////////////////////////////////////////////////////////////////////////////

luabind::scope
CompoundAbsorberSystem::luaBindings() {
    using namespace luabind;
    return class_<CompoundAbsorberSystem, System>("CompoundAbsorberSystem")
        .def(constructor<>())
    ;
}

struct CompoundAbsorberSystem::Implementation {

    Implementation()
      : m_compoundCollisions("microbe", "compound")
    {
    }

    EntityFilter<
        CompoundAbsorberComponent
    > m_absorbers;

    EntityFilter<
        CompoundComponent
    > m_compounds;

    btDiscreteDynamicsWorld* m_world = nullptr;

    CollisionFilter m_compoundCollisions;

};


CompoundAbsorberSystem::CompoundAbsorberSystem()
  : m_impl(new Implementation())
{
}


CompoundAbsorberSystem::~CompoundAbsorberSystem() {}


void
CompoundAbsorberSystem::init(
    GameState* gameState
) {
    System::init(gameState);
    m_impl->m_absorbers.setEntityManager(&gameState->entityManager());
    m_impl->m_compounds.setEntityManager(&gameState->entityManager());
    m_impl->m_world = gameState->physicsWorld();
    m_impl->m_compoundCollisions.init(gameState);
}


void
CompoundAbsorberSystem::shutdown() {
    m_impl->m_absorbers.setEntityManager(nullptr);
    m_impl->m_compounds.setEntityManager(nullptr);
    m_impl->m_world = nullptr;
    m_impl->m_compoundCollisions.shutdown();
    System::shutdown();
}


void
CompoundAbsorberSystem::update(int) {
    for (const auto& entry : m_impl->m_absorbers) {
        CompoundAbsorberComponent* absorber = std::get<0>(entry.second);
        absorber->m_absorbedCompounds.clear();
    }
    for (Collision collision : m_impl->m_compoundCollisions)
    {
        EntityId entityA = collision.entityId1;
        EntityId entityB = collision.entityId2;

        CompoundAbsorberComponent* absorber = nullptr;
        CompoundComponent* compound = nullptr;
        if (
            m_impl->m_compounds.containsEntity(entityA) and
            m_impl->m_absorbers.containsEntity(entityB)
        ) {
            compound = std::get<0>(
                m_impl->m_compounds.entities().at(entityA)
            );
            absorber = std::get<0>(
                m_impl->m_absorbers.entities().at(entityB)
            );
        }
        else if (
            m_impl->m_absorbers.containsEntity(entityA) and
            m_impl->m_compounds.containsEntity(entityB)
        ) {
            absorber = std::get<0>(
                m_impl->m_absorbers.entities().at(entityA)
            );
            compound = std::get<0>(
                m_impl->m_compounds.entities().at(entityB)
            );
        }
        if (compound and absorber and absorber->m_enabled == true and absorber->m_absorbtionCapacity >= compound->m_potency * CompoundRegistry::getCompoundUnitVolume(compound->m_compoundId) and
                                        absorber->canAbsorbCompound(compound->m_compoundId) and compound->m_timeToLive > 0) {
            absorber->m_absorbedCompounds[compound->m_compoundId] += compound->m_potency;
            compound->m_timeToLive = 0;
        }

    }

    m_impl->m_compoundCollisions.clearCollisions();
}


////////////////////////////////////////////////////////////////////////////////
// CompoundRegistry
////////////////////////////////////////////////////////////////////////////////

luabind::scope
CompoundRegistry::luaBindings() {
    using namespace luabind;
    return class_<CompoundRegistry>("CompoundRegistry")
        .scope
        [
            def("registerCompoundType", &CompoundRegistry::registerCompoundType),
            def("getCompoundDisplayName", &CompoundRegistry::getCompoundDisplayName),
            def("getCompoundInternalName", &CompoundRegistry::getCompoundInternalName),
			def("getCompoundMeshName", &CompoundRegistry::getCompoundMeshName),
            def("getCompoundUnitVolume", &CompoundRegistry::getCompoundUnitVolume),
            def("getCompoundId", &CompoundRegistry::getCompoundId),
            def("getCompoundList", &CompoundRegistry::getCompoundList, return_stl_iterator),
            def("getCompoundMeshScale", &CompoundRegistry::getCompoundMeshScale)
        ]
    ;
}


namespace {
    struct CompoundRegistryEntry
    {
        std::string internalName;
        std::string displayName;
        int unitVolume;
		std::string meshName;
        double meshScale;
    };
}

static std::vector<CompoundRegistryEntry>&
compoundRegistry() {
    static std::vector<CompoundRegistryEntry> compoundRegistry;
    return compoundRegistry;
}
static std::unordered_map<std::string, CompoundId>&
compoundRegistryMap() {
    static std::unordered_map<std::string, CompoundId> compoundRegistryMap;
    return compoundRegistryMap;
}

CompoundId
CompoundRegistry::registerCompoundType(
    const std::string& internalName,
    const std::string& displayName,
	const std::string& meshName,
    double meshScale,
    int unitVolume
) {
    if (compoundRegistryMap().count(internalName) == 0)
    {
        CompoundRegistryEntry entry;
        entry.internalName = internalName;
        entry.displayName = displayName;
		entry.meshName = meshName;
        entry.meshScale = meshScale;
        entry.unitVolume = unitVolume;
        compoundRegistry().push_back(entry);
        compoundRegistryMap().emplace(std::string(internalName), compoundRegistry().size());
        return compoundRegistry().size();
    }
    else
    {
        throw std::invalid_argument("Duplicate internalName not allowed.");
    }
}

std::string
CompoundRegistry::getCompoundDisplayName(
    CompoundId id
) {
    if (static_cast<std::size_t>(id) > compoundRegistry().size())
        throw std::out_of_range("Index of compound does not exist.");
    return compoundRegistry()[id-1].displayName;
}

std::string
CompoundRegistry::getCompoundInternalName(
    CompoundId id
) {
    if (static_cast<std::size_t>(id) > compoundRegistry().size())
        throw std::out_of_range("Index of compound does not exist.");
    return compoundRegistry()[id-1].internalName;
}

int
CompoundRegistry::getCompoundUnitVolume(
    CompoundId id
) {
    if (static_cast<std::size_t>(id) > compoundRegistry().size())
        throw std::out_of_range("Index of compound does not exist.");
    return compoundRegistry()[id-1].unitVolume;
}

CompoundId
CompoundRegistry::getCompoundId(
    const std::string& internalName
) {
    CompoundId compoundId;
    try
    {
        compoundId = compoundRegistryMap().at(internalName);
    }
    catch(std::out_of_range&)
    {
        throw std::out_of_range("Internal name of compound does not exist.");
    }
    return compoundId;
}

std::string
CompoundRegistry::getCompoundMeshName(
    CompoundId id
) {
    if (static_cast<std::size_t>(id) > compoundRegistry().size())
        throw std::out_of_range("Index of compound does not exist.");
    return compoundRegistry()[id-1].meshName;
}

double
CompoundRegistry::getCompoundMeshScale(
    CompoundId compoundId


) {
    if (static_cast<std::size_t>(compoundId) > compoundRegistry().size())
        throw std::out_of_range("Index of compound does not exist.");
    return compoundRegistry()[compoundId-1].meshScale;
}
const boost::range_detail::select_second_mutable_range<std::unordered_map<std::string, CompoundId>>
CompoundRegistry::getCompoundList(
) {
    return compoundRegistryMap() | boost::adaptors::map_values;
}
