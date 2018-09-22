#include "microbe_stage/compound_cloud_system.h"
#include "microbe_stage/simulation_parameters.h"

#include "ThriveGame.h"

#include "engine/player_data.h"
#include "generated/cell_stage_world.h"

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

constexpr auto YOffset = -1;
constexpr auto OGRE_CLOUD_TEXTURE_BYTES_PER_ELEMENT = 4;

static std::atomic<int> CloudTextureNumber = {0};
static std::atomic<int> CloudMeshNumberCounter = {0};

////////////////////////////////////////////////////////////////////////////////
// CompoundCloudComponent
////////////////////////////////////////////////////////////////////////////////
CompoundCloudComponent::CompoundCloudComponent(Compound* first,
    Compound* second,
    Compound* third,
    Compound* fourth) :
    Leviathan::Component(TYPE),
    m_textureName("cloud_" + std::to_string(++CloudTextureNumber))
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

    if(initialized) {

        initialized = false;
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
    if(x >= width || y >= height)
        throw std::runtime_error(
            "CompoundCloudComponent coordinates out of range");

    LOG_WRITE("Pos things: " + std::to_string(x) + ", " + std::to_string(y) +
              " width: " + std::to_string(width) +
              " height: " + std::to_string(height));

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
    if(x >= width || y >= height)
        throw std::runtime_error(
            "CompoundCloudComponent coordinates out of range");

    switch(getSlotForCompound(compound)) {
    case SLOT::FIRST: {
        int amountToGive = static_cast<int>(m_density1[x][y]) * rate;
        m_density1[x][y] -= amountToGive;
        if(m_density1[x][y] < 1)
            m_density1[x][y] = 0;

        return amountToGive;
    }
    case SLOT::SECOND: {
        int amountToGive = static_cast<int>(m_density2[x][y]) * rate;
        m_density2[x][y] -= amountToGive;
        if(m_density2[x][y] < 1)
            m_density2[x][y] = 0;

        return amountToGive;
    }
    case SLOT::THIRD: {
        int amountToGive = static_cast<int>(m_density3[x][y]) * rate;
        m_density3[x][y] -= amountToGive;
        if(m_density3[x][y] < 1)
            m_density3[x][y] = 0;

        return amountToGive;
    }
    case SLOT::FOURTH: {
        int amountToGive = static_cast<int>(m_density4[x][y]) * rate;
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
    if(x >= width || y >= height)
        throw std::runtime_error(
            "CompoundCloudComponent coordinates out of range");

    switch(getSlotForCompound(compound)) {
    case SLOT::FIRST: {
        int amountToGive = static_cast<int>(m_density1[x][y]) * rate;
        return amountToGive;
    }
    case SLOT::SECOND: {
        int amountToGive = static_cast<int>(m_density2[x][y]) * rate;
        return amountToGive;
    }
    case SLOT::THIRD: {
        int amountToGive = static_cast<int>(m_density3[x][y]) * rate;
        return amountToGive;
    }
    case SLOT::FOURTH: {
        int amountToGive = static_cast<int>(m_density4[x][y]) * rate;
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
    if(x >= width || y >= height)
        throw std::runtime_error(
            "CompoundCloudComponent coordinates out of range");

    if(m_compoundId1 != NULL_COMPOUND) {
        const auto amount = m_density1[x][y];
        if(amount > 0)
            result.push_back(std::make_tuple(m_compoundId1, amount));
    }

    if(m_compoundId2 != NULL_COMPOUND) {
        const auto amount = m_density2[x][y];
        if(amount > 0)
            result.push_back(std::make_tuple(m_compoundId2, amount));
    }

    if(m_compoundId3 != NULL_COMPOUND) {
        const auto amount = m_density3[x][y];
        if(amount > 0)
            result.push_back(std::make_tuple(m_compoundId3, amount));
    }

    if(m_compoundId4 != NULL_COMPOUND) {
        const auto amount = m_density4[x][y];
        if(amount > 0)
            result.push_back(std::make_tuple(m_compoundId4, amount));
    }
}

////////////////////////////////////////////////////////////////////////////////
// CompoundCloudSystem
////////////////////////////////////////////////////////////////////////////////
CompoundCloudSystem::CompoundCloudSystem() :
    xVelocity(width, std::vector<float>(height, 0)),
    yVelocity(width, std::vector<float>(height, 0))
{}

CompoundCloudSystem::~CompoundCloudSystem() {}

void
    CompoundCloudSystem::Init(CellStageWorld& world)
{
    // Use the curl of a Perlin noise field to create a turbulent velocity
    // field.
    CreateVelocityField();

    // Create a background plane on which the fluid clouds will be drawn.
    // Ogre::Plane plane(Ogre::Vector3::UNIT_Z, -1.0);
    Ogre::Plane plane(1, 1, 1, 1);

    const auto meshName =
        "CompoundCloudSystem_Plane_" + std::to_string(++CloudMeshNumberCounter);

    const auto mesh =
        Ogre::v1::MeshManager::getSingleton().createPlane(meshName + "_v1",
            Ogre::ResourceGroupManager::DEFAULT_RESOURCE_GROUP_NAME, plane,
            width * gridSize, height * gridSize, 1, 1,
            // Normals
            true, 1, 1.0f, 1.0f, Ogre::Vector3::UNIT_Y,
            Ogre::v1::HardwareBuffer::HBU_STATIC_WRITE_ONLY,
            Ogre::v1::HardwareBuffer::HBU_STATIC_WRITE_ONLY, false, false);

    m_planeMesh = Ogre::MeshManager::getSingleton().createManual(
        meshName, Ogre::ResourceGroupManager::DEFAULT_RESOURCE_GROUP_NAME);

    // Fourth true is qtangent encoding which is not needed if we don't do
    // normal mapping
    m_planeMesh->importV1(mesh.get(), true, true, true);

    Ogre::v1::MeshManager::getSingleton().remove(mesh);

    // Need to edit the render queue (for when the item is created)
    world.GetScene()->getRenderQueue()->setRenderQueueMode(
        2, Ogre::RenderQueue::FAST);
}

void
    CompoundCloudSystem::Release(CellStageWorld& world)
{
    // Make sure all of our entities are destroyed //

    for(auto iter = m_managedClouds.begin(); iter != m_managedClouds.end();
        ++iter) {

        world.DestroyEntity(iter->first);
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
        float x,
        float z)
{
    // TODO: store these
    const auto halfWidth = width * gridSize / 2;
    const auto halfHeight = height * gridSize / 2;

    // Find the target cloud //
    for(auto& cloud : m_managedClouds) {

        const auto& pos = cloud.second->m_position;

        const float relativeX = (x - (pos.X - halfWidth)) / gridSize;
        const float relativeZ = (z - (pos.Z - halfHeight)) / gridSize;

        if(relativeX >= 0 && relativeX <= width && relativeZ >= 0 &&
            relativeZ <= height) {
            // Within cloud

            // Skip wrong types
            if(!cloud.second->handlesCompound(compound))
                continue;

            LOG_INFO("Adding compound: " + std::to_string(compound) +
                     " (amount: " + std::to_string(density) + ") to cloud (" +
                     std::to_string(cloud.first) + ") at pos: " +
                     std::to_string(x) + ", " + std::to_string(z) +
                     ", relative pos: " + std::to_string(relativeX) + ", " +
                     std::to_string(relativeZ));

            cloud.second->addCloud(compound, density, relativeX, relativeZ);
        }
    }

    return false;
}

//! \param rate should be less than one.
float
    CompoundCloudSystem::takeCompound(CompoundId compound,
        float x,
        float z,
        float rate)
{
    return 0;
}

//! \param rate should be less than one.
float
    CompoundCloudSystem::amountAvailable(CompoundId compound,
        float x,
        float z,
        float rate)
{
    return 0;
}

std::vector<std::tuple<CompoundId, float>>
    CompoundCloudSystem::getAllAvailableAt(float x, float z)
{
    std::vector<std::tuple<CompoundId, float>> result;

    // TODO: store these
    const auto halfWidth = width * gridSize / 2;
    const auto halfHeight = height * gridSize / 2;

    // Find the target cloud //
    for(auto& cloud : m_managedClouds) {

        const auto& pos = cloud.second->m_position;

        const float relativeX = (x - (pos.X - halfWidth)) / gridSize;
        const float relativeZ = (z - (pos.Z - halfHeight)) / gridSize;

        if(relativeX >= 0 && relativeX <= width && relativeZ >= 0 &&
            relativeZ <= height) {
            // Within cloud

            // LOG_INFO("Adding compound: " + std::to_string(compound) +
            //          " (amount: " + std::to_string(density) + ") to cloud ("
            //          + std::to_string(cloud.first) + ") at pos: " +
            //          std::to_string(x) + ", " + std::to_string(z) +
            //          ", relative pos: " + std::to_string(relativeX) + ", " +
            //          std::to_string(relativeZ));

            // We don't need to check for collisions as the clouds
            // don't overlap and a single compound type is only in one
            // cloud
            cloud.second->getCompoundsAt(relativeX, relativeZ, result);
        }
    }

    return result;
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

        if(!value.second->initialized) {
            LEVIATHAN_ASSERT(false, "CompoundCloudSystem spawned a cloud that "
                                    "it didn't initialize");
        }

        ProcessCloud(*value.second, renderTime);
    }
}

void
    CompoundCloudSystem::doSpawnCycle(CellStageWorld& world,
        const Float3& playerPos)
{
    // Initial spawning if everything is empty
    if(m_managedClouds.empty()) {

        LOG_INFO("CompoundCloudSystem doing initial spawning");

        for(size_t i = 0; i < m_cloudTypes.size(); i += CLOUDS_IN_ONE) {

            _spawnCloud(world, playerPos, i);
        }

        m_lastPosition = playerPos;
    }

    const auto moved = playerPos - m_lastPosition;

    // Despawn clouds that are too far away

    // And spawn new ones
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
        entity, first, second, third, fourth);


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

    // Set initial position and the rotation that is preserved

    // shift cloud to the left by half a width so its positioned correctly
    // cloud.m_position.X = cloud.m_position.X - width / 2;

    const auto halfWidth = width * gridSize / 2;
    const auto halfHeight = height * gridSize / 2;
    // set the position properly
    cloud.m_sceneNode->setPosition(
        cloud.m_position.X, YOffset, cloud.m_position.Z);

    // Stolen from the old background rotation
    // Ogre::Quaternion(0, sqrt(0.5), 1, sqrt(0.5)) * 4
    // 0,45,90,45
    // Ogre::Quaternion rot;
    // rot.FromAngleAxis(Ogre::Degree(90),
    //     Ogre::Vector3(sqrt(0.5), sqrt(0.5), 1).normalisedCopy());

    // This isn't fully right so the UVs and the shader compliments this
    // rotation
    cloud.m_sceneNode->setOrientation(
        // Ogre::Quaternion(0, sqrt(.5), 1, sqrt(.5))
        Ogre::Quaternion(Ogre::Degree(90), Ogre::Vector3::UNIT_Z) *
        Ogre::Quaternion(Ogre::Degree(45), Ogre::Vector3::UNIT_Y));


    // Set the size of each grid tile and its position.
    cloud.width = width;
    cloud.height = height;
    cloud.gridSize = gridSize;

    // All the densities
    if(cloud.m_compoundId1 != NULL_COMPOUND) {
        cloud.m_density1.resize(width, std::vector<float>(height, 0));
        cloud.m_oldDens1.resize(width, std::vector<float>(height, 0));
    }
    if(cloud.m_compoundId2 != NULL_COMPOUND) {
        cloud.m_density2.resize(width, std::vector<float>(height, 0));
        cloud.m_oldDens2.resize(width, std::vector<float>(height, 0));
    }
    if(cloud.m_compoundId3 != NULL_COMPOUND) {
        cloud.m_density3.resize(width, std::vector<float>(height, 0));
        cloud.m_oldDens3.resize(width, std::vector<float>(height, 0));
    }
    if(cloud.m_compoundId4 != NULL_COMPOUND) {
        cloud.m_density4.resize(width, std::vector<float>(height, 0));
        cloud.m_oldDens4.resize(width, std::vector<float>(height, 0));
    }

    // Testing putting compound at different points
    // cloud.m_density1[width / 2][height / 2] = 190;
    // cloud.m_density1[width / 2 - 1][height / 2 - 1] = 190;
    // cloud.m_density1[width / 2 + 1][height / 2 + 1] = 190;


    // Modifies the material to draw this compound cloud in addition to the
    // others.
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
    // blendblock.mAlphaToCoverageEnabled = false;

    // This is the old setting
    // pass->setSceneBlending(Ogre::SBT_TRANSPARENT_ALPHA);
    // And according to Ogre source code (OgrePass.cpp Pass::_getBlendFlags)
    // it matches this: source = SBF_SOURCE_ALPHA; dest =
    // SBF_ONE_MINUS_SOURCE_ALPHA;

    blendblock.mSourceBlendFactor = Ogre::SBF_SOURCE_ALPHA;
    blendblock.mDestBlendFactor = Ogre::SBF_ONE_MINUS_SOURCE_ALPHA;

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

    // Position for consistent UVs
    pass->getFragmentProgramParameters()->setNamedConstant(
        "cloudPos", cloud.m_position);

    cloud.m_texture = Ogre::TextureManager::getSingleton().createManual(
        cloud.m_textureName, "Generated", Ogre::TEX_TYPE_2D, width, height, 0,
        Ogre::PF_BYTE_RGBA, Ogre::TU_DYNAMIC_WRITE_ONLY_DISCARDABLE, nullptr
        // Gamma correction
        // , true
    );

    LEVIATHAN_ASSERT(Ogre::PixelUtil::getNumElemBytes(Ogre::PF_BYTE_RGBA) ==
                         OGRE_CLOUD_TEXTURE_BYTES_PER_ELEMENT,
        "Pixel format bytes has changed");

    // TODO: switch to Ogre 2.1 way
    auto pixelBuffer = cloud.m_texture->getBuffer();
    pixelBuffer->lock(Ogre::v1::HardwareBuffer::HBL_DISCARD);
    const Ogre::PixelBox& pixelBox = pixelBuffer->getCurrentLock();

    // Fill with zeroes
    std::memset(
        static_cast<uint8_t*>(pixelBox.data), 0, pixelBuffer->getSizeInBytes());

    // Unlock the pixel buffer
    pixelBuffer->unlock();
    // Make sure it wraps to make the borders also look good
    // TODO: check is this needed
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
    // to be called AFTER the first compound cloud has been created
    cloud.m_compoundCloudsPlane->setMaterialName(
        cloud.m_planeMaterial->getName());

    // TODO: check if this approach could be used to use a single material for
    // all clouds cloudsPlane->getSubItem(0)->setCustomParameter(1, offset);

    cloud.initialized = true;
}

void
    CompoundCloudSystem::ProcessCloud(CompoundCloudComponent& cloud,
        int renderTime)
{
    // diffusing appears to work fine
    // Compound clouds move from area of high concentration to area of low.
    if(cloud.m_compoundId1 != NULL_COMPOUND) {
        diffuse(.007, cloud.m_oldDens1, cloud.m_density1, renderTime);
        // Move the compound clouds about the velocity field.
        advect(cloud.m_oldDens1, cloud.m_density1, renderTime);
    }
    if(cloud.m_compoundId2 != NULL_COMPOUND) {
        diffuse(.007, cloud.m_oldDens2, cloud.m_density2, renderTime);
        // Move the compound clouds about the velocity field.
        advect(cloud.m_oldDens2, cloud.m_density2, renderTime);
    }
    if(cloud.m_compoundId3 != NULL_COMPOUND) {
        diffuse(.007, cloud.m_oldDens3, cloud.m_density3, renderTime);
        // Move the compound clouds about the velocity field.
        advect(cloud.m_oldDens3, cloud.m_density3, renderTime);
    }
    if(cloud.m_compoundId4 != NULL_COMPOUND) {
        diffuse(.007, cloud.m_oldDens4, cloud.m_density4, renderTime);
        // Move the compound clouds about the velocity field.
        advect(cloud.m_oldDens4, cloud.m_density4, renderTime);
    }

    // Store the pixel data in a hardware buffer for quick access.
    auto pixelBuffer = cloud.m_texture->getBuffer();

    pixelBuffer->lock(Ogre::v1::HardwareBuffer::HBL_DISCARD);
    const Ogre::PixelBox& pixelBox = pixelBuffer->getCurrentLock();
    uint8_t* pDest = static_cast<uint8_t*>(pixelBox.data);

    const size_t rowBytes =
        pixelBox.rowPitch * OGRE_CLOUD_TEXTURE_BYTES_PER_ELEMENT;

    // Copy the density vector into the buffer.


    // This is probably branch predictor friendly to move each bunch of pixels
    // separately

    // First channel
    if(cloud.m_compoundId1 != NULL_COMPOUND)
        fillCloudChannel(cloud.m_density1, 0, rowBytes, pDest);
    if(cloud.m_compoundId2 != NULL_COMPOUND)
        fillCloudChannel(cloud.m_density2, 1, rowBytes, pDest);
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
    for(int j = 0; j < height; j++) {
        for(int i = 0; i < width; i++) {
            // Flipping in y-direction
            // TODO: check is this uint8_t conversion better than clamping
            int intensity = static_cast<int>(density[i][height - j - 1]);

            // TODO: can this be removed as this probably causes some
            // performance concerns by being here
            std::clamp(intensity, 0, 255);

            // This can be used to debug the clouds
            // if(index == 1)
            //     intensity += 70;
            // if(intensity > 0)
            //    intensity = 140;
            pDest[rowBytes * j + (i * OGRE_CLOUD_TEXTURE_BYTES_PER_ELEMENT) +
                  index] = intensity;
        }
    }
}


void
    CompoundCloudSystem::CreateVelocityField()
{
    float nxScale = noiseScale;
    float nyScale = nxScale * float(width) / float(height);
    float x0, y0, x1, y1, n0, n1, nx, ny;

    for(int x = 0; x < width; x++) {
        for(int y = 0; y < height; y++) {
            x0 = (float(x - 1) / float(width)) * nxScale;
            y0 = (float(y - 1) / float(height)) * nyScale;
            x1 = (float(x + 1) / float(width)) * nxScale;
            y1 = (float(y + 1) / float(height)) * nyScale;

            n0 = fieldPotential.noise(x0, y0, 0);
            n1 = fieldPotential.noise(x1, y0, 0);
            ny = n0 - n1;
            n0 = fieldPotential.noise(x0, y0, 0);
            n1 = fieldPotential.noise(x0, y1, 0);
            nx = n1 - n0;

            xVelocity[x][y] = nx / 2;
            yVelocity[x][y] = ny / 2;
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

    for(int x = 1; x < width - 1; x++) {
        for(int y = 1; y < height - 1; y++) {
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
    for(int x = 0; x < width; x++) {
        for(int y = 0; y < height; y++) {
            density[x][y] = 0;
        }
    }

    float dx, dy;
    int x0, x1, y0, y1;
    float s1, s0, t1, t0;
    for(int x = 1; x < width - 1; x++) {
        for(int y = 1; y < height - 1; y++) {
            if(oldDens[x][y] > 1) {
                dx = x + dt * xVelocity[x][y];
                dy = y + dt * yVelocity[x][y];

                if(dx < 0.5)
                    dx = 0.5;
                if(dx > width - 1.5)
                    dx = width - 1.5f;

                if(dy < 0.5)
                    dy = 0.5;
                if(dy > height - 1.5)
                    dy = height - 1.5f;

                x0 = static_cast<int>(dx);
                x1 = x0 + 1;
                y0 = static_cast<int>(dy);
                y1 = y0 + 1;

                s1 = dx - x0;
                s0 = 1 - s1;
                t1 = dy - y0;
                t0 = 1 - t1;

                density[x0][y0] += oldDens[x][y] * s0 * t0;
                density[x0][y1] += oldDens[x][y] * s0 * t1;
                density[x1][y0] += oldDens[x][y] * s1 * t0;
                density[x1][y1] += oldDens[x][y] * s1 * t1;
            }
        }
    }
}
