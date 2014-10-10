#include "compound_absorber_system.h"

#include "bullet/collision_filter.h"
#include "bullet/collision_system.h"
#include "bullet/rigid_body_system.h"
#include "engine/component_factory.h"
#include "engine/engine.h"
#include "engine/entity_filter.h"
#include "engine/game_state.h"
#include "engine/serialization.h"
#include "game.h"
#include "ogre/scene_node_system.h"
#include "scripting/luabind.h"
#include "util/make_unique.h"
#include "microbe_stage/compound.h"
#include "microbe_stage/compound_registry.h"

#include "tinyxml.h"

#include <luabind/iterator_policy.hpp>
#include <OgreEntity.h>
#include <OgreSceneManager.h>
#include <stdexcept>

using namespace thrive;

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
CompoundAbsorberSystem::update(int, int) {
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
