#pragma once

#include "general/perlin_noise.h"
#include "microbe_stage/compounds.h"

#include "engine/component_types.h"
#include "engine/typedefs.h"

#include <Entities/Component.h>
#include <Entities/System.h>

#include <OgreMesh.h>

#include <vector>


namespace thrive {

class CompoundCloudSystem;
class CellStageWorld;

// Don't touch these without changing the explanation below
constexpr auto CLOUDS_IN_ONE = 4;
constexpr auto CLOUD_WIDTH = 100;
constexpr auto CLOUD_X_EXTENT = CLOUD_WIDTH * 2;
constexpr auto CLOUD_HEIGHT = 100;
// This is cloud local Y not world Y
constexpr auto CLOUD_Y_EXTENT = CLOUD_HEIGHT * 2;
//! This can be freely adjusted to adjust the performance The higher
//! this value is the smaller the size of the simulated cloud is and
//! the performance is better. Don't change this to be higher than 1.
constexpr auto CLOUD_RESOLUTION = 2;
//! The actual vector size is this + 1 because the coordinate
//! CLOUD_SIMULATION_WIDTH is assumed to be valid
constexpr auto CLOUD_SIMULATION_WIDTH =
    static_cast<int>(CLOUD_X_EXTENT / CLOUD_RESOLUTION);
constexpr auto CLOUD_SIMULATION_HEIGHT =
    static_cast<int>(CLOUD_Y_EXTENT / CLOUD_RESOLUTION);

// //! This makes the cloud be behind all the cells
//! Actually this has to be 0 in order for this to match up with mouse
//! coordinates / world coordinates properly. Or the clouds need to be drawn in
//! orthographic mode somehow /* (this is -0.4 to make cells look good and still
//! be pretty accurate with world coordinates) */
constexpr auto CLOUD_Y_COORDINATE = 0;

/*! \page how_compound_clouds_work Description of how the clouds work

The world is split into grid cells sizes of CLOUD_WIDTH x CLOUD_HEIGHT
where width is on the x axis and height is on the y axis.

The cloud entities are dynamically created around the player and there
can be multiple at the same place as each cloud can only have 4 types
of compound in it.

The Y coordinate of clouds is -5 to make them appear behind all cells.

The first cloud entity (at origin) is placed like this:

\todo This shows up really badly in doxygen


UV: 0, 0
-100, 0, 100                        100, 0, -100
    +-----------------+------------------+
    |               0, -100              |
    |                                    |
    |        -20, -40                    |
    |          x                         |
    |                                    |
    |                                    |
    | -100, 0       0, 0           100, 0|
    +                 x                  +
    |          m_cloudPos                |
    |                                    |
    |                                    |
    |                                    |
    |                                    |
    |               0, 100               |
    |                                    |
    +-----------------+------------------+
-100, 0, 100                        100, 0, 100
                                     UV: 1, 1

So the first cloud is at 0, 0 and then the next cloud to the right is
at (CLOUD_WIDTH * 2) 200, 0


World coordinates can be transformed to cloud coordinates by first
dividing both by the size of cloud in that direction (CLOUD_WIDTH,
CLOUD_HEIGHT) For example the point 25, 0, 70 (all future positions
will just show X and Z of the world coordinate):

27 / 100 = 0.25 and 70 / 100 = 0.7 and then floor():ing and casting to
integer to get the index of the cloud you get the grid index.

In the real code for simplicity we do a bounding box check with the
coordinates to determine in which cloud it is (this is slightly slower
but easier to reason about and with only about 20 cloud entities
existing at once the performance difference is negligible.

The bottom and right edges are part of the next cloud over.

This is implemented in \ref CompoundCloudSystem::cloudContainsPosition

Once we have a cloud selected (by selecting one with suitable
component ids matching the coordinates) we can translate the grab or
put operation to local cloud coordinates to perform it.

The cloud is for performance reasons split into less vector elements
than the actual size determined with CLOUD_RESOLUTION in order to have
to simulate less of the cloud cells interacting with each other.

So the local cloud coordinates are between 0-(CLOUD_SIMULATION_WIDTH - 1),
0-(CLOUD_SIMULATION_HEIGHT - 1).

To convert world coordinates into these the following math is used:

topLeftRelative.x = worldPos.x - (m_cloudPos.x - CLOUD_WIDTH)
topLeftRelative.z = worldPos.z - (m_cloudPos.z - CLOUD_HEIGHT)



topLeftRelative must always have x and z >= 0, otherwise the world
point was not within the cloud! and it has to be < CLOUD_WIDTH / CLOUD_HEIGHT


cloudLocal = topLeftRelative / CLOUD_RESOLUTION

cloudLocal must be < CLOUD_SIMULATION_WIDTH otherwise it is out of range.

With this method both putting compounds and getting compounds from a
cloud calculates the right coordinates.

This is implemented in \ref CompoundCloudSystem::convertWorldToCloudLocal

Example:

worldPos = -20, -40

topLeftRelative.x = -20 - (0 - 100) = 80
topLeftRelative.z = -40 - (0 - 100) = 60

These match all the conditions. So the final result is:

cloudLocal.X = 80 / 2 = 40
cloudLocal.Y = 60 / 2 = 30


Look at the tests for the clouds for more examples


When the cloud is rendered the densities from the simulation vector is
transferred to the texture and the top left is UV coordinate 0, 0 and
the bottom right is 1, 1.


The implementation is split into CompoundCloudComponent and CompoundCloudSystem


\todo Compounds should be able to travel between existing cloud
entities because otherwise there will be clumping at the edges of the
entities


*/


/**
 * @brief Compound clouds that flow in the environment
 *
 * Now this has 4 compound types packed into it
 */
class CompoundCloudComponent : public Leviathan::Component {
    friend CompoundCloudSystem;
    friend class CompoundAbsorberSystem;

public:
    enum class SLOT {

        FIRST,
        SECOND,
        THIRD,
        FOURTH
    };

public:
    //! \brief Creates a cloud with the specified compound types and colour
    //!
    //! Set not used ones to null. At least first must be not null
    //! \param owner Needed to report destruction
    CompoundCloudComponent(CompoundCloudSystem& owner,
        Compound* first,
        Compound* second,
        Compound* third,
        Compound* fourth);

    ~CompoundCloudComponent();

    void
        Release(Ogre::SceneManager* scene);

    //! \returns Index for CompoundId or throws if not found
    SLOT
        getSlotForCompound(CompoundId compound);

    //! \returns True if CompoundId is in this cloud
    bool
        handlesCompound(CompoundId compound);

    //! \brief Places specified amount of compound at position (in this cloud's
    //! coordinates)
    //!
    //! The coordinates must be between 0 <= x <= CLOUD_SIMULATION_WIDTH
    void
        addCloud(CompoundId compound, float density, size_t x, size_t y);

    //! Coordinates are in this cloud's coordinate system
    //! \param rate should be less than one.
    int
        takeCompound(CompoundId compound, size_t x, size_t y, float rate);

    //! Coordinates are in this cloud's coordinate system
    //! \param rate should be less than one.
    int
        amountAvailable(CompoundId compound, size_t x, size_t y, float rate);

    //! Used by CompoundCloudSystem::getAllAvailableAt
    void
        getCompoundsAt(size_t x,
            size_t y,
            std::vector<std::tuple<CompoundId, float>>& result);

    CompoundId
        getCompoundId1() const
    {
        return m_compoundId1;
    }
    CompoundId
        getCompoundId2() const
    {
        return m_compoundId2;
    }
    CompoundId
        getCompoundId3() const
    {
        return m_compoundId3;
    }
    CompoundId
        getCompoundId4() const
    {
        return m_compoundId4;
    }

    auto
        getPosition() const
    {
        return m_position;
    }

    //! \brief Moves this cloud to a new position and resets the contents
    void
        recycleToPosition(const Float3& newPosition);


    REFERENCE_HANDLE_UNCOUNTED_TYPE(CompoundCloudComponent);

    //! The name of the texture that is made for this cloud
    const std::string m_textureName;

    static constexpr auto TYPE =
        componentTypeConvert(THRIVE_COMPONENT::COMPOUND_CLOUD);

protected:
    // Now each cloud has it's own plane that it renders onto
    Ogre::Item* m_compoundCloudsPlane = nullptr;
    Ogre::SceneNode* m_sceneNode = nullptr;

    // True once initialized by CompoundCloudSystem
    bool m_initialized = false;

    //! This is customized with the parameters of this cloud
    //! \todo Check if one material could be used by setting custom parameters
    //! on it
    Ogre::MaterialPtr m_planeMaterial;
    Ogre::TexturePtr m_texture;

    //! The world position this cloud is at. Used to despawn and spawn new ones
    //! Y is ignored and replaced with CLOUD_Y_COORDINATE
    Float3 m_position = Float3(0, 0, 0);

    /// The 2D array that contains the current compound clouds and those from
    /// last frame.
    //! \todo switch to a single dimensional vector as this vector of vectors is
    //! not the most efficient
    std::vector<std::vector<float>> m_density1;
    std::vector<std::vector<float>> m_density2;
    std::vector<std::vector<float>> m_density3;
    std::vector<std::vector<float>> m_density4;

    std::vector<std::vector<float>> m_oldDens1;
    std::vector<std::vector<float>> m_oldDens2;
    std::vector<std::vector<float>> m_oldDens3;
    std::vector<std::vector<float>> m_oldDens4;

    //! The 3x3 grid of density tiles around this cloud for moving compounds
    //! between them
    //! \todo This isn't implemented
    CompoundCloudComponent* m_leftCloud = nullptr;
    CompoundCloudComponent* m_rightCloud = nullptr;
    CompoundCloudComponent* m_lowerCloud = nullptr;
    CompoundCloudComponent* m_upperCloud = nullptr;

    //! The color of the compound cloud.
    //! Every used channel must have alpha of 1. The others have alpha 0 so that
    //! they don't need to be worried about affecting the resulting colours
    Ogre::Vector4 m_color1;
    Ogre::Vector4 m_color2;
    Ogre::Vector4 m_color3;
    Ogre::Vector4 m_color4;

    //! \brief The compound id.
    //! \note NULL_COMPOUND means that this cloud doesn't have that slot filled
    CompoundId m_compoundId1 = NULL_COMPOUND;
    CompoundId m_compoundId2 = NULL_COMPOUND;
    CompoundId m_compoundId3 = NULL_COMPOUND;
    CompoundId m_compoundId4 = NULL_COMPOUND;

    //! Used to report destruction
    //! \todo This can be removed once there is a proper clear method available
    //! for systems to detect
    CompoundCloudSystem& m_owner;
};



//! \brief Moves the compound clouds.
//! \see \ref how_compound_clouds_work
class CompoundCloudSystem {
    friend CompoundCloudComponent;

public:
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
     * @param world
     */
    void
        Init(CellStageWorld& world);

    //! \brief Sets the clouds that this system manages
    void
        registerCloudTypes(CellStageWorld& world,
            const std::vector<Compound>& clouds);

    //! \brief Places specified amount of compound at position
    //! \returns True if a cloud at that position was loaded and the cloud has
    //! been placed
    bool
        addCloud(CompoundId compound,
            float density,
            const Float3& worldPosition);

    //! \param rate should be less than one.
    float
        takeCompound(CompoundId compound,
            const Float3& worldPosition,
            float rate);

    //! \param rate should be less than one.
    float
        amountAvailable(CompoundId compound,
            const Float3& worldPosition,
            float rate);

    //! \brief Returns the total amount of all compounds at position
    std::vector<std::tuple<CompoundId, float>>
        getAllAvailableAt(const Float3& worldPosition);

    /**
     * @brief Shuts the system down releasing all current compound cloud
     * entities
     */
    void
        Release(CellStageWorld& world);

    /**
     * @brief Updates the system
     * @todo Is it too rough if the compound clouds only update every 50
     * milliseconds. this needs the support of variable timestep
     */
    void
        Run(CellStageWorld& world);

    //! \brief Returns true when the cloud at cloudPosition contains
    //! worldPosition
    //!
    //! This is used internally to target operations to certain clouds
    static bool
        cloudContainsPosition(const Float3& cloudPosition,
            const Float3& worldPosition);

    //! \brief Returns true if position with radius around it contains any
    //! points that are within this cloud
    static bool
        cloudContainsPositionWithRadius(const Float3& cloudPosition,
            const Float3& worldPosition,
            float radius);

    //! \brief Converts a world position to cloud relative position
    //!
    //! This needs to be used for all operations that want to call the methods
    //! on CompoundCloudComponent as that uses local coordinates
    //! \exception Leviathan::InvalidArgument if the position is out of range
    //! for this cloud
    static std::tuple<size_t, size_t>
        convertWorldToCloudLocal(const Float3& cloudPosition,
            const Float3& worldPosition);

    //! \brief Converts a world position to cloud local. This version has no
    //! bounds checking
    //!
    //! This is used by the compound absorber as the origin point in it can be
    //! outside the cloud but due to the radius it can have points within the
    //! cloud
    static std::tuple<float, float>
        convertWorldToCloudLocalForGrab(const Float3& cloudPosition,
            const Float3& worldPosition);

protected:
    //! \brief Removes deleted clouds from m_managedClouds
    void
        cloudReportDestroyed(CompoundCloudComponent* cloud);

private:
    //! \brief Spawns and despawns the cloud entities around the player
    void
        doSpawnCycle(CellStageWorld& world, const Float3& playerPos);

    void
        _spawnCloud(CellStageWorld& world,
            const Float3& pos,
            size_t startIndex);

    void
        processCloud(CompoundCloudComponent& cloud, int renderTime);

    void
        initializeCloud(CompoundCloudComponent& cloud,
            Ogre::SceneManager* scene);

    void
        fillCloudChannel(const std::vector<std::vector<float>>& density,
            size_t index,
            size_t rowBytes,
            uint8_t* pDest);

    void
        createVelocityField();

    void
        diffuse(float diffRate,
            std::vector<std::vector<float>>& oldDens,
            const std::vector<std::vector<float>>& density,
            int dt);

    void
        advect(const std::vector<std::vector<float>>& oldDens,
            std::vector<std::vector<float>>& density,
            int dt);

private:
    //! This system now spawns these entities when it needs them
    //!
    //! There are always 9 of these entities at once that get positioned around
    //! the player when the player moves around
    //! \note Extra care needs to be taken because this is not updated directly
    //! by the GameWorld when entities are destroyed
    std::unordered_map<ObjectID, CompoundCloudComponent*> m_managedClouds;

    //! This is the point in the center of the middle cloud. This is used for
    //! calculating which clouds to move when the player moves
    Float3 m_cloudGridCenter = Float3(0, 0, 0);

    //! List of the types that need to be created. These are split every 4 into
    //! one cloud
    std::vector<Compound> m_cloudTypes;

    Ogre::MeshPtr m_planeMesh;

    // Shared perlin noise for adding turbulence to the movement of compounds in
    // the clouds
    PerlinNoise m_fieldPotential;
    const float m_noiseScale = 5;

    //! The velocity of the fluid.
    //! This is not updated after the initial generation, which isn't probably
    //! the best way to simulate fluid velocity
    std::vector<std::vector<float>> m_xVelocity;
    std::vector<std::vector<float>> m_yVelocity;
};

} // namespace thrive
