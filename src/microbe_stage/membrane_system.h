#pragma once

#include "engine/component_types.h"

#include "Entities/Component.h"
#include "Entities/System.h"

#include "Entities/Components.h"

#include <OgreColourValue.h>
#include <OgreItem.h>

#include <atomic>

namespace thrive {

/**
 * @brief Adds a membrane to an entity
 * @todo To improve performance this has to actually calculate the bounds for
 * frustrum culling to work well
 */
class MembraneComponent : public Leviathan::Component {
    struct MembraneVertex {

        Ogre::Vector3 m_pos;
        Ogre::Vector2 m_uv;
        // Ogre::Vector3 m_normal;
    };

public:
    MembraneComponent();
    ~MembraneComponent();

    void
        Release(Ogre::SceneManager* scene);

    //! Should set the colour of the membrane once working
    void
        setColour(const Float4& value);

    //! Returns the last set colour
    Float4
        getColour() const;

    // Gets organelle positions from the .lua file.
    // The y is actually the z component in the game 3d world
    void
        sendOrganelles(double x, double y);

    //! Deletes the membrane mesh.
    //!
    //! This needs to be called before modifications take effect
    void
        clear();

    // Gets the amount of a certain compound the membrane absorbed.
    int
        getAbsorbedCompounds();

    // Creates the 2D points in the membrane by looking at the positions of the
    // organelles.
    void
        DrawMembrane();

    // Sees if the given point is inside the membrane.
    //! note This is quite an expensive method as this loops all the vertices
    bool
        contains(float x, float y);

    //! \brief Cheaper version of contains for absorbing stuff
    //!
    //! Calculates a circle radius that contains all the points (when it is
    //! placed at 0,0 local coordinate)
    //! \todo Cache this after initialization to increase performance
    float
        calculateEncompassingCircleRadius() const;

    //! \param parentcomponentpos The mesh is attached to this node when the
    //! mesh is created \todo As this is currently only executed once (when
    //! isInitialized is false) this should be changed to directly upload the
    //! fully created data, instead of creating the buffers first and then
    //! filling them with data
    void
        Update(Ogre::SceneManager* scene, Ogre::SceneNode* parentcomponentpos);

    // Returns the length of the bounding membrane "box".
    int
        getCellDimensions()
    {
        return cellDimensions;
    }

    // Adds absorbed compound to the membrane.
    // These are later queried and added to the vacuoles.
    void
        absorbCompounds(int amount);

    // Finds the position of external organelles based on its "internal"
    // location.
    Ogre::Vector3
        GetExternalOrganelle(double x, double y);

    // Return the position of the closest organelle to the target
    // point if it is less then a certain threshold away.
    Ogre::Vector3
        FindClosestOrganelles(Ogre::Vector3 target);

    // Decides where the point needs to move based on the position of the
    // closest organelle.
    Ogre::Vector3
        GetMovement(Ogre::Vector3 target, Ogre::Vector3 closestOrganelle);

    REFERENCE_HANDLE_UNCOUNTED_TYPE(MembraneComponent);

    static constexpr auto TYPE =
        componentTypeConvert(THRIVE_COMPONENT::MEMBRANE);

protected:
    //! Called on first Update
    void
        Initialize();

    //! So it seems that the membrane should be generated just once when the
    //! geometry is changed so when this is true Update does nothing
    bool isInitialized = false;

private:
    // Stores the positions of the organelles.
    std::vector<Ogre::Vector3> organellePositions;

    // The colour of the membrane.
    // still broken
    Ogre::ColourValue colour;

    // The length in pixels of a side of the square that bounds the membrane.
    // Half the side length of the original square that is compressed to make
    // the membrane.
    int cellDimensions = 10;
    // Amount of segments on one side of the above described square.
    // The amount of points on the side of the membrane.
    int membraneResolution = 10;
    // Stores the generated 2-Dimensional membrane.
    std::vector<Ogre::Vector3> vertices2D;

    // Ogre renderable that holds the mesh
    Ogre::MeshPtr m_mesh;
    // The submesh that actually holds our vertex and index buffers
    Ogre::SubMesh* m_subMesh = nullptr;

    //! Actual object that is attached to a scenenode
    Ogre::Item* m_item = nullptr;

    Ogre::VertexBufferPacked* m_vertexBuffer = nullptr;

    //! A material created from the base material that can be colored
    //! \todo It would be better to share this between all cells of a species
    Ogre::MaterialPtr coloredMaterial;
    // Ogre::MaterialPtr speciesMaterial;

    static std::atomic<int> membraneNumber;

    // The amount of compounds stored in the membrane.
    int compoundAmount = 0;
};



/**
 * @brief Handles entities with MembraneComponent
 */
class MembraneSystem
    : public Leviathan::System<
          std::tuple<MembraneComponent&, Leviathan::RenderNode&>> {
public:
    //! Updates the membrane calculations every frame
    void
        Run(GameWorld& world, Ogre::SceneManager* scene)
    {

        auto& index = CachedComponents.GetIndex();
        for(auto iter = index.begin(); iter != index.end(); ++iter) {

            std::get<0>(*iter->second)
                .Update(scene, std::get<1>(*iter->second).Node);
        }
    }

    void
        CreateNodes(const std::vector<std::tuple<MembraneComponent*, ObjectID>>&
                        firstdata,
            const std::vector<std::tuple<Leviathan::RenderNode*, ObjectID>>&
                seconddata,
            const ComponentHolder<MembraneComponent>& firstholder,
            const ComponentHolder<Leviathan::RenderNode>& secondholder)
    {
        TupleCachedComponentCollectionHelper(
            CachedComponents, firstdata, seconddata, firstholder, secondholder);
    }

    void
        DestroyNodes(
            const std::vector<std::tuple<MembraneComponent*, ObjectID>>&
                firstdata,
            const std::vector<std::tuple<Leviathan::RenderNode*, ObjectID>>&
                seconddata)
    {
        CachedComponents.RemoveBasedOnKeyTupleList(firstdata);
        CachedComponents.RemoveBasedOnKeyTupleList(seconddata);
    }
};

} // namespace thrive
