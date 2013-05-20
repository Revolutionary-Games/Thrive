#include "ogre/scene_node_system.h"

#include "common/transform.h"
#include "engine/component_registry.h"
#include "engine/entity_filter.h"
#include "ogre/ogre_engine.h"
#include "scripting/luabind.h"

#include <OgreSceneManager.h>

using namespace thrive;

static void
OgreSceneNodeComponent_touch(
    OgreSceneNodeComponent* self
) {
    return self->m_properties.touch();
}


static OgreSceneNodeComponent::Properties&
OgreSceneNodeComponent_getWorkingCopy(
    OgreSceneNodeComponent* self
) {
    return self->m_properties.workingCopy();
}


static const OgreSceneNodeComponent::Properties&
OgreSceneNodeComponent_getLatest(
    OgreSceneNodeComponent* self
) {
    return self->m_properties.latest();
}


luabind::scope
OgreSceneNodeComponent::luaBindings() {
    using namespace luabind;
    return class_<OgreSceneNodeComponent, Component, std::shared_ptr<Component>>("OgreSceneNodeComponent")
        .scope [
            def("TYPE_NAME", &OgreSceneNodeComponent::TYPE_NAME),
            def("TYPE_ID", &OgreSceneNodeComponent::TYPE_ID),
            class_<Properties>("Properties")
                .def_readwrite("orientation", &Properties::orientation)
                .def_readwrite("position", &Properties::position)
                .def_readwrite("scale", &Properties::scale)
        ]
        .def(constructor<>())
        .property("latest", OgreSceneNodeComponent_getLatest)
        .property("workingCopy", OgreSceneNodeComponent_getWorkingCopy)
        .def("touch", OgreSceneNodeComponent_touch)
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
    OgreEngine* ogreEngine = dynamic_cast<OgreEngine*>(engine);
    assert(ogreEngine != nullptr && "System requires an OgreEngine");
    m_impl->m_sceneManager = ogreEngine->sceneManager();
    m_impl->m_entities.setEngine(engine);
}


void
OgreAddSceneNodeSystem::shutdown() {
    m_impl->m_entities.setEngine(nullptr);
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
    m_impl->m_entities.removedEntities().clear();
    added.clear();
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
    OgreEngine* ogreEngine = dynamic_cast<OgreEngine*>(engine);
    assert(ogreEngine != nullptr && "System requires an OgreEngine");
    m_impl->m_sceneManager = ogreEngine->sceneManager();
    m_impl->m_entities.setEngine(engine);
}


void
OgreRemoveSceneNodeSystem::shutdown() {
    m_impl->m_entities.setEngine(nullptr);
    m_impl->m_sceneManager = nullptr;
    System::shutdown();
}


void
OgreRemoveSceneNodeSystem::update(int) {
    auto& removed = m_impl->m_entities.removedEntities();
    for (EntityId entityId : removed) {
        Ogre::SceneNode* node = m_impl->m_sceneNodes[entityId];
        m_impl->m_sceneManager->destroySceneNode(node);
        m_impl->m_sceneNodes.erase(entityId);
    }
    m_impl->m_entities.addedEntities().clear();
    removed.clear();
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
    OgreEngine* ogreEngine = dynamic_cast<OgreEngine*>(engine);
    (void) ogreEngine; // Avoid unused variable warning in release build
    assert(ogreEngine != nullptr && "System requires an OgreEngine");
    m_impl->m_entities.setEngine(engine);
}


void
OgreUpdateSceneNodeSystem::shutdown() {
    m_impl->m_entities.setEngine(nullptr);
    System::shutdown();
}


void
OgreUpdateSceneNodeSystem::update(int) {
    for (const auto& entry : m_impl->m_entities) {
        OgreSceneNodeComponent* sceneNodeComponent = std::get<0>(entry.second);
        if (sceneNodeComponent->m_properties.hasChanges()) {
            Ogre::SceneNode* sceneNode = sceneNodeComponent->m_sceneNode;
            const auto& properties = sceneNodeComponent->m_properties.stable();
            sceneNode->setOrientation(
                properties.orientation
            );
            sceneNode->setPosition(
                properties.position
            );
            sceneNode->setScale(
                properties.scale
            );
            sceneNodeComponent->m_properties.untouch();
        }
    }
}


