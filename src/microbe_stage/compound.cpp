#include "microbe_stage/compound.h"

#include "bullet/collision_filter.h"
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

#include "tinyxml.h"

#include <luabind/iterator_policy.hpp>
#include <OgreEntity.h>
#include <OgreSceneManager.h>
#include <stdexcept>


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
    m_velocity = storage.get<Ogre::Vector3>("velocity");
}


StorageContainer
CompoundComponent::storage() const {
    StorageContainer storage = Component::storage();
    storage.set<CompoundId>("compoundId", m_compoundId);
    storage.set<float>("potency", m_potency);
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
        .def("getAbsorbedCompounds", &CompoundAbsorberComponent::getAbsorbedCompounds, return_stl_iterator)
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

BoostAbsorbedMapIterator
CompoundAbsorberComponent::getAbsorbedCompounds() {
    return m_absorbedCompounds | boost::adaptors::map_keys;
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
CompoundMovementSystem::update(int milliseconds, bool paused) {
    if (!paused) {
        for (auto& value : m_impl->m_entities) {
            CompoundComponent* compoundComponent = std::get<0>(value.second);
            RigidBodyComponent* rigidBodyComponent = std::get<1>(value.second);
            Ogre::Vector3 delta = compoundComponent->m_velocity * float(milliseconds) / 1000.0f;
            rigidBodyComponent->m_dynamicProperties.position += delta;
        }
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
    double angle,
    double radius,
    CompoundEmitterComponent* emitterComponent
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
CompoundEmitterSystem::update(int milliseconds, bool paused) {
    for (auto& value : m_impl->m_entities) {
        CompoundEmitterComponent* emitterComponent = std::get<0>(value.second);
        OgreSceneNodeComponent* sceneNodeComponent = std::get<1>(value.second);
        TimedCompoundEmitterComponent* timedEmitterComponent = std::get<2>(value.second);

        for (auto emission : emitterComponent->m_compoundEmissions)
        {
            emitCompound(std::get<0>(emission), std::get<1>(emission), sceneNodeComponent->m_transform.position, std::get<2>(emission), std::get<3>(emission), emitterComponent);
        }
        emitterComponent->m_compoundEmissions.clear();
        if (timedEmitterComponent && !paused)
        {
            timedEmitterComponent->m_timeSinceLastEmission += milliseconds;
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
                    emitCompound(timedEmitterComponent->m_compoundId, timedEmitterComponent->m_potencyPerParticle, sceneNodeComponent->m_transform.position, angle,  emitterComponent->m_emissionRadius, emitterComponent);
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
CompoundAbsorberSystem::update(int, bool) {
    for (const auto& entry : m_impl->m_absorbers) {
        CompoundAbsorberComponent* absorber = std::get<0>(entry.second);
        absorber->m_absorbedCompounds.clear();
    }
    for (Collision collision : m_impl->m_compoundCollisions)
    {
        EntityId entityA = collision.entityId1;
        EntityId entityB = collision.entityId2;
        EntityId compoundEntity = NULL_ENTITY;
        EntityId absorberEntity = NULL_ENTITY;

        CompoundAbsorberComponent* absorber = nullptr;
        CompoundComponent* compound = nullptr;
        if (
            m_impl->m_compounds.containsEntity(entityA) and
            m_impl->m_absorbers.containsEntity(entityB)
        ) {
            compoundEntity = entityA;
            absorberEntity = entityB;
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
            compoundEntity = entityB;
            absorberEntity = entityA;
            absorber = std::get<0>(
                m_impl->m_absorbers.entities().at(entityA)
            );
            compound = std::get<0>(
                m_impl->m_compounds.entities().at(entityB)
            );
        }
        if (compound and absorber and absorber->m_enabled == true and absorber->canAbsorbCompound(compound->m_compoundId)) {
            if (CompoundRegistry::isAgentType(compound->m_compoundId)){
                (*CompoundRegistry::getAgentEffect(compound->m_compoundId))(absorberEntity, compound->m_potency);
                this->entityManager()->removeEntity(compoundEntity);
            }
            else if(absorber->m_absorbtionCapacity >= compound->m_potency * CompoundRegistry::getCompoundUnitVolume(compound->m_compoundId)){
                absorber->m_absorbedCompounds[compound->m_compoundId] += compound->m_potency;
                this->entityManager()->removeEntity(compoundEntity);
            }
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
            def("registerAgentType",
                static_cast<CompoundId (*)(
                    const std::string&,
                    const std::string&,
                    const std::string&,
                    double,
                    int,
                    const luabind::object&
                )>(&CompoundRegistry::registerAgentType)
            ),
            def("loadFromXML", &CompoundRegistry::loadFromXML),
            def("getCompoundDisplayName", &CompoundRegistry::getCompoundDisplayName),
            def("getCompoundInternalName", &CompoundRegistry::getCompoundInternalName),
			def("getCompoundMeshName", &CompoundRegistry::getCompoundMeshName),
            def("getCompoundUnitVolume", &CompoundRegistry::getCompoundUnitVolume),
            def("getCompoundId", &CompoundRegistry::getCompoundId),
            def("getCompoundList", &CompoundRegistry::getCompoundList, return_stl_iterator),
            def("getCompoundMeshScale", &CompoundRegistry::getCompoundMeshScale),
            def("getAgentEffect", &CompoundRegistry::getAgentEffect)
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
        bool isAgent;
        std::function<bool(EntityId, double)>* effect;
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

void
CompoundRegistry::loadFromXML(
    const std::string& filename
) {
    TiXmlDocument doc(filename.c_str());
    bool loadOkay = doc.LoadFile();
    if (loadOkay)
	{
	    // Handles used for null-safety when possible
		TiXmlHandle hDoc(&doc),
                    hCompounds(0),
                    hDisplay(0),
                    hModel(0),
                    hAgents(0),
                    hEffect(0);
        // Elements used for iteration with explicit null-checks
        TiXmlElement * pCompound,
                     * pAgent;

        hCompounds=hDoc.FirstChildElement("Compounds");

		pCompound=hCompounds.FirstChildElement("Compound").Element();
        while (pCompound)
		{
		    hDisplay = TiXmlHandle(pCompound->FirstChildElement("Display"));
            hModel = TiXmlHandle(hDisplay.FirstChildElement("Model"));
            int molecularWeight;
            double modelSize;
            if (pCompound->QueryIntAttribute("weight", &molecularWeight) != TIXML_SUCCESS){
                throw std::logic_error("Could not access 'weight' attribute on compound element of " + filename);
            }
            if (hModel.Element()->QueryDoubleAttribute("size", &modelSize) != TIXML_SUCCESS){
                throw std::logic_error("Could not access 'size' attribute on Model element of " + filename);
            }
            const char* name = pCompound->Attribute("name");
            if (name == nullptr) {
                throw std::logic_error("Could not access 'name' attribute on compound element of " + filename);
            }
            const char* displayName = hDisplay.Element()->Attribute("text");
            if (displayName == nullptr) {
                throw std::logic_error("Could not access 'text' attribute on Display element of " + filename);
            }
            const char* meshname = hModel.Element()->Attribute("file");
            if (meshname == nullptr) {
                throw std::logic_error("Could not access 'file' attribute on Model element of " + filename);
            }
            registerCompoundType(
                name,
                displayName,
                meshname,
                modelSize,
                molecularWeight
            );
            pCompound=pCompound->NextSiblingElement("Compound");
		}
		hAgents=hCompounds.FirstChildElement("AgentCompounds");
		pAgent=hAgents.FirstChildElement("Agent").Element();
        while (pAgent)
		{
		    hDisplay = TiXmlHandle(pAgent->FirstChildElement("Display"));
            hModel = TiXmlHandle(hDisplay.FirstChildElement("Model"));
            hEffect = TiXmlHandle(pAgent->FirstChildElement("Effect"));
            int molecularWeight;
            double modelSize;
            if (pAgent->QueryIntAttribute("weight", &molecularWeight) != TIXML_SUCCESS){
                throw std::logic_error("Could not access 'weight' attribute on Compound element of " + filename);
            }
            if (hModel.Element()->QueryDoubleAttribute("size", &modelSize) != TIXML_SUCCESS){

                throw std::logic_error("Could not access 'size' attribute on Model element of " + filename);
            }
            const char* functionName = hEffect.Element()->Attribute("function");
            if (functionName == nullptr) {
                throw std::logic_error("Could not access 'function' attribute on Effect element of " + filename);
            }
            std::string luaFunctionName = std::string(functionName);
            // Create a lambda to call the function defined in the XML document
            auto effectLambda = new std::function<bool(EntityId, double)>(
                [luaFunctionName](EntityId entityId, double potency) -> bool
                {
                    luabind::call_function<void>(Game::instance().engine().luaState(), luaFunctionName.c_str(), entityId, potency);
                    return true;
                });
            const char* name = pAgent->Attribute("name");
            if (name == nullptr) {
                throw std::logic_error("Could not access 'name' attribute on compound element of " + filename);
            }
            const char* displayName = hDisplay.Element()->Attribute("text");
            if (displayName == nullptr) {
                throw std::logic_error("Could not access 'text' attribute on Display element of " + filename);
            }
            const char* meshname = hModel.Element()->Attribute("file");
            if (meshname == nullptr) {
                throw std::logic_error("Could not access 'file' attribute on Model element of " + filename);
            }
            //Register the agent type
            registerAgentType(
                name,
                displayName,
                meshname,
                modelSize,
                molecularWeight,
                effectLambda
            );
            pAgent=pAgent->NextSiblingElement("Agent");
		}
	}
	else {
		throw std::invalid_argument(doc.ErrorDesc());
	}
}

CompoundId
CompoundRegistry::registerCompoundType(
    const std::string& internalName,
    const std::string& displayName,
	const std::string& meshName,
    double meshScale,
    int unitVolume
) {
    return registerAgentType(internalName,
                         displayName,
                         meshName,
                         meshScale,
                         unitVolume,
                         static_cast<std::function<bool(EntityId, double)>*>(nullptr));
}


//Luabind version
CompoundId
CompoundRegistry::registerAgentType(
    const std::string& internalName,
    const std::string& displayName,
	const std::string& meshName,
    double meshScale,
    int unitVolume,
    const luabind::object& effect
) {
    auto effectLambda = new std::function<bool(EntityId, double)>(
        [effect](EntityId entityId, double potency) -> bool
        {
            luabind::call_function<void>(effect, entityId, potency);
            return true;
        });
    //Call overload
    return registerAgentType(
        internalName,
         displayName,
         meshName,
         meshScale,
         unitVolume,
         effectLambda);
}

CompoundId
CompoundRegistry::registerAgentType(
    const std::string& internalName,
    const std::string& displayName,
	const std::string& meshName,
    double meshScale,
    int unitVolume,
    std::function<bool(EntityId, double)>* effect
) {
    if (compoundRegistryMap().count(internalName) == 0)
    {
        CompoundRegistryEntry entry;
        entry.internalName = internalName;
        entry.displayName = displayName;
		entry.meshName = meshName;
        entry.meshScale = meshScale;
        entry.unitVolume = unitVolume;
        entry.effect = effect;
        entry.isAgent = (effect != nullptr);
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
const BoostCompoundMapIterator
CompoundRegistry::getCompoundList(
) {
    return compoundRegistryMap() | boost::adaptors::map_values;
}

std::function<bool(EntityId, double)>*
CompoundRegistry::getAgentEffect(
    CompoundId id
) {
    if (static_cast<std::size_t>(id) > compoundRegistry().size())
        throw std::out_of_range("Index of compound does not exist.");
    return compoundRegistry()[id-1].effect;
}


bool
CompoundRegistry::isAgentType(
    CompoundId id
) {
    if (static_cast<std::size_t>(id) > compoundRegistry().size())
        throw std::out_of_range("Index of compound does not exist.");
    return compoundRegistry()[id-1].isAgent;
}
