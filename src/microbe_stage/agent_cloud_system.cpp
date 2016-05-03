#include "microbe_stage/agent_cloud_system.h"
#include "microbe_stage/membrane_system.h"

#include "bullet/collision_system.h"
#include "bullet/rigid_body_system.h"
#include "engine/component_factory.h"
#include "engine/engine.h"
#include "engine/entity_filter.h"
#include "engine/entity.h"
#include "engine/game_state.h"
#include "engine/player_data.h"
#include "engine/serialization.h"
#include "game.h"
#include "ogre/scene_node_system.h"
#include "scripting/luabind.h"
#include "util/make_unique.h"

#include <iostream>
#include <errno.h>
#include <stdio.h>
#include <OgreMeshManager.h>
#include <OgreMaterialManager.h>
#include <OgreMaterial.h>
#include <OgreTextureManager.h>
#include <OgreTechnique.h>
#include <OgreRoot.h>
#include <OgreSubMesh.h>

#include <string.h>
#include <cstdio>

#include <chrono>

using namespace thrive;

////////////////////////////////////////////////////////////////////////////////
// AgentCloudComponent
////////////////////////////////////////////////////////////////////////////////

luabind::scope
AgentCloudComponent::luaBindings() {
    using namespace luabind;
    return class_<AgentCloudComponent, Component>("AgentCloudComponent")
        .enum_("ID") [
            value("TYPE_ID", AgentCloudComponent::TYPE_ID)
        ]
        .scope [
            def("TYPE_NAME", &AgentCloudComponent::TYPE_NAME)
        ]
        .def(constructor<>())
        .def("initialize", &AgentCloudComponent::initialize)
        .def_readwrite("direction", &AgentCloudComponent::direction)
        .def_readwrite("potency", &AgentCloudComponent::potency)
        .def_readonly("width", &AgentCloudComponent::width)
        .def_readonly("height", &AgentCloudComponent::height)
        .def_readonly("gridSize", &AgentCloudComponent::gridSize)
    ;
}

void
AgentCloudComponent::initialize(
    CompoundId id,
    float red,
    float green,
    float blue
) {
    m_compoundId = id;
    color = Ogre::ColourValue(red, green, blue);
}

void
AgentCloudComponent::load(
    const StorageContainer& storage
) {
    Component::load(storage);

    m_compoundId = storage.get<CompoundId>("id", NULL_COMPOUND);
    color = storage.get<Ogre::ColourValue>("color", Ogre::ColourValue(0,0,0));
    width = storage.get<int>("width", 0);
    height = storage.get<int>("height", 0);
    gridSize = storage.get<float>("gridSize", 0.0);


}

StorageContainer
AgentCloudComponent::storage() const {
    StorageContainer storage = Component::storage();

    storage.set<CompoundId>("id", m_compoundId);
    storage.set<Ogre::ColourValue>("color", color);
    storage.set<int>("width", width);
    storage.set<int>("height", height);
    storage.set<float>("gridSize", gridSize);

    return storage;
}

float
AgentCloudComponent::getPotency() {

    //if (x >= 0 && x < width && y >= 0 && y < height)
    //{
    //    return static_cast<int>(density[x][y]);
    //}

    return potency;

}

REGISTER_COMPONENT(AgentCloudComponent)


////////////////////////////////////////////////////////////////////////////////
// AgentCloudSystem
////////////////////////////////////////////////////////////////////////////////

luabind::scope
AgentCloudSystem::luaBindings() {
    using namespace luabind;
    return class_<AgentCloudSystem, System>("AgentCloudSystem")
        .def(constructor<>())
    ;
}


struct AgentCloudSystem::Implementation {
    // All entities that have an agent CloudsComponent and a scene node.
    // These should be the various agents/toxins.
    EntityFilter<
        OgreSceneNodeComponent,
        AgentCloudComponent
    > m_compounds = {true};

    Ogre::SceneManager* m_sceneManager = nullptr;
};


AgentCloudSystem::AgentCloudSystem()
  : m_impl(new Implementation())
{
}

AgentCloudSystem::~AgentCloudSystem() {
}


void
AgentCloudSystem::init(
    GameState* gameState
) {
    System::initNamed("AgentCloudSystem", gameState);
    m_impl->m_compounds.setEntityManager(&gameState->entityManager());
    m_impl->m_sceneManager = gameState->sceneManager();
    this->gameState = gameState;
}


void
AgentCloudSystem::shutdown() {
    m_impl->m_compounds.setEntityManager(nullptr);
    m_impl->m_sceneManager = nullptr;
    System::shutdown();
}


void
AgentCloudSystem::update(int dt, int) {

    // For all newly created entities, initialize their parameters.
    //for (auto& value : m_impl->m_compounds.addedEntities()) {
        //OgreSceneNodeComponent* sceneNodeComponent = std::get<0>(value.second);

        // Create a background plane on which the fluid clouds will be drawn.
        //Ogre::Plane plane(Ogre::Vector3::UNIT_Z, -1.0);
        //Ogre::MeshManager::getSingleton().createPlane("AgentCloudPlane", "General", plane, 5, 5, 1, 1, true, 1, 1, 1, Ogre::Vector3::UNIT_Y);
        //sceneNodeComponent->m_meshName = "AgentCloudPlane";

        //Ogre::Entity* thisEntity = m_impl->m_sceneManager->createEntity("agent" + std::to_string(value.first), "General");
        //Ogre::MaterialPtr materialPtr = Ogre::MaterialManager::getSingleton().getByName("Membrane");
        //thisEntity->setMaterialName("Membrane");

        //sceneNodeComponent->m_sceneNode->attachObject(thisEntity);
    //}
    m_impl->m_compounds.clearChanges();
    // For all types of compound clouds...
    for (auto& value : m_impl->m_compounds)
    {
        OgreSceneNodeComponent* sceneNodeComponent = std::get<0>(value.second);
        AgentCloudComponent* agent = std::get<1>(value.second);

        if (agent->potency > 0.5) agent->potency *= .99;
        else this->entityManager()->removeEntity(value.first);

        sceneNodeComponent->m_transform.position += agent->direction*dt/1000;
        sceneNodeComponent->m_transform.scale = agent->potency;
        sceneNodeComponent->m_transform.touch();
    }
}

void
AgentCloudSystem::diffuse(float diffRate, std::vector<  std::vector<float>  >& oldDens, const std::vector<  std::vector<float>  >& density, int dt) {
    dt = 1;
    float a = dt*diffRate;

    for (int x = 1; x < 10-1; x++)
    {
        for (int y = 1; y < 10-1; y++)
        {
            oldDens[x][y] = (density[x][y] + a*(oldDens[x - 1][y] + oldDens[x + 1][y] +
                oldDens[x][y-1] + oldDens[x][y+1])) / (1 + 4 * a);
        }
    }
}
