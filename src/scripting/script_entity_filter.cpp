#include "scripting/script_entity_filter.h"

#include "engine/component.h"
#include "engine/component_collection.h"
#include "engine/entity_manager.h"
#include "engine/game_state.h"
#include "game.h"

using namespace thrive;

struct ScriptEntityFilter::Implementation {

    Implementation(
        sol::table componentTypes,
        bool recordChanges
    ) : m_recordChanges(recordChanges)
    {
        if (componentTypes.get_type() != sol::type::table) {
            throw std::runtime_error("ScriptEntityFilter constructor expects a list "
                "(table) of component types");
        }
        for (const auto& pair : componentTypes) {
            ComponentTypeId typeId = pair.second.as<sol::table>().get<
                ComponentTypeId>("TYPE_ID");
            
            m_requiredComponents.insert(typeId);
        }
    }

    void
    addEntity(
        EntityId id
    ) {
        if (m_recordChanges) {
            m_addedEntities.insert(id);
        }
        m_entities.insert(id);
    }

    void
    initialize() {
        for (EntityId id : m_entityManager->entities()) {
            if (this->isEligible(id)) {
                this->addEntity(id);
            }
        }
    }

    bool
    isEligible(
        EntityId id
    ) const {
        if (not m_entityManager or m_requiredComponents.empty()) {
            return false;
        }
        for (ComponentTypeId typeId : m_requiredComponents) {
            if (not m_entityManager->getComponent(id, typeId)) {
                return false;
            }
        }
        return true;
    }

    void
    registerCallbacks() {
        for (ComponentTypeId typeId : m_requiredComponents) {
            auto& collection = m_entityManager->getComponentCollection(typeId);
            auto onAdded = [this] (EntityId id, Component&) {
                if (this->isEligible(id)) {
                    this->addEntity(id);
                }
            };
            auto onRemoved = [this] (EntityId id, Component&) {
                this->removeEntity(id);
            };
            unsigned int handle = collection.registerChangeCallbacks(
                onAdded, 
                onRemoved
            );
            m_registeredCallbacks[typeId] = handle;
        }
    }

    void
    setEntityManager(
        EntityManager* entityManager
    ) {
        if (m_entityManager) {
            this->unregisterCallbacks();
        }
        m_entityManager = entityManager;
        m_addedEntities.clear();
        m_removedEntities.clear();
        m_entities.clear();
        if (entityManager) {
            this->initialize();
            this->registerCallbacks();
        }
    }

    void
    removeEntity(
        EntityId id
    ) {
        if (m_recordChanges) {
            m_removedEntities.insert(id);
        }
        m_entities.erase(id);
    }

    void
    unregisterCallbacks() {
        for (const auto& pair : m_registeredCallbacks) {
            auto& collection = m_entityManager->getComponentCollection(
                pair.first
            );
            collection.unregisterChangeCallbacks(pair.second);
        }
        m_registeredCallbacks.clear();
    }

    std::unordered_set<EntityId> m_addedEntities;

    std::unordered_set<EntityId> m_entities;

    EntityManager* m_entityManager = nullptr;

    bool m_recordChanges = false;

    std::unordered_map<ComponentTypeId, unsigned int> m_registeredCallbacks;

    std::unordered_set<EntityId> m_removedEntities;

    std::unordered_set<ComponentTypeId> m_requiredComponents;

};

void ScriptEntityFilter::luaBindings(
    sol::state &lua
){
    lua.new_usertype<ScriptEntityFilter>("EntityFilter",

        sol::constructors<sol::types<sol::table>, sol::types<sol::table, bool>>(),

        "addedEntities", &ScriptEntityFilter::addedEntities, 
        "clearChanges", &ScriptEntityFilter::clearChanges,
        "containsEntity", &ScriptEntityFilter::containsEntity,
        "entities", &ScriptEntityFilter::entities, 
        "init", &ScriptEntityFilter::init,
        "removedEntities", &ScriptEntityFilter::removedEntities, 
        "shutdown", &ScriptEntityFilter::shutdown
    );
}

ScriptEntityFilter::ScriptEntityFilter(
    sol::table componentTypes,
    bool recordChanges
) : m_impl(new Implementation(componentTypes, recordChanges))
{
}


ScriptEntityFilter::ScriptEntityFilter(
    sol::table componentTypes
) : m_impl(new Implementation(componentTypes, false))
{
}


ScriptEntityFilter::~ScriptEntityFilter() {
    assert(
        not m_impl->m_entityManager && 
        "Entity filter still active while being destroyed. Call shutdown() on it."
    );
}


const std::unordered_set<EntityId>&
ScriptEntityFilter::addedEntities() {
    return m_impl->m_addedEntities;
}


void
ScriptEntityFilter::clearChanges() {
    m_impl->m_addedEntities.clear();
    m_impl->m_removedEntities.clear();
}


bool
ScriptEntityFilter::containsEntity(
    EntityId id
) const {
    return m_impl->m_entities.count(id) > 0;
}


const std::unordered_set<EntityId>&
ScriptEntityFilter::entities() {
    if (not m_impl->m_entityManager) {
        throw std::runtime_error("Entity filter is not initialized. Call init() on it.");
    }
    return m_impl->m_entities;
}


void
ScriptEntityFilter::init(
    GameStateData* gameState
) {
    m_impl->setEntityManager(
        gameState->entityManager()
    );
}


const std::unordered_set<EntityId>&
ScriptEntityFilter::removedEntities() {
    return m_impl->m_removedEntities;
}


void
ScriptEntityFilter::shutdown() {
    m_impl->setEntityManager(nullptr);
}


