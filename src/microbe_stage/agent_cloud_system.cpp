#include "microbe_stage/agent_cloud_system.h"
// #include "microbe_stage/membrane_system.h"

// #include "bullet/collision_system.h"
// #include "bullet/rigid_body_system.h"
// #include "engine/component_factory.h"
// #include "engine/engine.h"
// #include "engine/entity_filter.h"
// #include "engine/entity.h"
// #include "engine/game_state.h"
// #include "engine/player_data.h"
// #include "engine/serialization.h"
// #include "game.h"
// #include "ogre/scene_node_system.h"
// #include "scripting/luajit.h"
// #include "util/make_unique.h"

// #include <iostream>
// #include <errno.h>
// #include <stdio.h>
// #include <OgreMeshManager.h>
// #include <OgreMaterialManager.h>
// #include <OgreMaterial.h>
// #include <OgreTextureManager.h>
// #include <OgreTechnique.h>
// #include <OgreRoot.h>
// #include <OgreSubMesh.h>

// #include <string.h>
// #include <cstdio>

// #include <chrono>

#include <Entities/GameWorld.h>

using namespace thrive;

////////////////////////////////////////////////////////////////////////////////
// AgentCloudComponent
////////////////////////////////////////////////////////////////////////////////

// void AgentCloudComponent::luaBindings(
//     sol::state &lua
// ){
//     lua.new_usertype<AgentCloudComponent>("AgentCloudComponent",

//         "new", sol::factories([](){
//                 return std::make_unique<AgentCloudComponent>();
//             }),

//         COMPONENT_BINDINGS(AgentCloudComponent),

//         "initialize", &AgentCloudComponent::initialize,
//         "direction", &AgentCloudComponent::direction,
//         "potency", &AgentCloudComponent::potency,
//         "width", sol::readonly(&AgentCloudComponent::width),
//         "height", sol::readonly(&AgentCloudComponent::height),
//         "gridSize", sol::readonly(&AgentCloudComponent::gridSize)
//     );
// }

AgentCloudComponent::AgentCloudComponent(CompoundId id,
    float red,
    float green,
    float blue) :
    Leviathan::Component(TYPE),
    color(red, green, blue, 1.f), m_compoundId(id)
{}

// void
// AgentCloudComponent::load(
//     const StorageContainer& storage
// ) {
//     Component::load(storage);

//     m_compoundId = storage.get<CompoundId>("id", NULL_COMPOUND);
//     color = storage.get<Ogre::ColourValue>("color",
//     Ogre::ColourValue(0,0,0)); width = storage.get<int>("width", 0); height =
//     storage.get<int>("height", 0); gridSize = storage.get<float>("gridSize",
//     0.0);


// }

// StorageContainer
// AgentCloudComponent::storage() const {
//     StorageContainer storage = Component::storage();

//     storage.set<CompoundId>("id", m_compoundId);
//     storage.set<Ogre::ColourValue>("color", color);
//     storage.set<int>("width", width);
//     storage.set<int>("height", height);
//     storage.set<float>("gridSize", gridSize);

//     return storage;
// }

float
    AgentCloudComponent::getPotency()
{

    // if (x >= 0 && x < width && y >= 0 && y < height)
    //{
    //    return static_cast<int>(density[x][y]);
    //}

    return potency;
}

////////////////////////////////////////////////////////////////////////////////
// AgentCloudSystem
////////////////////////////////////////////////////////////////////////////////

void
    AgentCloudSystem::Run(GameWorld& world)
{
    // const int dt = Leviathan::TICKSPEED;

    auto& index = CachedComponents.GetIndex();
    for(auto iter = index.begin(); iter != index.end(); ++iter) {

        // This does not work currently, don't do anything with this
        DEBUG_BREAK;

        // Leviathan::Position& position = std::get<0>(*iter->second);
        // AgentCloudComponent& agent = std::get<1>(*iter->second);
        // Leviathan::RenderNode& renderNode = std::get<2>(*iter->second);


        // if(agent.potency > 0.5) {
        //     agent.potency *= .99;
        // } else {
        //     world.DestroyEntity(iter->first);
        //     continue;
        // }

        // position.Members._Position += agent.direction * (dt / 1000.f);
        // position.Marked = true;

        // renderNode.Scale = Float3(agent.potency);
        // renderNode.Marked = true;
    }
}
