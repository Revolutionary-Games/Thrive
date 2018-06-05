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

constexpr auto CLOUDS_IN_ONE = 4;

/**
 * @brief Compound clouds that flow in the environment
 *
 * Now this has 4 compound types back into it
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
    CompoundCloudComponent(Compound* first,
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

    float
        getGridSize() const
    {
        return gridSize;
    }

    auto
        getPosition() const
    {
        return m_position;
    }

    auto
        getHeight() const
    {
        return height;
    }
    auto
        getWidth() const
    {
        return width;
    }

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
    bool initialized = false;

    //! This is customized with the parameters of this cloud
    //! \todo Check if one material could be used by setting custom parameters
    //! on it
    Ogre::MaterialPtr m_planeMaterial;
    Ogre::TexturePtr m_texture;

    /// The size of the compound cloud grid.
    // These are now initialized here to catch trying to spawn compounds before
    // the cloud is initialized
    size_t width = 0;
    size_t height = 0;
    float gridSize = 2;

    //! The world position this cloud is at. Used to despawn and spawn new ones
    //! Y is ignored and replaced with YOffset
    Float3 m_position;

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

    // The 3x3 grid of density tiles around the player for seamless movement.
    CompoundCloudComponent* leftCloud = nullptr;
    CompoundCloudComponent* rightCloud = nullptr;
    CompoundCloudComponent* lowerCloud = nullptr;
    CompoundCloudComponent* upperCloud = nullptr;

    //! The color of the compound cloud.
    //! Every used channel must have alpha of 1
    Ogre::Vector4 m_color1;
    Ogre::Vector4 m_color2;
    Ogre::Vector4 m_color3;
    Ogre::Vector4 m_color4;

    /**
     * @brief The compound id.
     * @note NULL_COMPOUND means that this cloud doesn't have that slot filled
     */
    CompoundId m_compoundId1 = NULL_COMPOUND;
    CompoundId m_compoundId2 = NULL_COMPOUND;
    CompoundId m_compoundId3 = NULL_COMPOUND;
    CompoundId m_compoundId4 = NULL_COMPOUND;
};



/**
 * @brief Moves the compound clouds.
 */
class CompoundCloudSystem {
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
        addCloud(CompoundId compound, float density, float x, float z);

    //! \param rate should be less than one.
    float
        takeCompound(CompoundId compound, float x, float z, float rate);

    //! \param rate should be less than one.
    float
        amountAvailable(CompoundId compound, float x, float z, float rate);

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

private:
    //! \brief Spawns and despawns the cloud entities around the player
    void
        doSpawnCycle(CellStageWorld& world, const Float3& playerPos);

    void
        _spawnCloud(CellStageWorld& world,
            const Float3& pos,
            size_t startIndex);

    void
        ProcessCloud(CompoundCloudComponent& cloud, int renderTime);

    void
        initializeCloud(CompoundCloudComponent& cloud,
            Ogre::SceneManager* scene);

    void
        fillCloudChannel(const std::vector<std::vector<float>>& density,
            size_t index,
            size_t rowBytes,
            uint8_t* pDest);

private:
    //! This system now spawns these entities when it needs them
    std::unordered_map<ObjectID, CompoundCloudComponent*> m_managedClouds;

    //! List of the types that need to be created. These are split every 4 into
    //! one cloud
    std::vector<Compound> m_cloudTypes;

    Ogre::MeshPtr m_planeMesh;

    PerlinNoise fieldPotential;
    float noiseScale = 5;

    /// The size of the compound cloud grid.
    int width = 120;
    int height = 120;
    //! Should be something like 2, or 0.5 to nicely hit the
    float gridSize = 2;


    //! Used to spawn and despawn compound cloud entities when the player
    //! moves
    Float3 m_lastPosition = Float3(0, 0, 0);

    /// The velocity of the fluid.
    // This is not updated after the initial generation, which isn't probably
    // the best way to simulate fluid velocity
    std::vector<std::vector<float>> xVelocity;
    std::vector<std::vector<float>> yVelocity;

    void
        CreateVelocityField();
    void
        diffuse(float diffRate,
            std::vector<std::vector<float>>& oldDens,
            const std::vector<std::vector<float>>& density,
            int dt);
    void
        advect(const std::vector<std::vector<float>>& oldDens,
            std::vector<std::vector<float>>& density,
            int dt);
};

} // namespace thrive
