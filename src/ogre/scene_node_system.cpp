#include "ogre/scene_node_system.h"

#include "engine/component_registry.h"
#include "engine/engine.h"
#include "engine/entity_filter.h"
#include "scripting/luabind.h"

#include <OgreSceneManager.h>

using namespace thrive;

luabind::scope
OgreSceneNodeComponent::luaBindings() {
    using namespace luabind;
    return class_<OgreSceneNodeComponent, Component, std::shared_ptr<Component>>("OgreSceneNodeComponent")
        .scope [
            def("TYPE_NAME", &OgreSceneNodeComponent::TYPE_NAME),
            def("TYPE_ID", &OgreSceneNodeComponent::TYPE_ID),
            class_<Properties, Touchable>("Properties")
                .def_readwrite("orientation", &Properties::orientation)
                .def_readwrite("position", &Properties::position)
                .def_readwrite("scale", &Properties::scale)
        ]
        .def(constructor<>())
        .def_readonly("properties", &OgreSceneNodeComponent::m_properties)
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
        Ogre::SceneNode* rootNode = m_impl->m_sceneManager->getRootSceneNode();
        Ogre::SceneNode* node = rootNode->createChildSceneNode();
        m_impl->m_sceneNodes[entityId] = node;
        component->m_sceneNode = node;
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
    m_impl->m_entities.setEntityManager(&engine->entityManager());
}


void
OgreUpdateSceneNodeSystem::shutdown() {
    m_impl->m_entities.setEntityManager(nullptr);
    System::shutdown();
}


void
OgreUpdateSceneNodeSystem::update(int) {
    for (const auto& entry : m_impl->m_entities) {
        OgreSceneNodeComponent* sceneNodeComponent = std::get<0>(entry.second);
        auto& properties = sceneNodeComponent->m_properties;
        if (properties.hasChanges()) {
            Ogre::SceneNode* sceneNode = sceneNodeComponent->m_sceneNode;
            sceneNode->setOrientation(
                properties.orientation
            );
            sceneNode->setPosition(
                properties.position
            );
            sceneNode->setScale(
                properties.scale
            );
            properties.untouch();
        }
    }
}


