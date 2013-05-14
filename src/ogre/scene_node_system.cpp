#include "ogre/scene_node.h"

using namespace thrive;

luabind::scope
OgreSceneNodeComponent::luaBindings() {
    using namespace luabind;
    return class_<OgreSceneNodeComponent, Component, std::shared_ptr<Component>>("SkyPlaneComponent")
        .scope [
            def("TYPE_NAME", &OgreSceneNodeComponent::TYPE_NAME),
            def("TYPE_ID", &OgreSceneNodeComponent::TYPE_ID)
        ]
        .def(constructor<>())
    ;
}

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
    auto& added = m_entities.addedEntities();
    for (const auto& entry : added) {
        EntityId entityId = entry.first;
        OgreSceneNodeComponent* component = std::get<0>(entry.second);
        Ogre::SceneNode* rootNode = m_impl->m_sceneManager->getRootSceneNode();
        Ogre::SceneNode* node = rootNode->createChildSceneNode();
        m_impl->m_sceneNodes[entityId] = node;
        component->m_sceneNode = node;
    }
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
    auto& removed = m_entities.removedEntities();
    for (EntityId entityId : removed) {
        SceneNode* node = m_impl->m_sceneNodes[entityId];
        m_impl->m_sceneManager->destroySceneNode(node);
        m_impl->m_sceneNodes.erase(entityId);
    }
    removed.clear();
}


