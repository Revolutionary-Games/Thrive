#include "ogre/scene_node_system.h"

#include "engine/component_factory.h"
#include "engine/entity.h"
#include "engine/entity_filter.h"
#include "engine/entity_manager.h"
#include "engine/game_state.h"
#include "engine/serialization.h"
#include "scripting/luabind.h"

#include <OgreSceneManager.h>
#include <OgreEntity.h>

using namespace thrive;


static Ogre::String
OgreSceneNodeComponent_getMeshName(
    const OgreSceneNodeComponent* self
) {
    return self->m_meshName.get();
}


static void
OgreSceneNodeComponent_setMeshName(
    OgreSceneNodeComponent* self,
    const Ogre::String& meshName
) {
    self->m_meshName = meshName;
}


static Entity
OgreSceneNodeComponent_getParent(
    const OgreSceneNodeComponent* self
) {
    return Entity(self->m_parentId.get());
}


static void
OgreSceneNodeComponent_setParent(
    OgreSceneNodeComponent* self,
    const Entity& entity
) {
    self->m_parentId = entity.id();
    self->m_parentId.touch();
}


luabind::scope
OgreSceneNodeComponent::luaBindings() {
    using namespace luabind;
    return class_<OgreSceneNodeComponent, Component>("OgreSceneNodeComponent")
        .enum_("ID") [
            value("TYPE_ID", OgreSceneNodeComponent::TYPE_ID)
        ]
        .scope [
            def("TYPE_NAME", &OgreSceneNodeComponent::TYPE_NAME),
            class_<Transform, Touchable>("Transform")
                .def_readwrite("orientation", &Transform::orientation)
                .def_readwrite("position", &Transform::position)
                .def_readwrite("scale", &Transform::scale)
        ]
        .def(constructor<>())
        .def_readonly("transform", &OgreSceneNodeComponent::m_transform)
        .def_readonly("entity", &OgreSceneNodeComponent::m_entity)
        .property("parent", OgreSceneNodeComponent_getParent, OgreSceneNodeComponent_setParent)
        .property("meshName", OgreSceneNodeComponent_getMeshName, OgreSceneNodeComponent_setMeshName)
    ;
}


void
OgreSceneNodeComponent::load(
    const StorageContainer& storage
) {
    Component::load(storage);
    m_transform.orientation = storage.get<Ogre::Quaternion>("orientation", Ogre::Quaternion::IDENTITY);
    m_transform.position = storage.get<Ogre::Vector3>("position", Ogre::Vector3(0,0,0));
    m_transform.scale = storage.get<Ogre::Vector3>("scale", Ogre::Vector3(1,1,1));
    m_meshName = storage.get<Ogre::String>("meshName");
    m_parentId = storage.get<EntityId>("parentId", NULL_ENTITY);
}


StorageContainer
OgreSceneNodeComponent::storage() const {
    StorageContainer storage = Component::storage();
    storage.set<Ogre::Quaternion>("orientation", m_transform.orientation);
    storage.set<Ogre::Vector3>("position", m_transform.position);
    storage.set<Ogre::Vector3>("scale", m_transform.scale);
    storage.set<Ogre::String>("meshName", m_meshName);
    storage.set<EntityId>("parentId", m_parentId);
    return storage;
}

REGISTER_COMPONENT(OgreSceneNodeComponent)

////////////////////////////////////////////////////////////////////////////////
// OgreAddSceneNodeSystem
////////////////////////////////////////////////////////////////////////////////

luabind::scope
OgreAddSceneNodeSystem::luaBindings() {
    using namespace luabind;
    return class_<OgreAddSceneNodeSystem, System>("OgreAddSceneNodeSystem")
        .def(constructor<>())
    ;
}


struct OgreAddSceneNodeSystem::Implementation {

    Ogre::SceneManager* m_sceneManager = nullptr;

    EntityFilter<OgreSceneNodeComponent> m_entities = {true};
};


OgreAddSceneNodeSystem::OgreAddSceneNodeSystem()
  : m_impl(new Implementation())
{
}


OgreAddSceneNodeSystem::~OgreAddSceneNodeSystem() {}


void
OgreAddSceneNodeSystem::init(
    GameState* gameState
) {
    System::init(gameState);
    assert(m_impl->m_sceneManager == nullptr && "Double init of system");
    m_impl->m_sceneManager = gameState->sceneManager();
    m_impl->m_entities.setEntityManager(&gameState->entityManager());
}


void
OgreAddSceneNodeSystem::shutdown() {
    m_impl->m_entities.setEntityManager(nullptr);
    m_impl->m_sceneManager = nullptr;
    System::shutdown();
}


void
OgreAddSceneNodeSystem::update(int) {
    auto& added = m_impl->m_entities.addedEntities();
    for (const auto& entry : added) {
        OgreSceneNodeComponent* component = std::get<0>(entry.second);
        Ogre::SceneNode* parentNode = nullptr;
        EntityId parentId = component->m_parentId;
        if (parentId == NULL_ENTITY) {
            parentNode = m_impl->m_sceneManager->getRootSceneNode();
            component->m_parentId.untouch();
        }
        else {
            auto parentComponent = this->entityManager()->getComponent<OgreSceneNodeComponent>(parentId);
            if (parentComponent and parentComponent->m_sceneNode) {
                parentNode = parentComponent->m_sceneNode;
                component->m_parentId.untouch();
            }
            else {
                parentNode = m_impl->m_sceneManager->getRootSceneNode();
                // Mark component for later reparenting
                component->m_parentId.touch();
            }
        }
        Ogre::SceneNode* node = parentNode->createChildSceneNode();
        component->m_sceneNode = node;
    }
    m_impl->m_entities.clearChanges();
}


////////////////////////////////////////////////////////////////////////////////
// OgreRemoveSceneNodeSystem
////////////////////////////////////////////////////////////////////////////////

luabind::scope
OgreRemoveSceneNodeSystem::luaBindings() {
    using namespace luabind;
    return class_<OgreRemoveSceneNodeSystem, System>("OgreRemoveSceneNodeSystem")
        .def(constructor<>())
    ;
}


struct OgreRemoveSceneNodeSystem::Implementation {

    std::unordered_map<EntityId, Ogre::Entity*> m_ogreEntities;

    Ogre::SceneManager* m_sceneManager = nullptr;

    std::unordered_map<EntityId, Ogre::SceneNode*> m_sceneNodes;

    EntityFilter<OgreSceneNodeComponent> m_entities = {true};
};


OgreRemoveSceneNodeSystem::OgreRemoveSceneNodeSystem()
  : m_impl(new Implementation())
{
}


OgreRemoveSceneNodeSystem::~OgreRemoveSceneNodeSystem() {}


void
OgreRemoveSceneNodeSystem::init(
    GameState* gameState
) {
    System::init(gameState);
    assert(m_impl->m_sceneManager == nullptr && "Double init of system");
    m_impl->m_sceneManager = gameState->sceneManager();
    m_impl->m_entities.setEntityManager(&gameState->entityManager());
}


void
OgreRemoveSceneNodeSystem::shutdown() {
    m_impl->m_entities.setEntityManager(nullptr);
    m_impl->m_sceneManager = nullptr;
    System::shutdown();
}


void
OgreRemoveSceneNodeSystem::update(int) {
    for (EntityId entityId : m_impl->m_entities.removedEntities()) {
        // Scene node
        Ogre::SceneNode* node = m_impl->m_sceneNodes[entityId];
        if (node) {
            node->detachAllObjects();
            m_impl->m_sceneManager->destroySceneNode(node);
        }
        m_impl->m_sceneNodes.erase(entityId);
        // Ogre Entity
        Ogre::Entity* entity = m_impl->m_ogreEntities[entityId];
        if (entity) {
            m_impl->m_sceneManager->destroyEntity(entity);
        }
        m_impl->m_ogreEntities.erase(entityId);
    }
    for (auto& value : m_impl->m_entities.addedEntities()) {
        EntityId entityId = value.first;
        OgreSceneNodeComponent* sceneNodeComponent = std::get<0>(value.second);
        m_impl->m_ogreEntities[entityId] = sceneNodeComponent->m_entity;
        m_impl->m_sceneNodes[entityId] = sceneNodeComponent->m_sceneNode;
    }
    m_impl->m_entities.clearChanges();
}


////////////////////////////////////////////////////////////////////////////////
// OgreUpdateSceneNodeSystem
////////////////////////////////////////////////////////////////////////////////

luabind::scope
OgreUpdateSceneNodeSystem::luaBindings() {
    using namespace luabind;
    return class_<OgreUpdateSceneNodeSystem, System>("OgreUpdateSceneNodeSystem")
        .def(constructor<>())
    ;
}


struct OgreUpdateSceneNodeSystem::Implementation {

    EntityFilter<
        OgreSceneNodeComponent
    > m_entities;

    Ogre::SceneManager* m_sceneManager = nullptr;

};


OgreUpdateSceneNodeSystem::OgreUpdateSceneNodeSystem()
  : m_impl(new Implementation())
{
}


OgreUpdateSceneNodeSystem::~OgreUpdateSceneNodeSystem() {}


void
OgreUpdateSceneNodeSystem::init(
    GameState* gameState
) {
    System::init(gameState);
    m_impl->m_sceneManager = gameState->sceneManager();
    m_impl->m_entities.setEntityManager(&gameState->entityManager());
}


void
OgreUpdateSceneNodeSystem::shutdown() {
    m_impl->m_entities.setEntityManager(nullptr);
    m_impl->m_sceneManager = nullptr;
    System::shutdown();
}


void
OgreUpdateSceneNodeSystem::update(int) {
    for (const auto& entry : m_impl->m_entities) {
        OgreSceneNodeComponent* component = std::get<0>(entry.second);
        Ogre::SceneNode* sceneNode = component->m_sceneNode;
        auto& transform = component->m_transform;
        if (transform.hasChanges()) {
            sceneNode->setOrientation(
                transform.orientation
            );
            sceneNode->setPosition(
                transform.position
            );
            sceneNode->setScale(
                transform.scale
            );
            transform.untouch();
        }
        if (component->m_parentId.hasChanges()) {
            EntityId parentId = component->m_parentId;
            Ogre::SceneNode* newParentNode = nullptr;
            if (parentId == NULL_ENTITY) {
                newParentNode = m_impl->m_sceneManager->getRootSceneNode();
                component->m_parentId.untouch();
            }
            else {
                auto parentComponent = this->entityManager()->getComponent<OgreSceneNodeComponent>(
                    parentId
                );
                if (parentComponent and parentComponent->m_sceneNode) {
                    newParentNode = parentComponent->m_sceneNode;
                    component->m_parentId.untouch();
                }
                else {
                    newParentNode = m_impl->m_sceneManager->getRootSceneNode();
                    // Mark component for later reparenting
                    component->m_parentId.touch();
                }
            }
            Ogre::SceneNode* currentParentNode = sceneNode->getParentSceneNode();
            currentParentNode->removeChild(sceneNode);
            newParentNode->addChild(sceneNode);
        }
        if (component->m_meshName.hasChanges()) {
            if (component->m_entity) {
                sceneNode->detachObject(component->m_entity);
                m_impl->m_sceneManager->destroyEntity(component->m_entity);
                component->m_entity = nullptr;
            }
            if (component->m_meshName.get().size() > 0) {
                component->m_entity = m_impl->m_sceneManager->createEntity(
                    component->m_meshName
                );
                sceneNode->attachObject(component->m_entity);
            }
            component->m_meshName.untouch();
        }
    }
}


