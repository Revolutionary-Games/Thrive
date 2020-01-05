#pragma once

#include "membrane_types.h"
#include "engine/component_types.h"
#include "simulation_parameters.h"

#include <Entities/Component.h>
#include <Entities/Components.h>
#include <Entities/System.h>

#include <bsfUtility/Math/BsVector2.h>
#include <bsfUtility/Math/BsVector3.h>

#include <atomic>

namespace thrive {

/**
 * @brief Adds a membrane to an entity
 * @todo To improve performance this has to actually calculate the bounds for
 * frustrum culling to work well
 * @todo All the processing functions from this should be moved to the system.
 */
class MembraneComponent : public Leviathan::Component {
    struct MembraneVertex {

        bs::Vector3 m_pos;
        bs::Vector2 m_uv;
    };

    static_assert(sizeof(MembraneVertex) == 5 * sizeof(float));

public:
    MembraneComponent(MembraneTypeId type);
    virtual ~MembraneComponent();



    // Holder for membrane type id
    MembraneTypeId membraneType;
    const MembraneType* rawMembraneType;

    // This does not take affect without resetting this membrane as only that
    // causes the mesh to actually be re-generated.
    void
        setMembraneType(MembraneTypeId type);

    MembraneTypeId
    getMembraneType();

    void
        Release(bs::Scene* scene);

    //! Should set the colour of the membrane once working
    void
        setColour(const Float4& value);

    //! Should set the health percentage.
    void
        setHealthFraction(float value);

    //! Returns the last set colour
    Float4
        getColour() const;

    // Gets organelle positions from the .lua file.
    // The y is actually the z component in the game 3d world
    void
        sendOrganelles(double x, double y);

    //! Removes previously added organelles. This is the only way to get rid of
    //! them. clear() doesn't clear them
    bool
        removeSentOrganelle(double x, double y);

    //! Deletes the membrane mesh.
    //!
    //! This needs to be called before modifications take effect
    //! \version 0.4.0 Now this only marks this for clearing
    void
        clear();

    // Gets the amount of a certain compound the membrane absorbed.
    int
        getAbsorbedCompounds();

    // Creates the 2D points in the membrane by looking at the positions of the
    // organelles.
    virtual void
        DrawMembrane();

    size_t
        InitializeCorrectMembrane(size_t writeIndex,
            MembraneVertex* meshVertices);

    //! Sees if the given point is inside the membrane.
    //! \note This is quite an expensive method as this loops all the vertices
    bool
        contains(float x, float y);

    //! \brief Cheaper version of contains for absorbing stuff
    //!
    //! Calculates a circle radius that contains all the points (when it is
    //! placed at 0,0 local coordinate)
    float
        calculateEncompassingCircleRadius() const;

    //! \param parentcomponentpos The mesh is attached to this node when the
    //! mesh is created \todo As this is currently only executed once (when
    //! isInitialized is false) this should be changed to directly upload the
    //! fully created data, instead of creating the buffers first and then
    //! filling them with data
    void
        Update(bs::Scene* scene,
            const bs::HSceneObject& parentComponentPos,
            const bs::SPtr<bs::VertexDataDesc>& vertexDesc);

    // Adds absorbed compound to the membrane.
    // These are later queried and added to the vacuoles.
    void
        absorbCompounds(int amount);

    //! Finds the position of external organelles based on its "internal"
    //! location.
    //! \note The returned Vector is in world coordinates (x, 0, z) and not in
    //! internal membrane coordinates (x, y, 0). This is so that gameplay code
    //! doesn't have to do the conversion everywhere this is used
    Float3
        GetExternalOrganelle(double x, double y);

    // Return the position of the closest organelle to the target
    // point if it is less then a certain threshold away.
    Float2
        FindClosestOrganelles(const Float2& target);

    // Decides where the point needs to move based on the position of the
    // closest organelle.
    Float2
        GetMovement(const Float2& target, const Float2& closestOrganelle);

    REFERENCE_HANDLE_UNCOUNTED_TYPE(MembraneComponent);

    static constexpr auto TYPE =
        componentTypeConvert(THRIVE_COMPONENT::MEMBRANE);

    /*
    code for generic things
    */

    bs::HMaterial
        chooseMaterialByType();

    void
        DrawCorrectMembrane();

    // Cell Wall COde
    // Creates the 2D points in the membrane by looking at the positions of the
    // organelles.
    void
        DrawCellWall();

    Float2
        GetMovementForCellWall(const Float2& target,
            const Float2& closestOrganelle);

protected:
    //! Called on first Update
    void
        Initialize();

    void
        releaseCurrentMesh();

    //! When this should be recreated this is true. So that clearing will be
    //! done
    bool clearNeeded = false;

    //! So it seems that the membrane should be generated just once when the
    //! geometry is changed so when this is true Update does nothing
    bool isInitialized = false;
    // Stores the positions of the organelles.
    std::vector<Float2> organellePositions;

    //! The colour of the membrane.
    Float4 colour;

    //! The length in pixels of a side of the square that bounds the membrane.
    //! Half the side length of the original square that is compressed to make
    //! the membrane.
    int cellDimensions = 10;

    //! Amount of segments on one side of the above described square.
    //! The amount of points on the side of the membrane.
    int membraneResolution = 10;

    //! Stores the generated 2-Dimensional membrane.
    std::vector<Float2> vertices2D;

    //! Marks if cached encompassing circleradius is calculated
    mutable bool m_isEncompassingCircleCalculated = false;
    //! Cached circle radius
    mutable float m_encompassingCircleRadius;

    bs::HMesh m_mesh;

    //! Actual object that is attached to a scenenode
    bs::HRenderable m_item;

    //! A material created from the base material that can be colored
    bs::HMaterial coloredMaterial;

    //! The amount of compounds stored in the membrane.
    int compoundAmount = 0;

    // The health percentage of a cell, in the range [0.0, 1.0], used to get
    // damage effects in the membrane.
    float healthFraction = 1.0;

private:
};


/**
 * @brief Handles entities with MembraneComponent
 */
class MembraneSystem
    : public Leviathan::System<
          std::tuple<MembraneComponent&, Leviathan::RenderNode&>> {
    struct Implementation;

public:
    MembraneSystem();
    ~MembraneSystem();

    //! Updates the membrane calculations every frame
    void
        Run(GameWorld& world, bs::Scene* scene)
    {
        auto& index = CachedComponents.GetIndex();
        for(auto iter = index.begin(); iter != index.end(); ++iter) {

            UpdateComponent(std::get<0>(*iter->second), scene,
                std::get<1>(*iter->second).Node);
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

private:
    void
        UpdateComponent(MembraneComponent& component,
            bs::Scene* scene,
            const bs::HSceneObject& parentComponentPos);

private:
    std::unique_ptr<Implementation> m_impl;
};

} // namespace thrive
