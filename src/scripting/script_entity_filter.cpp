#include "scripting/script_entity_filter.h"

#include "engine/component.h"
#include "engine/component_collection.h"
#include "engine/entity_manager.h"
#include "engine/game_state.h"
#include "game.h"
#include "scripting/luabind.h"

#include <luabind/iterator_policy.hpp>

using namespace thrive;

struct ScriptEntityFilter::Implementation {

    Implementation(
        luabind::object componentTypes,
        bool recordChanges
    ) : m_recordChanges(recordChanges)
    {
        using namespace luabind;
        if (luabind::type(componentTypes) != LUA_TTABLE) {
            throw std::runtime_error("ScriptEntityFilter constructor expects a list (table) of component types");
        }
        for (luabind::iterator iter(componentTypes), end; iter != end; ++iter) {
            luabind::object ret = (*iter)["TYPE_ID"];
            ComponentTypeId typeId = luabind::object_cast<ComponentTypeId>(ret);
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


luabind::scope
ScriptEntityFilter::luaBindings() {
    using namespace luabind;
    return class_<ScriptEntityFilter>("EntityFilter")
        .def(constructor<luabind::object>())
        .def(constructor<luabind::object, bool>())
        .def("addedEntities", &ScriptEntityFilter::addedEntities, return_stl_iterator)
        .def("clearChanges", &ScriptEntityFilter::clearChanges)
        .def("containsEntity", &ScriptEntityFilter::containsEntity)
        .def("entities", &ScriptEntityFilter::entities, return_stl_iterator)
        .def("init", &ScriptEntityFilter::init)
        .def("removedEntities", &ScriptEntityFilter::removedEntities, return_stl_iterator)
        .def("shutdown", &ScriptEntityFilter::shutdown)
    ;
}


ScriptEntityFilter::ScriptEntityFilter(
    luabind::object componentTypes,
    bool recordChanges
) : m_impl(new Implementation(componentTypes, recordChanges))
{
}


ScriptEntityFilter::ScriptEntityFilter(
    luabind::object componentTypes
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
    GameState* gameState
) {
    m_impl->setEntityManager(
        &gameState->entityManager()
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


