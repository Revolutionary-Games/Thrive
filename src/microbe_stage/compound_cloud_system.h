#pragma once

#include <vector>
#include <OgreEntity.h>
#include <OgreSceneManager.h>

#include "engine/component.h"
#include "engine/system.h"
#include "engine/touchable.h"
#include "scripting/luabind.h"
#include "engine/typedefs.h"

#include "general/perlin_noise.h"
#include "ogre/scene_node_system.h"

namespace thrive {

class CompoundCloudSystem;

/**
* @brief Compound clouds that flow in the environment
*/
class CompoundCloudComponent : public Component {
    COMPONENT(CompoundCloudComponent)

public:
    /// The size of the compound cloud grid.
	int width, height;
	int offsetX, offsetY;
	float gridSize;

    std::vector<  std::vector<float>  > density;
    std::vector<  std::vector<float>  > oldDens;

public:
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

    void
    addCloud(
        float density,
        int x,
        int y
    );

    /// Rate should be less than one.
    int
    takeCompound(
        int x,
        int y,
        float rate
    );
};



/**
* @brief Moves the compound clouds.
*/
class CompoundCloudSystem : public System {

public:
    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - CompoundCloudSystem()
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Constructor
    */
    CompoundCloudSystem();

    /**
    * @brief Destructor
    */
    ~CompoundCloudSystem();

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
    Ogre::Entity* compoundCloudsPlane;
    OgreSceneNodeComponent* playerNode;

    PerlinNoise fieldPotential;
	float noiseScale;

	/// The size of the compound cloud grid.
	int width, height;
	int offsetX, offsetY;
	float gridSize;

    /// The velocity of the fluid.
	std::vector<  std::vector<float>  > xVelocity;
	std::vector<  std::vector<float>  > yVelocity;

	void CreateVelocityField();
	void diffuse(float diffRate, std::vector<  std::vector<float>  >& oldDens, const std::vector<  std::vector<float>  >& density, int dt);
	void advect(std::vector<  std::vector<float>  >& oldDens, std::vector<  std::vector<float>  >& density, int dt);

	// Draws the density field of the compound to a .bmp, which can then be read by the fragment shader.
	void writeToFile(std::vector<  std::vector<float>  >& density);
};

}
