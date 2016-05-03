#pragma once

#include <vector>
#include <OgreEntity.h>
#include <OgreSceneManager.h>
#include "OgreHardwarePixelBuffer.h"

#include "engine/component.h"
#include "engine/system.h"
#include "engine/touchable.h"
#include "scripting/luabind.h"
#include "engine/typedefs.h"

#include "general/perlin_noise.h"
#include "ogre/scene_node_system.h"
#include "microbe_stage/compound_registry.h"

namespace thrive {

class AgentCloudSystem;

/**
* @brief Agents clouds that flow in the environment.
*/
class AgentCloudComponent : public Component {
    COMPONENT(AgentCloudComponent)

public:
    /// The size of the compound cloud grid.
	int width, height;
	float gridSize;

	Ogre::Vector3 direction;

	float potency;

    /// The 2D array that contains the current compound clouds and those from last frame.
    std::vector<  std::vector<float>  > density;
    std::vector<  std::vector<float>  > oldDens;

    /// The color of the compound cloud.
    Ogre::ColourValue color;

    /**
    * @brief The compound id.
    */
    CompoundId m_compoundId = NULL_COMPOUND;

public:

    void initialize(CompoundId Id, float red, float green, float blue);

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - CompoundCloudComponent()
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    void
    load(
        const StorageContainer& storage
    ) override;

    StorageContainer
    storage() const override;

    /// Rate should be less than one.
    float
    getPotency();
};



/**
* @brief Moves the compound clouds.
*/
class AgentCloudSystem : public System {

public:
    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - AgentCloudSystem()
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Constructor
    */
    AgentCloudSystem();

    /**
    * @brief Destructor
    */
    ~AgentCloudSystem();

    /**
    * @brief Initializes the system
    *
    * @param gameState
    */
    void init(GameState* gameState) override;

    /**
    * @brief Shuts the system down
    */
    void shutdown() override;

    /**
    * @brief Updates the system
    */
    void update(int renderTime, int logicTime) override;

private:
    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
    GameState* gameState;

	void diffuse(float diffRate, std::vector<  std::vector<float>  >& oldDens, const std::vector<  std::vector<float>  >& density, int dt);
};

}
