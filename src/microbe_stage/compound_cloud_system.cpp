#include "microbe_stage/compound_cloud_system.h"
#include "microbe_stage/simulation_parameters.h"

#include "ThriveGame.h"

#include "engine/player_data.h"
#include "generated/cell_stage_world.h"

#include <Rendering/GeometryHelpers.h>

#include <OgreHardwarePixelBuffer.h>
#include <OgreMaterialManager.h>
#include <OgreMesh2.h>
#include <OgreMeshManager.h>
#include <OgreMeshManager2.h>
#include <OgrePlane.h>
#include <OgreRoot.h>
#include <OgreSceneManager.h>
#include <OgreTechnique.h>
#include <OgreTextureManager.h>

#include <atomic>

using namespace thrive;

constexpr auto OGRE_CLOUD_TEXTURE_BYTES_PER_ELEMENT = 4;

static std::atomic<int> CloudTextureNumber = {0};
static std::atomic<int> CloudMeshNumberCounter = {0};

////////////////////////////////////////////////////////////////////////////////
// CompoundCloudComponent
////////////////////////////////////////////////////////////////////////////////
CompoundCloudComponent::CompoundCloudComponent(CompoundCloudSystem& owner,
    Compound* first,
    Compound* second,
    Compound* third,
    Compound* fourth) :
    Leviathan::Component(TYPE),
    m_textureName("cloud_" + std::to_string(++CloudTextureNumber)),
    m_owner(owner)
{
    if(!first)
        throw std::runtime_error(
            "CompoundCloudComponent needs at least one Compound type");

    // Read data
    // Redundant check (see the throw above)
    if(first) {

        m_compoundId1 = first->id;
        m_color1 =
            Ogre::Vector4(first->colour.r, first->colour.g, first->colour.b, 1);
    }

    if(second) {

        m_compoundId2 = second->id;
        m_color2 = Ogre::Vector4(
            second->colour.r, second->colour.g, second->colour.b, 1);
    }

    if(third) {

        m_compoundId3 = third->id;
        m_color3 =
            Ogre::Vector4(third->colour.r, third->colour.g, third->colour.b, 1);
    }

    if(fourth) {

        m_compoundId4 = fourth->id;
        m_color4 = Ogre::Vector4(
            fourth->colour.r, fourth->colour.g, fourth->colour.b, 1);
    }
}

CompoundCloudComponent::~CompoundCloudComponent()
{
    LEVIATHAN_ASSERT(!m_compoundCloudsPlane && !m_sceneNode,
        "CompoundCloudComponent not Released");

    m_owner.cloudReportDestroyed(this);
}

void
    CompoundCloudComponent::Release(Ogre::SceneManager* scene)
{
    // Destroy the plane
    if(m_compoundCloudsPlane) {
        scene->destroyItem(m_compoundCloudsPlane);
        m_compoundCloudsPlane = nullptr;
    }

    // Scenenode
    if(m_sceneNode) {
        scene->destroySceneNode(m_sceneNode);
        m_sceneNode = nullptr;
    }

    if(m_initialized) {

        m_initialized = false;
    }

    // And material
    if(m_planeMaterial) {

        Ogre::MaterialManager::getSingleton().remove(m_planeMaterial);
        m_planeMaterial.reset();
    }

    // Texture
    if(m_texture) {
        Ogre::TextureManager::getSingleton().remove(m_texture);
        m_texture.reset();
    }
}

// ------------------------------------ //
CompoundCloudComponent::SLOT
    CompoundCloudComponent::getSlotForCompound(CompoundId compound)
{
    if(compound == m_compoundId1)
        return SLOT::FIRST;
    if(compound == m_compoundId2)
        return SLOT::SECOND;
    if(compound == m_compoundId3)
        return SLOT::THIRD;
    if(compound == m_compoundId4)
        return SLOT::FOURTH;

    throw std::runtime_error("This cloud doesn't contain the used CompoundId");
}

bool
    CompoundCloudComponent::handlesCompound(CompoundId compound)
{
    if(compound == m_compoundId1)
        return true;
    if(compound == m_compoundId2)
        return true;
    if(compound == m_compoundId3)
        return true;
    if(compound == m_compoundId4)
        return true;
    return false;
}
// ------------------------------------ //
void
    CompoundCloudComponent::addCloud(CompoundId compound,
        float dens,
        size_t x,
        size_t y)
{
    // TODO: this check isn't even very good so it can be removed once this is
    // debugged
    if(x >= m_density1.size() || y >= m_density1[0].size())
        throw std::runtime_error(
            "CompoundCloudComponent coordinates out of range");

    switch(getSlotForCompound(compound)) {
    case SLOT::FIRST: m_density1[x][y] += dens; break;
    case SLOT::SECOND: m_density2[x][y] += dens; break;
    case SLOT::THIRD: m_density3[x][y] += dens; break;
    case SLOT::FOURTH: m_density4[x][y] += dens; break;
    }
}

int
    CompoundCloudComponent::takeCompound(CompoundId compound,
        size_t x,
        size_t y,
        float rate)
{
    switch(getSlotForCompound(compound)) {
    case SLOT::FIRST: {
        int amountToGive = static_cast<int>(m_density1[x][y] * rate);
        m_density1[x][y] -= amountToGive;
        if(m_density1[x][y] < 1)
            m_density1[x][y] = 0;

        return amountToGive;
    }
    case SLOT::SECOND: {
        int amountToGive = static_cast<int>(m_density2[x][y] * rate);
        m_density2[x][y] -= amountToGive;
        if(m_density2[x][y] < 1)
            m_density2[x][y] = 0;

        return amountToGive;
    }
    case SLOT::THIRD: {
        int amountToGive = static_cast<int>(m_density3[x][y] * rate);
        m_density3[x][y] -= amountToGive;
        if(m_density3[x][y] < 1)
            m_density3[x][y] = 0;

        return amountToGive;
    }
    case SLOT::FOURTH: {
        int amountToGive = static_cast<int>(m_density4[x][y] * rate);
        m_density4[x][y] -= amountToGive;
        if(m_density4[x][y] < 1)
            m_density4[x][y] = 0;

        return amountToGive;
    }
    }

    LEVIATHAN_ASSERT(false, "Shouldn't get here");
    return -1;
}

int
    CompoundCloudComponent::amountAvailable(CompoundId compound,
        size_t x,
        size_t y,
        float rate)
{
    switch(getSlotForCompound(compound)) {
    case SLOT::FIRST: {
        int amountToGive = static_cast<int>(m_density1[x][y] * rate);
        return amountToGive;
    }
    case SLOT::SECOND: {
        int amountToGive = static_cast<int>(m_density2[x][y] * rate);
        return amountToGive;
    }
    case SLOT::THIRD: {
        int amountToGive = static_cast<int>(m_density3[x][y] * rate);
        return amountToGive;
    }
    case SLOT::FOURTH: {
        int amountToGive = static_cast<int>(m_density4[x][y] * rate);
        return amountToGive;
    }
    }

    LEVIATHAN_ASSERT(false, "Shouldn't get here");
    return -1;
}

void
    CompoundCloudComponent::getCompoundsAt(size_t x,
        size_t y,
        std::vector<std::tuple<CompoundId, float>>& result)
{
    if(m_compoundId1 != NULL_COMPOUND) {
        const auto amount = m_density1[x][y];
        if(amount > 0)
            result.emplace_back(m_compoundId1, amount);
    }

    if(m_compoundId2 != NULL_COMPOUND) {
        const auto amount = m_density2[x][y];
        if(amount > 0)
            result.emplace_back(m_compoundId2, amount);
    }

    if(m_compoundId3 != NULL_COMPOUND) {
        const auto amount = m_density3[x][y];
        if(amount > 0)
            result.emplace_back(m_compoundId3, amount);
    }

    if(m_compoundId4 != NULL_COMPOUND) {
        const auto amount = m_density4[x][y];
        if(amount > 0)
            result.emplace_back(m_compoundId4, amount);
    }
}
// ------------------------------------ //
void
    CompoundCloudComponent::recycleToPosition(const Float3& newPosition)
{
    m_position = newPosition;

    m_sceneNode->setPosition(m_position.X, CLOUD_Y_COORDINATE, m_position.Z);

    // Clear data. Maybe there is a faster way
    for(size_t x = 0; x < m_density1.size(); ++x) {
        for(size_t y = 0; y < m_density1[x].size(); ++y) {

            m_density1[x][y] = 0;
            m_oldDens1[x][y] = 0;

            m_density2[x][y] = 0;
            m_oldDens2[x][y] = 0;

            m_density3[x][y] = 0;
            m_oldDens3[x][y] = 0;

            m_density4[x][y] = 0;
            m_oldDens4[x][y] = 0;
        }
    }
}

////////////////////////////////////////////////////////////////////////////////
// CompoundCloudSystem
////////////////////////////////////////////////////////////////////////////////
CompoundCloudSystem::CompoundCloudSystem() :
    m_xVelocity(CLOUD_SIMULATION_WIDTH,
        std::vector<float>(CLOUD_SIMULATION_HEIGHT, 0)),
    m_yVelocity(CLOUD_SIMULATION_WIDTH,
        std::vector<float>(CLOUD_SIMULATION_HEIGHT, 0))
{}

CompoundCloudSystem::~CompoundCloudSystem() {}

void
    CompoundCloudSystem::Init(CellStageWorld& world)
{
    // Use the curl of a Perlin noise field to create a turbulent velocity
    // field.
    createVelocityField();


    // Create a background plane on which the fluid clouds will be drawn.
    Ogre::Plane plane(Ogre::Vector3::UNIT_Y, 1.0);
    // Ogre::Plane plane(1, 1, 1, 1);

    const auto meshName =
        "CompoundCloudSystem_Plane_" + std::to_string(++CloudMeshNumberCounter);

    const auto mesh =
        Ogre::v1::MeshManager::getSingleton().createPlane(meshName + "_v1",
            Ogre::ResourceGroupManager::DEFAULT_RESOURCE_GROUP_NAME, plane,
            CLOUD_X_EXTENT, CLOUD_Y_EXTENT, 1, 1,
            // Normals. These are required for import to V2 to work
            true, 1, 1.0f, 1.0f, Ogre::Vector3::UNIT_X,
            Ogre::v1::HardwareBuffer::HBU_STATIC_WRITE_ONLY,
            Ogre::v1::HardwareBuffer::HBU_STATIC_WRITE_ONLY, false, false);

    m_planeMesh = Ogre::MeshManager::getSingleton().createManual(
        meshName, Ogre::ResourceGroupManager::DEFAULT_RESOURCE_GROUP_NAME);

    // Fourth true is qtangent encoding which is not needed if we don't do
    // normal mapping
    m_planeMesh->importV1(mesh.get(), true, true, true);

    Ogre::v1::MeshManager::getSingleton().remove(mesh);

    // This crashes when used with RenderDoc and doesn't render anything
    // m_planeMesh = Leviathan::GeometryHelpers::CreateXZPlane(
    //     meshName, CLOUD_WIDTH, CLOUD_HEIGHT);

    // Need to edit the render queue (for when the item is created)
    world.GetScene()->getRenderQueue()->setRenderQueueMode(
        2, Ogre::RenderQueue::FAST);
}

void
    CompoundCloudSystem::Release(CellStageWorld& world)
{
    // Make sure all of our entities are destroyed //
    // Because their destruction callback unregisters them we have to delete
    // them like this
    while(!m_managedClouds.empty()) {

        world.DestroyEntity(m_managedClouds.begin()->first);
    }

    // Destroy the shared mesh
    Ogre::MeshManager::getSingleton().remove(m_planeMesh);
}
// ------------------------------------ //
void
    CompoundCloudSystem::registerCloudTypes(CellStageWorld& world,
        const std::vector<Compound>& clouds)
{
    m_cloudTypes = clouds;

    // We do a spawn cycle immediately to make sure that even early code can
    // spawn clouds
    doSpawnCycle(world, Float3(0, 0, 0));
}

//! \brief Places specified amount of compound at position
bool
    CompoundCloudSystem::addCloud(CompoundId compound,
        float density,
        const Float3& worldPosition)
{
    // Find the target cloud //
    for(auto& cloud : m_managedClouds) {

        const auto& pos = cloud.second->m_position;

        if(cloudContainsPosition(pos, worldPosition)) {
            // Within cloud

            // Skip wrong types
            if(!cloud.second->handlesCompound(compound))
                continue;

            try {
                auto [x, y] = convertWorldToCloudLocal(pos, worldPosition);
                cloud.second->addCloud(compound, density, x, y);

                return true;

            } catch(const Leviathan::InvalidArgument& e) {
                LOG_ERROR("CompoundCloudSystem: can't place cloud because the "
                          "cloud math is "
                          "wrong, exception:");
                e.PrintToLog();
                return false;
            }
        }
    }

    return false;
}

//! \param rate should be less than one.
float
    CompoundCloudSystem::takeCompound(CompoundId compound,
        const Float3& worldPosition,
        float rate)
{
    for(auto& cloud : m_managedClouds) {

        const auto& pos = cloud.second->m_position;

        if(cloudContainsPosition(pos, worldPosition)) {
            // Within cloud

            // Skip wrong types
            if(!cloud.second->handlesCompound(compound))
                continue;

            try {
                auto [x, y] = convertWorldToCloudLocal(pos, worldPosition);
                return cloud.second->takeCompound(compound, x, y, rate);

            } catch(const Leviathan::InvalidArgument& e) {
                LOG_ERROR(
                    "CompoundCloudSystem: can't take from cloud because the "
                    "cloud math is "
                    "wrong, exception:");
                e.PrintToLog();
                return false;
            }
        }
    }

    return 0;
}

//! \param rate should be less than one.
float
    CompoundCloudSystem::amountAvailable(CompoundId compound,
        const Float3& worldPosition,
        float rate)
{
    for(auto& cloud : m_managedClouds) {

        const auto& pos = cloud.second->m_position;

        if(cloudContainsPosition(pos, worldPosition)) {
            // Within cloud

            // Skip wrong types
            if(!cloud.second->handlesCompound(compound))
                continue;

            try {
                auto [x, y] = convertWorldToCloudLocal(pos, worldPosition);
                return cloud.second->amountAvailable(compound, x, y, rate);

            } catch(const Leviathan::InvalidArgument& e) {
                LOG_ERROR(
                    "CompoundCloudSystem: can't get available compounds "
                    "from cloud because the cloud math is wrong, exception:");
                e.PrintToLog();
                return false;
            }
        }
    }

    return 0;
}

std::vector<std::tuple<CompoundId, float>>
    CompoundCloudSystem::getAllAvailableAt(const Float3& worldPosition)
{
    std::vector<std::tuple<CompoundId, float>> result;

    for(auto& cloud : m_managedClouds) {

        const auto& pos = cloud.second->m_position;

        if(cloudContainsPosition(pos, worldPosition)) {
            // Within cloud

            try {
                auto [x, y] = convertWorldToCloudLocal(pos, worldPosition);
                cloud.second->getCompoundsAt(x, y, result);

            } catch(const Leviathan::InvalidArgument& e) {
                LOG_ERROR(
                    "CompoundCloudSystem: can't get available compounds "
                    "from cloud because the cloud math is wrong, exception:");
                e.PrintToLog();
            }
        }
    }

    return result;
}
// ------------------------------------ //
bool
    CompoundCloudSystem::cloudContainsPosition(const Float3& cloudPosition,
        const Float3& worldPosition)
{
    if(worldPosition.X < cloudPosition.X - CLOUD_WIDTH ||
        worldPosition.X >= cloudPosition.X + CLOUD_WIDTH ||
        worldPosition.Z < cloudPosition.Z - CLOUD_HEIGHT ||
        worldPosition.Z >= cloudPosition.Z + CLOUD_HEIGHT)
        return false;
    return true;
}

bool
    CompoundCloudSystem::cloudContainsPositionWithRadius(
        const Float3& cloudPosition,
        const Float3& worldPosition,
        float radius)
{
    if(worldPosition.X + radius < cloudPosition.X - CLOUD_WIDTH ||
        worldPosition.X - radius >= cloudPosition.X + CLOUD_WIDTH ||
        worldPosition.Z + radius < cloudPosition.Z - CLOUD_HEIGHT ||
        worldPosition.Z - radius >= cloudPosition.Z + CLOUD_HEIGHT)
        return false;
    return true;
}

std::tuple<size_t, size_t>
    CompoundCloudSystem::convertWorldToCloudLocal(const Float3& cloudPosition,
        const Float3& worldPosition)
{
    const auto topLeftRelative =
        Float3(worldPosition.X - (cloudPosition.X - CLOUD_WIDTH), 0,
            worldPosition.Z - (cloudPosition.Z - CLOUD_HEIGHT));

    // Floor is used here because otherwise the last coordinate is wrong
    const auto localX =
        static_cast<size_t>(std::floor(topLeftRelative.X / CLOUD_RESOLUTION));
    const auto localY =
        static_cast<size_t>(std::floor(topLeftRelative.Z / CLOUD_RESOLUTION));

    // Safety check
    if(localX >= CLOUD_SIMULATION_WIDTH || localY >= CLOUD_SIMULATION_HEIGHT)
        throw Leviathan::InvalidArgument("position not within cloud");

    return std::make_tuple(localX, localY);
}

std::tuple<float, float>
    CompoundCloudSystem::convertWorldToCloudLocalForGrab(
        const Float3& cloudPosition,
        const Float3& worldPosition)
{
    const auto topLeftRelative =
        Float3(worldPosition.X - (cloudPosition.X - CLOUD_WIDTH), 0,
            worldPosition.Z - (cloudPosition.Z - CLOUD_HEIGHT));

    // Floor is used here because otherwise the last coordinate is wrong
    // and we don't want our caller to constantly have to call std::floor
    const auto localX = std::floor(topLeftRelative.X / CLOUD_RESOLUTION);
    const auto localY = std::floor(topLeftRelative.Z / CLOUD_RESOLUTION);

    return std::make_tuple(localX, localY);
}

// ------------------------------------ //
void
    CompoundCloudSystem::Run(CellStageWorld& world)
{
    const int renderTime = Leviathan::TICKSPEED;

    auto playerEntity = ThriveGame::instance()->playerData().activeCreature();

    Float3 position = Float3(0, 0, 0);

    if(playerEntity == NULL_OBJECT) {

        LOG_WARNING("CompoundCloudSystem: Run: playerData().activeCreature() "
                    "is NULL_OBJECT. "
                    "Using default position");
    } else {

        try {
            // Get the player's position.
            const Leviathan::Position& posEntity =
                world.GetComponent_Position(playerEntity);
            position = posEntity.Members._Position;

        } catch(const Leviathan::NotFound&) {
            LOG_WARNING("CompoundCloudSystem: Run: playerEntity(" +
                        std::to_string(playerEntity) + ") has no position");
        }
    }

    doSpawnCycle(world, position);

    for(auto& value : m_managedClouds) {

        if(!value.second->m_initialized) {
            LEVIATHAN_ASSERT(false, "CompoundCloudSystem spawned a cloud that "
                                    "it didn't initialize");
        }

        processCloud(*value.second, renderTime);
    }
}

void
    CompoundCloudSystem::doSpawnCycle(CellStageWorld& world,
        const Float3& playerPos)
{
    // Initial spawning if everything is empty
    if(m_managedClouds.empty()) {

        LOG_INFO("CompoundCloudSystem doing initial spawning");

        m_cloudGridCenter = Float3(0, 0, 0);

        for(size_t i = 0; i < m_cloudTypes.size(); i += CLOUDS_IN_ONE) {

            // Center
            _spawnCloud(world, m_cloudGridCenter, i);

            // Top left
            _spawnCloud(world,
                m_cloudGridCenter +
                    Float3(-CLOUD_WIDTH * 2, 0, -CLOUD_HEIGHT * 2),
                i);

            // Up
            _spawnCloud(
                world, m_cloudGridCenter + Float3(0, 0, -CLOUD_HEIGHT * 2), i);

            // Top right
            _spawnCloud(world,
                m_cloudGridCenter +
                    Float3(CLOUD_WIDTH * 2, 0, -CLOUD_HEIGHT * 2),
                i);

            // Left
            _spawnCloud(
                world, m_cloudGridCenter + Float3(-CLOUD_WIDTH * 2, 0, 0), i);

            // Right
            _spawnCloud(
                world, m_cloudGridCenter + Float3(CLOUD_WIDTH * 2, 0, 0), i);

            // Bottom left
            _spawnCloud(world,
                m_cloudGridCenter +
                    Float3(-CLOUD_WIDTH * 2, 0, CLOUD_HEIGHT * 2),
                i);

            // Down
            _spawnCloud(
                world, m_cloudGridCenter + Float3(0, 0, CLOUD_HEIGHT * 2), i);

            // Bottom right
            _spawnCloud(world,
                m_cloudGridCenter +
                    Float3(CLOUD_WIDTH * 2, 0, CLOUD_HEIGHT * 2),
                i);
        }
    }

    const auto moved = playerPos - m_cloudGridCenter;

    // TODO: because we no longer check if the player has moved at least a bit
    // it is possible that this gets triggered very often if the player spins
    // around a cloud edge, but hopefully there isn't a performance problem and
    // that case can just be ignored.
    // Z is used here because these are world coordinates
    if(std::abs(moved.X) > CLOUD_WIDTH || std::abs(moved.Z) > CLOUD_HEIGHT) {

        // Calculate the new center
        if(moved.X < -CLOUD_WIDTH) {
            m_cloudGridCenter -= Float3(CLOUD_WIDTH * 2, 0, 0);
        } else if(moved.X > CLOUD_WIDTH) {
            m_cloudGridCenter += Float3(CLOUD_WIDTH * 2, 0, 0);
        }

        if(moved.Z < -CLOUD_HEIGHT) {
            m_cloudGridCenter -= Float3(0, 0, CLOUD_HEIGHT * 2);
        } else if(moved.Z > CLOUD_HEIGHT) {
            m_cloudGridCenter += Float3(0, 0, CLOUD_HEIGHT * 2);
        }

        // Reposition clouds according to the origin
        // MAX of 9 clouds can ever be repositioned (this is only the case when
        // respawning)
        constexpr size_t MAX_FAR_CLOUDS = 9;
        std::array<CompoundCloudComponent*, MAX_FAR_CLOUDS> tooFarAwayClouds;
        size_t farAwayIndex = 0;

        for(auto iter = m_managedClouds.begin(); iter != m_managedClouds.end();
            ++iter) {

            const auto pos = iter->second->m_position;

            const auto distance = m_cloudGridCenter - pos;

            if(std::abs(distance.X) > 3 * CLOUD_WIDTH ||
                std::abs(distance.Z) > 3 * CLOUD_HEIGHT) {

                if(farAwayIndex >= MAX_FAR_CLOUDS) {

                    LOG_FATAL("CompoundCloudSystem: Logic error in calculating "
                              "far away clouds that need to move");
                    break;
                }

                tooFarAwayClouds[farAwayIndex++] = iter->second;
            }
        }

        // Move clouds that are too far away
        // We check through each position that should have a cloud and move one
        // where there isn't one

        const Float3 requiredCloudPositions[] = {
            // Center
            m_cloudGridCenter,

            // Top left
            m_cloudGridCenter + Float3(-CLOUD_WIDTH * 2, 0, -CLOUD_HEIGHT * 2),

            // Up
            m_cloudGridCenter + Float3(0, 0, -CLOUD_HEIGHT * 2),

            // Top right
            m_cloudGridCenter + Float3(CLOUD_WIDTH * 2, 0, -CLOUD_HEIGHT * 2),

            // Left
            m_cloudGridCenter + Float3(-CLOUD_WIDTH * 2, 0, 0),

            // Right
            m_cloudGridCenter + Float3(CLOUD_WIDTH * 2, 0, 0),

            // Bottom left
            m_cloudGridCenter + Float3(-CLOUD_WIDTH * 2, 0, CLOUD_HEIGHT * 2),

            // Down
            m_cloudGridCenter + Float3(0, 0, CLOUD_HEIGHT * 2),

            // Bottom right
            m_cloudGridCenter + Float3(CLOUD_WIDTH * 2, 0, CLOUD_HEIGHT * 2),
        };

        size_t farAwayRepositionedIndex = 0;

        for(size_t i = 0; i < std::size(requiredCloudPositions); ++i) {

            bool hasCloud = false;
            const auto& requiredPos = requiredCloudPositions[i];

            for(auto iter = m_managedClouds.begin();
                iter != m_managedClouds.end(); ++iter) {

                const auto pos = iter->second->m_position;

                // An exact check might work but just to be safe slight
                // inaccuracy is allowed here
                if((pos - requiredPos).HAddAbs() < Leviathan::EPSILON) {
                    hasCloud = true;
                    break;
                }
            }

            if(hasCloud)
                continue;

            if(farAwayRepositionedIndex >= farAwayIndex) {
                LOG_FATAL("CompoundCloudSystem: Logic error in moving far "
                          "clouds (ran out)");
                break;
            }

            tooFarAwayClouds[farAwayRepositionedIndex++]->recycleToPosition(
                requiredPos);
        }
    }
}

void
    CompoundCloudSystem::_spawnCloud(CellStageWorld& world,
        const Float3& pos,
        size_t startIndex)
{
    auto entity = world.CreateEntity();

    Compound* first =
        startIndex < m_cloudTypes.size() ? &m_cloudTypes[startIndex] : nullptr;
    Compound* second = startIndex + 1 < m_cloudTypes.size() ?
                           &m_cloudTypes[startIndex + 1] :
                           nullptr;
    Compound* third = startIndex + 2 < m_cloudTypes.size() ?
                          &m_cloudTypes[startIndex + 2] :
                          nullptr;
    Compound* fourth = startIndex + 3 < m_cloudTypes.size() ?
                           &m_cloudTypes[startIndex + 3] :
                           nullptr;

    CompoundCloudComponent& cloud = world.Create_CompoundCloudComponent(
        entity, *this, first, second, third, fourth);

    // Set correct position
    // TODO: this should probably be made a constructor parameter
    cloud.m_position = pos;

    initializeCloud(cloud, world.GetScene());
    m_managedClouds[entity] = &cloud;
}


void
    CompoundCloudSystem::initializeCloud(CompoundCloudComponent& cloud,
        Ogre::SceneManager* scene)
{
    LOG_INFO("Initializing a new compound cloud entity");

    // Create where the eventually created plane object will be attached
    cloud.m_sceneNode = scene->getRootSceneNode()->createChildSceneNode();

    // set the position properly
    cloud.m_sceneNode->setPosition(
        cloud.m_position.X, CLOUD_Y_COORDINATE, cloud.m_position.Z);

    // Because of the way Ogre generates the UVs for a plane we need to rotate
    // the plane to match up with world coordinates
    cloud.m_sceneNode->setOrientation(
        Ogre::Quaternion(Ogre::Degree(90), Ogre::Vector3::UNIT_Y));

    // All the densities
    if(cloud.m_compoundId1 != NULL_COMPOUND) {
        cloud.m_density1.resize(CLOUD_SIMULATION_WIDTH,
            std::vector<float>(CLOUD_SIMULATION_HEIGHT, 0));
        cloud.m_oldDens1.resize(CLOUD_SIMULATION_WIDTH,
            std::vector<float>(CLOUD_SIMULATION_HEIGHT, 0));
    }
    if(cloud.m_compoundId2 != NULL_COMPOUND) {
        cloud.m_density2.resize(CLOUD_SIMULATION_WIDTH,
            std::vector<float>(CLOUD_SIMULATION_HEIGHT, 0));
        cloud.m_oldDens2.resize(CLOUD_SIMULATION_WIDTH,
            std::vector<float>(CLOUD_SIMULATION_HEIGHT, 0));
    }
    if(cloud.m_compoundId3 != NULL_COMPOUND) {
        cloud.m_density3.resize(CLOUD_SIMULATION_WIDTH,
            std::vector<float>(CLOUD_SIMULATION_HEIGHT, 0));
        cloud.m_oldDens3.resize(CLOUD_SIMULATION_WIDTH,
            std::vector<float>(CLOUD_SIMULATION_HEIGHT, 0));
    }
    if(cloud.m_compoundId4 != NULL_COMPOUND) {
        cloud.m_density4.resize(CLOUD_SIMULATION_WIDTH,
            std::vector<float>(CLOUD_SIMULATION_HEIGHT, 0));
        cloud.m_oldDens4.resize(CLOUD_SIMULATION_WIDTH,
            std::vector<float>(CLOUD_SIMULATION_HEIGHT, 0));
    }

    // Create a modified material that uses
    cloud.m_planeMaterial = Ogre::MaterialManager::getSingleton().create(
        cloud.m_textureName + "_material", "Generated");

    cloud.m_planeMaterial->setReceiveShadows(false);

    // cloud.m_planeMaterial->createTechnique();
    LEVIATHAN_ASSERT(cloud.m_planeMaterial->getTechnique(0) &&
                         cloud.m_planeMaterial->getTechnique(0)->getPass(0),
        "Ogre material didn't create default technique and pass");
    Ogre::Pass* pass = cloud.m_planeMaterial->getTechnique(0)->getPass(0);

    // Set blendblock
    Ogre::HlmsBlendblock blendblock;
    blendblock.mAlphaToCoverageEnabled = true;

    // This is the old setting
    // pass->setSceneBlending(Ogre::SBT_TRANSPARENT_ALPHA);
    // And according to Ogre source code (OgrePass.cpp Pass::_getBlendFlags)
    // it matches this: source = SBF_SOURCE_ALPHA; dest =
    // SBF_ONE_MINUS_SOURCE_ALPHA;

    blendblock.mSourceBlendFactor = Ogre::SBF_SOURCE_ALPHA;
    blendblock.mDestBlendFactor = Ogre::SBF_ONE_MINUS_SOURCE_ALPHA;

    // Important for proper blending (not sure,
    // mAlphaToCoverageEnabled seems to be more important as a lot of
    // stuff breaks without it)
    blendblock.mIsTransparent = true;

    pass->setBlendblock(blendblock);
    pass->setVertexProgram("CompoundCloud_VS");
    pass->setFragmentProgram("CompoundCloud_PS");

    // Set colour parameter //
    if(cloud.m_compoundId1 != NULL_COMPOUND)
        pass->getFragmentProgramParameters()->setNamedConstant(
            "cloudColour1", cloud.m_color1);
    if(cloud.m_compoundId2 != NULL_COMPOUND)
        pass->getFragmentProgramParameters()->setNamedConstant(
            "cloudColour2", cloud.m_color2);
    if(cloud.m_compoundId3 != NULL_COMPOUND)
        pass->getFragmentProgramParameters()->setNamedConstant(
            "cloudColour3", cloud.m_color3);
    if(cloud.m_compoundId4 != NULL_COMPOUND)
        pass->getFragmentProgramParameters()->setNamedConstant(
            "cloudColour4", cloud.m_color4);

    // The perlin noise texture needs to be tileable. We can't do tricks with
    // the cloud's position

    cloud.m_texture = Ogre::TextureManager::getSingleton().createManual(
        cloud.m_textureName, "Generated", Ogre::TEX_TYPE_2D,
        CLOUD_SIMULATION_WIDTH, CLOUD_SIMULATION_HEIGHT, 0, Ogre::PF_BYTE_RGBA,
        Ogre::TU_DYNAMIC_WRITE_ONLY_DISCARDABLE,
        nullptr
        // Gamma correction
        ,
        true);

    LEVIATHAN_ASSERT(Ogre::PixelUtil::getNumElemBytes(Ogre::PF_BYTE_RGBA) ==
                         OGRE_CLOUD_TEXTURE_BYTES_PER_ELEMENT,
        "Pixel format bytes has changed");

    auto pixelBuffer = cloud.m_texture->getBuffer();
    pixelBuffer->lock(Ogre::v1::HardwareBuffer::HBL_DISCARD);
    const Ogre::PixelBox& pixelBox = pixelBuffer->getCurrentLock();

    // Fill with zeroes
    std::memset(
        static_cast<uint8_t*>(pixelBox.data), 0, pixelBuffer->getSizeInBytes());

    // Unlock the pixel buffer
    pixelBuffer->unlock();
    // Make sure it wraps to make the borders also look good
    // TODO: check is this needed. This is absolutely needed for the perlin
    // noise but probably not for the cloud densities. So it is easier to keep
    // this for now
    Ogre::HlmsSamplerblock wrappedBlock;
    wrappedBlock.setAddressingMode(Ogre::TextureAddressingMode::TAM_WRAP);

    auto* densityState = pass->createTextureUnitState();
    densityState->setTexture(cloud.m_texture);
    // densityState->setTextureName("TestImageThing.png");
    densityState->setSamplerblock(wrappedBlock);

    Ogre::TexturePtr texturePtr =
        Ogre::TextureManager::getSingleton().load("PerlinNoise.jpg",
            Ogre::ResourceGroupManager::DEFAULT_RESOURCE_GROUP_NAME);
    auto* noiseState = pass->createTextureUnitState();
    noiseState->setTexture(texturePtr);

    noiseState->setSamplerblock(wrappedBlock);

    // Maybe compiling this here is the best place
    cloud.m_planeMaterial->compile();

    // Needs to create a plane instance on which the material is used on
    cloud.m_compoundCloudsPlane = scene->createItem(m_planeMesh);
    cloud.m_compoundCloudsPlane->setCastShadows(false);

    // This needs to be add to an early render queue
    // But after the background
    cloud.m_compoundCloudsPlane->setRenderQueueGroup(2);

    cloud.m_sceneNode->attachObject(cloud.m_compoundCloudsPlane);

    // This loads the material first time this is called. This needs
    // to be called AFTER the first compound cloud has been created. We are
    // currently initializing one so it is fine
    cloud.m_compoundCloudsPlane->setMaterialName(
        cloud.m_planeMaterial->getName());

    cloud.m_initialized = true;
}
// ------------------------------------ //
void
    CompoundCloudSystem::cloudReportDestroyed(CompoundCloudComponent* cloud)
{
    for(auto iter = m_managedClouds.begin(); iter != m_managedClouds.end();
        ++iter) {

        if(iter->second == cloud) {
            m_managedClouds.erase(iter);
            return;
        }
    }

    LOG_WARNING("CompoundCloudSystem: non-registered CompoundCloudComponent "
                "reported that it was destroyed");
}
// ------------------------------------ //
void
    CompoundCloudSystem::processCloud(CompoundCloudComponent& cloud,
        int renderTime)
{
    // Try to slow things down (doesn't seem to work great)
    renderTime /= 5;

    // The diffusion rate seems to have a bigger effect

    // Compound clouds move from area of high concentration to area of low.
    if(cloud.m_compoundId1 != NULL_COMPOUND) {
        diffuse(0.0001f, cloud.m_oldDens1, cloud.m_density1, renderTime);
        // Move the compound clouds about the velocity field.
        advect(cloud.m_oldDens1, cloud.m_density1, renderTime);
    }
    if(cloud.m_compoundId2 != NULL_COMPOUND) {
        diffuse(0.0001f, cloud.m_oldDens2, cloud.m_density2, renderTime);
        // Move the compound clouds about the velocity field.
        advect(cloud.m_oldDens2, cloud.m_density2, renderTime);
    }
    if(cloud.m_compoundId3 != NULL_COMPOUND) {
        diffuse(0.0001f, cloud.m_oldDens3, cloud.m_density3, renderTime);
        // Move the compound clouds about the velocity field.
        advect(cloud.m_oldDens3, cloud.m_density3, renderTime);
    }
    if(cloud.m_compoundId4 != NULL_COMPOUND) {
        diffuse(0.0001f, cloud.m_oldDens4, cloud.m_density4, renderTime);
        // Move the compound clouds about the velocity field.
        advect(cloud.m_oldDens4, cloud.m_density4, renderTime);
    }

    // Store the pixel data in a hardware buffer for quick access.
    auto pixelBuffer = cloud.m_texture->getBuffer();

    pixelBuffer->lock(Ogre::v1::HardwareBuffer::HBL_DISCARD);
    const Ogre::PixelBox& pixelBox = pixelBuffer->getCurrentLock();
    auto* pDest = static_cast<uint8_t*>(pixelBox.data);

    const size_t rowBytes =
        pixelBox.rowPitch * OGRE_CLOUD_TEXTURE_BYTES_PER_ELEMENT;

    // Copy the density vector into the buffer.

    // This is probably branch predictor friendly to move each bunch of pixels
    // separately

    // First channel
    if(cloud.m_compoundId1 != NULL_COMPOUND)
        fillCloudChannel(cloud.m_density1, 0, rowBytes, pDest);
    // Second
    if(cloud.m_compoundId2 != NULL_COMPOUND)
        fillCloudChannel(cloud.m_density2, 1, rowBytes, pDest);
    // Etc.
    if(cloud.m_compoundId3 != NULL_COMPOUND)
        fillCloudChannel(cloud.m_density3, 2, rowBytes, pDest);
    if(cloud.m_compoundId4 != NULL_COMPOUND)
        fillCloudChannel(cloud.m_density4, 3, rowBytes, pDest);

    // Unlock the pixel buffer.
    pixelBuffer->unlock();
}

void
    CompoundCloudSystem::fillCloudChannel(
        const std::vector<std::vector<float>>& density,
        size_t index,
        size_t rowBytes,
        uint8_t* pDest)
{
    const auto height = density[0].size();
    for(size_t j = 0; j < height; j++) {
        for(int i = 0; i < density.size(); i++) {

            int intensity = static_cast<int>(density[i][j]);

            // This is the same clamping code as in the old version
            if(intensity < 0) {
                intensity = 0;
            } else if(intensity > 255) {
                intensity = 255;
            }

            pDest[rowBytes * j + (i * OGRE_CLOUD_TEXTURE_BYTES_PER_ELEMENT) +
                  index] = static_cast<uint8_t>(intensity);
        }
    }
}


void
    CompoundCloudSystem::createVelocityField()
{
    const float nxScale = m_noiseScale;
    // "float(CLOUD_SIMULATION_WIDTH) / float(CLOUD_SIMULATION_HEIGHT)" is the
    // aspect ratio of the cloud. This is 1 if the cloud is a square.
    const float nyScale = nxScale * (float(CLOUD_SIMULATION_WIDTH) /
                                        float(CLOUD_SIMULATION_HEIGHT));

    for(int x = 0; x < CLOUD_SIMULATION_WIDTH; x++) {
        for(int y = 0; y < CLOUD_SIMULATION_HEIGHT; y++) {
            const float x0 =
                (float(x - 1) / float(CLOUD_SIMULATION_WIDTH)) * nxScale;
            const float y0 =
                (float(y - 1) / float(CLOUD_SIMULATION_HEIGHT)) * nyScale;
            const float x1 =
                (float(x + 1) / float(CLOUD_SIMULATION_WIDTH)) * nxScale;
            const float y1 =
                (float(y + 1) / float(CLOUD_SIMULATION_HEIGHT)) * nyScale;

            float n0 = m_fieldPotential.noise(x0, y0, 0);
            float n1 = m_fieldPotential.noise(x1, y0, 0);
            const float ny = n0 - n1;
            n0 = m_fieldPotential.noise(x0, y0, 0);
            n1 = m_fieldPotential.noise(x0, y1, 0);
            const float nx = n1 - n0;

            m_xVelocity[x][y] = nx / 2;
            m_yVelocity[x][y] = ny / 2;
        }
    }
}

void
    CompoundCloudSystem::diffuse(float diffRate,
        std::vector<std::vector<float>>& oldDens,
        const std::vector<std::vector<float>>& density,
        int dt)
{
    float a = dt * diffRate;

    for(int x = 1; x < CLOUD_SIMULATION_WIDTH - 1; x++) {
        for(int y = 1; y < CLOUD_SIMULATION_HEIGHT - 1; y++) {
            oldDens[x][y] =
                (density[x][y] +
                    a * (oldDens[x - 1][y] + oldDens[x + 1][y] +
                            oldDens[x][y - 1] + oldDens[x][y + 1])) /
                (1 + 4 * a);
        }
    }
}

void
    CompoundCloudSystem::advect(const std::vector<std::vector<float>>& oldDens,
        std::vector<std::vector<float>>& density,
        int dt)
{
    for(int x = 0; x < CLOUD_SIMULATION_WIDTH; x++) {
        for(int y = 0; y < CLOUD_SIMULATION_HEIGHT; y++) {
            density[x][y] = 0;
        }
    }

    // TODO: this is probably the place to move the compounds on the edges into
    // the next cloud (instead of not handling them here)
    for(size_t x = 1; x < CLOUD_SIMULATION_WIDTH - 1; x++) {
        for(size_t y = 1; y < CLOUD_SIMULATION_HEIGHT - 1; y++) {
            if(oldDens[x][y] > 1) {
                float dx = x + dt * m_xVelocity[x][y];
                float dy = y + dt * m_yVelocity[x][y];

                if(dx < 0.5f)
                    dx = 0.5f;
                if(dx > CLOUD_SIMULATION_WIDTH - 1.5f)
                    dx = CLOUD_SIMULATION_WIDTH - 1.5f;

                if(dy < 0.5f)
                    dy = 0.5f;
                if(dy > CLOUD_SIMULATION_HEIGHT - 1.5f)
                    dy = CLOUD_SIMULATION_HEIGHT - 1.5f;

                const int x0 = static_cast<int>(dx);
                const int x1 = x0 + 1;
                const int y0 = static_cast<int>(dy);
                const int y1 = y0 + 1;

                float s1 = dx - x0;
                float s0 = 1.0f - s1;
                float t1 = dy - y0;
                float t0 = 1.0f - t1;

                density[x0][y0] += oldDens[x][y] * s0 * t0;
                density[x0][y1] += oldDens[x][y] * s0 * t1;
                density[x1][y0] += oldDens[x][y] * s1 * t0;
                density[x1][y1] += oldDens[x][y] * s1 * t1;
            }
        }
    }
}
