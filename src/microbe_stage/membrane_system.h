#pragma once

#include "engine/component_types.h"

#include "Entities/Component.h"

#include <OgreColourValue.h>
#include <OgreItem.h>

namespace thrive {

/**
* @brief Adds a membrane to an entity
*/
class MembraneComponent : public Leviathan::Component {
    struct MembraneVertex{

        Ogre::Vector3 m_pos;
        Ogre::Vector2 m_uv;
        //Ogre::Vector3 m_normal;
    };
public:

    MembraneComponent(Ogre::SceneManager* scene);
    ~MembraneComponent();

    void Release(Ogre::SceneManager* scene);

    // The colour of the membrane.
    // still broken
    Ogre::ColourValue colour;

    // Gets organelle positions from the .lua file.
    void sendOrganelles(double x, double y);

    // Deletes the membrane mesh.
    void clear();

    // Gets the amount of a certain compound the membrane absorbed.
    int getAbsorbedCompounds();

    // Creates the 2D points in the membrane by looking at the positions of the organelles.
	void DrawMembrane();

    // Sees if the given point is inside the membrane.
	bool contains(float x, float y);

	void Update();

	// Returns the length of the bounding membrane "box".
	int getCellDimensions() {return cellDimensions;}

	// Adds absorbed compound to the membrane.
	// These are later queried and added to the vacuoles.
	void absorbCompounds(int amount);

    // Finds the position of external organelles based on its "internal" location.
	Ogre::Vector3 GetExternalOrganelle(double x, double y);

	// Return the position of the closest organelle to the target
	// point if it is less then a certain threshold away.
	Ogre::Vector3 FindClosestOrganelles(Ogre::Vector3 target);

	// Decides where the point needs to move based on the position of the closest organelle.
	Ogre::Vector3 GetMovement(Ogre::Vector3 target, Ogre::Vector3 closestOrganelle);

    //! Makes the model move with the scene node
    void attach(Ogre::SceneNode* node);

    static constexpr auto TYPE = componentTypeConvert(THRIVE_COMPONENT::MEMBRANE);
    
protected:
    
    //! Called on first Update
    void Initialize();

    bool isInitialized = false;
    
private:

    // Stores the positions of the organelles.
    std::vector<Ogre::Vector3> organellePositions;

    // The length in pixels of a side of the square that bounds the membrane.
    // Half the side length of the original square that is compressed to make the membrane.
    int cellDimensions = 10;
    // Amount of segments on one side of the above described square.
    // The amount of points on the side of the membrane.
    int membraneResolution = 10;
    // Stores the generated 2-Dimensional membrane.
    std::vector<Ogre::Vector3> vertices2D;

    // Ogre renderable that holds the mesh
    Ogre::MeshPtr m_mesh;
    // The submesh that actually holds our vertex and index buffers
    Ogre::SubMesh* m_subMesh;

    //! Actual object that is attached to a scenenode
    Ogre::Item* m_item;

    Ogre::VertexBufferPacked* m_vertexBuffer = nullptr;

    // The amount of compounds stored in the membrane.
    int compoundAmount = 0;
};



/**
* @brief Handles entities with MembraneComponent
*/
class MembraneSystem{
public:

    //! Updates the membrane calculations every frame
    void Run(GameWorld &world,
        std::unordered_map<Leviathan::ObjectID, MembraneComponent*> &index)
    {
        for(auto iter = index.begin(); iter != index.end(); ++iter){

            auto& node = *iter->second;
            
            node.Update();
        }
    }
};

}
