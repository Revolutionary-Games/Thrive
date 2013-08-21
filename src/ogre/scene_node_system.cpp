#include "ogre/scene_node_system.h"

#include "engine/component_registry.h"
#include "engine/engine.h"
#include "engine/entity.h"
#include "engine/entity_filter.h"
#include "engine/entity_manager.h"
#include "scripting/luabind.h"

#include <OgreSceneManager.h>

using namespace thrive;


void
OgreSceneNodeComponent::attachObject(
    Ogre::MovableObject* object
) {
    bool isNew = m_attachedObjects.insert(object).second;
    if (isNew and m_sceneNode) {
        m_sceneNode->attachObject(object);
    }
}


void
OgreSceneNodeComponent::detachObject(
    Ogre::MovableObject* object
) {
    if (m_attachedObjects.erase(object) > 0 and m_sceneNode) {
        m_sceneNode->detachObject(object);
    }
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
        .scope [
            def("TYPE_NAME", &OgreSceneNodeComponent::TYPE_NAME),
            def("TYPE_ID", &OgreSceneNodeComponent::TYPE_ID),
            class_<Transform, Touchable>("Transform")
                .def_readwrite("orientation", &Transform::orientation)
                .def_readwrite("position", &Transform::position)
                .def_readwrite("scale", &Transform::scale)
        ]
        .def(constructor<>())
        .def("attachObject", &OgreSceneNodeComponent::attachObject)
        .def("detachObject", &OgreSceneNodeComponent::detachObject)
        .def_readonly("transform", &OgreSceneNodeComponent::m_transform)
        .property("parent", OgreSceneNodeComponent_getParent, OgreSceneNodeComponent_setParent)
    ;
}

REGISTER_COMPONENT(OgreSceneNodeComponent)

////////////////////////////////////////////////////////////////////////////////
// OgreAddSceneNodeSystem
////////////////////////////////////////////////////////////////////////////////

struct OgreAddSceneNodeSystem::Implementation {

    Ogre::SceneManager* m_sceneManager = nullptr;

    std::unordered_map<EntityId, Ogre::SceneNode*> m_sceneNodes;

    EntityFilter<OgreSceneNodeComponent> m_entities = {true};
};


OgreAddSceneNodeSystem::OgreAddSceneNodeSystem()
  : m_impl(new Implementation())
{
}


OgreAddSceneNodeSystem::~OgreAddSceneNodeSystem() {}


void
OgreAddSceneNodeSystem::init(
    Engine* engine
) {
    System::init(engine);
    assert(m_impl->m_sceneManager == nullptr && "Double init of system");
    m_impl->m_sceneManager = engine->sceneManager();
    m_impl->m_entities.setEntityManager(&engine->entityManager());
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
        EntityId entityId = entry.first;
        OgreSceneNodeComponent* component = std::get<0>(entry.second);
        Ogre::SceneNode* parentNode = nullptr;
        EntityId parentId = component->m_parentId;
        if (parentId == NULL_ENTITY) {
            parentNode = m_impl->m_sceneManager->getRootSceneNode();
        }
        else {
            auto parentComponent = this->engine()->entityManager().getComponent<OgreSceneNodeComponent>(parentId);
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
        m_impl->m_sceneNodes[entityId] = node;
        component->m_sceneNode = node;
        for (Ogre::MovableObject* object : component->m_attachedObjects) {
            node->attachObject(object);
        }
    }
    m_impl->m_entities.clearChanges();
}


////////////////////////////////////////////////////////////////////////////////
// OgreRemoveSceneNodeSystem
////////////////////////////////////////////////////////////////////////////////

struct OgreRemoveSceneNodeSystem::Implementation {

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
    Engine* engine
) {
    System::init(engine);
    assert(m_impl->m_sceneManager == nullptr && "Double init of system");
    m_impl->m_sceneManager = engine->sceneManager();
    m_impl->m_entities.setEntityManager(&engine->entityManager());
}


void
OgreRemoveSceneNodeSystem::shutdown() {
    m_impl->m_entities.setEntityManager(nullptr);
    m_impl->m_sceneManager = nullptr;
    System::shutdown();
}


void
OgreRemoveSceneNodeSystem::update(int) {
    for (auto& value : m_impl->m_entities.addedEntities()) {
        EntityId entityId = value.first;
        OgreSceneNodeComponent* sceneNodeComponent = std::get<0>(value.second);
        Ogre::SceneNode* sceneNode = sceneNodeComponent->m_sceneNode;
        m_impl->m_sceneNodes[entityId] = sceneNode;
    }
    for (EntityId entityId : m_impl->m_entities.removedEntities()) {
        Ogre::SceneNode* node = m_impl->m_sceneNodes[entityId];
        m_impl->m_sceneManager->destroySceneNode(node);
        m_impl->m_sceneNodes.erase(entityId);
    }
    m_impl->m_entities.clearChanges();
}


////////////////////////////////////////////////////////////////////////////////
// OgreUpdateSceneNodeSystem
////////////////////////////////////////////////////////////////////////////////

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
    Engine* engine
) {
    System::init(engine);
    m_impl->m_sceneManager = engine->sceneManager();
    m_impl->m_entities.setEntityManager(&engine->entityManager());
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
            }
            else {
                auto parentComponent = this->engine()->entityManager().getComponent<OgreSceneNodeComponent>(
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
    }
}


