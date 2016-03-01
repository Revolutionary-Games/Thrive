#pragma once

#include "engine/component.h"
#include "engine/system.h"
#include "engine/touchable.h"
#include "scripting/luabind.h"
#include "engine/typedefs.h"

#include <luabind/object.hpp>
#include <OgreCommon.h>
#include <OgreColourValue.h>
#include <OgreMath.h>
#include <OgreVector3.h>
#include <vector>
#include <algorithm>


namespace thrive {

class MembraneSystem;
class CompoundCloudSystem;

/**
* @brief Emitter for compound particles
*/
class MembraneComponent : public Component {
    COMPONENT(MembraneComponent)

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - MembraneComponent()
    * - MembraneComponent::m_emissionRadius
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    MembraneComponent();

    void
    load(
        const StorageContainer& storage
    ) override;

    StorageContainer
    storage() const override;

    // The colour of the membrane.
    Ogre::ColourValue colour;

    // Gets organelle positions from the .lua file.
    void sendOrganelles(double x, double y);

    // Sets the colour of the membrane.
    void setColour(float red, float green, float blue, float alpha);

    // Returns the color of the membrane.
    Ogre::Vector3 getColour();

    // Gets the amount of a certain compound the membrane absorbed.
    int getAbsorbedCompounds();

    // Creates the 2D points in the membrane by looking at the positions of the organelles.
	void DrawMembrane();

    // Sees if the given point is inside the membrane.
	bool contains(float x, float y);

	void Initialize();

	void Update();

	// Creates a 3D prism from the 2D vertices.
	void MakePrism();

	// Returns the length of the bounding membrane "box".
	int getCellDimensions() {return cellDimensions;}

	// Adds absorbed compound to the membrane.
	// These are later queried and added to the vacuoles.
	void absorbCompounds(int amount);

    // Finds the position of external organelles based on its "internal" location.
	Ogre::Vector3 GetExternalOrganelle(double x, double y);

	// Return the position of the closest organelle to the target point if it is less then a certain threshold away.
	Ogre::Vector3 FindClosestOrganelles(Ogre::Vector3 target);

	// Decides where the point needs to move based on the position of the closest organelle.
	Ogre::Vector3 GetMovement(Ogre::Vector3 target, Ogre::Vector3 closestOrganelle);


    // Gets the position of the closest membrane point
    luabind::object getExternOrganellePos(double x, double y);

    bool isInitialized;
    bool wantsMembrane;

    	// Finds the UV coordinates be projecting onto a plane and stretching to fit a circle.
	void CalcUVCircle();

	// Finds the normals for the mesh.
	void CalcNormals();

    // Stores the Mesh in a vector such that every 3 points make up a triangle.
    std::vector<Ogre::Vector3> MeshPoints;

    // Stores the UV coordinates for the MeshPoints.
    std::vector<Ogre::Vector3> UVs;

    // Stores the normals for every point described in MeshPoints.
    std::vector<Ogre::Vector3> Normals;


private:
    friend class MembraneSystem;
    friend class CompoundCloudSystem;

    // Stores the positions of the organelles.
    std::vector<Ogre::Vector3> organellePositions;

    // The length in pixels of a side of the square that bounds the membrane.
    int cellDimensions;
    // The amount of points on the side of the membrane.
    int membraneResolution;
    // Stores the generated 2-Dimensional membrane.
    std::vector<Ogre::Vector3>   vertices2D;


    // The amount of compounds stored in the membrane.
    int compoundAmount;
};



/**
* @brief Spawns compound particles for CompoundEmitterComponent
*/
class MembraneSystem : public System {

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - MembraneSystem()
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Constructor
    */
    MembraneSystem();

    /**
    * @brief Destructor
    */
    ~MembraneSystem();

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
    void update(int, int) override;


private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
