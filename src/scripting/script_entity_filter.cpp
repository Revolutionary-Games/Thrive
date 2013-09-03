#include "scripting/script_entity_filter.h"

#include "engine/component.h"
#include "engine/component_collection.h"
#include "engine/entity_manager.h"
#include "game.h"
#include "scripting/luabind.h"

#include <luabind/iterator_policy.hpp>

using namespace thrive;

struct ScriptEntityFilter::Implementation {

    Implementation(
        EntityManager* entityManager
    ) : m_entityManager(entityManager)
    {
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
        m_addedEntities.clear();
        m_removedEntities.clear();
        m_entities.clear();
        if (m_entityManager) {
            for (EntityId id : m_entityManager->entities()) {
                if (this->isEligible(id)) {
                    this->addEntity(id);
                }
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
    return class_<ScriptEntityFilter>("ScriptEntityFilter")
        .def(constructor<lua_State*>())
        .def("addedEntities", &ScriptEntityFilter::addedEntities, return_stl_iterator)
        .def("clearChanges", &ScriptEntityFilter::clearChanges)
        .def("containsEntity", &ScriptEntityFilter::containsEntity)
        .def("entities", &ScriptEntityFilter::entities, return_stl_iterator)
        .def("removedEntities", &ScriptEntityFilter::removedEntities, return_stl_iterator)
    ;
}


ScriptEntityFilter::ScriptEntityFilter(
    lua_State* L
) : m_impl(new Implementation(&Game::globalEntityManager()))
{
    for (int i = 0; i < lua_gettop(L); ++i) {
        luabind::object obj(luabind::from_stack(L, i));
        luabind::object ret = obj["TYPE_ID"]();
        unsigned int typeId = luabind::object_cast<unsigned int>(ret);
        m_impl->m_requiredComponents.insert(typeId);
    }
    m_impl->initialize();
    m_impl->registerCallbacks();
}


ScriptEntityFilter::~ScriptEntityFilter() {
    m_impl->unregisterCallbacks();
}


std::unordered_set<EntityId>
ScriptEntityFilter::addedEntities() const {
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


std::unordered_set<EntityId>
ScriptEntityFilter::entities() const {
    return m_impl->m_entities;
}


std::unordered_set<EntityId>
ScriptEntityFilter::removedEntities() const {
    return m_impl->m_removedEntities;
}


