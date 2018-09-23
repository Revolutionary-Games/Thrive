#include "membrane_system.h"

#include <OgreMaterial.h>
#include <OgreMaterialManager.h>
#include <OgreMesh2.h>
#include <OgreMeshManager2.h>
#include <OgreRoot.h>
#include <OgreSceneManager.h>
#include <OgreSubMesh2.h>
#include <OgreTechnique.h>
#include <OgreTextureUnitState.h>

#include <atomic>

using namespace thrive;

////////////////////////////////////////////////////////////////////////////////
// Membrane Component
////////////////////////////////////////////////////////////////////////////////
static std::atomic<int> MembraneMeshNumber = {0};
std::atomic<int> MembraneComponent::membraneNumber = {0};

MembraneComponent::MembraneComponent(MEMBRANE_TYPE type) :
    Leviathan::Component(TYPE)
{
    // membrane type
    membraneType = type;
    // Create the mesh for rendering us
    m_mesh = Ogre::MeshManager::getSingleton().createManual(
        "MembraneMesh_" + std::to_string(++MembraneMeshNumber),
        Ogre::ResourceGroupManager::DEFAULT_RESOURCE_GROUP_NAME);

    m_subMesh = m_mesh->createSubMesh();
}

MembraneComponent::~MembraneComponent()
{
    LEVIATHAN_ASSERT(!m_item, "MembraneComponent not released");

    Ogre::MeshManager::getSingleton().remove(m_mesh);
    m_mesh.reset();
    m_subMesh = nullptr;

    if(coloredMaterial) {

        Ogre::MaterialManager::getSingleton().remove(coloredMaterial);
        coloredMaterial.reset();
    }
}

void
    MembraneComponent::setMembraneType(MEMBRANE_TYPE type)
{
    membraneType = static_cast<MEMBRANE_TYPE>(type);
}

MEMBRANE_TYPE
MembraneComponent::getMembraneType()
{
    return membraneType;
}

void
    MembraneComponent::Release(Ogre::SceneManager* scene)
{
    releaseOgreResourcesForClear(scene);
}
// ------------------------------------ //
Ogre::Vector3
    MembraneComponent::FindClosestOrganelles(Ogre::Vector3 target)
{
    // The distance we want the membrane to be from the organelles squared.
    double closestSoFar = 4;
    int closestIndex = -1;

    for(size_t i = 0, end = organellePositions.size(); i < end; i++) {
        double lenToObject = target.squaredDistance(organellePositions[i]);

        if(lenToObject < 4 && lenToObject < closestSoFar) {
            closestSoFar = lenToObject;

            closestIndex = i;
        }
    }

    if(closestIndex != -1)
        return (organellePositions[closestIndex]);
    else
        return Ogre::Vector3(0, 0, -1);
}

Ogre::Vector3
    MembraneComponent::GetMovement(Ogre::Vector3 target,
        Ogre::Vector3 closestOrganelle)
{
    double power = pow(2.7, (-target.distance(closestOrganelle)) / 10) / 50;

    return (Ogre::Vector3(closestOrganelle) - Ogre::Vector3(target)) * power;
}

Ogre::Vector3
    MembraneComponent::GetExternalOrganelle(double x, double y)
{
    if(vertices2D.empty())
        LOG_WARNING("MembraneComponent: GetExternalOrganelle: called before "
                    "membrane is initialized. Returning 0, 0");

    float organelleAngle = Ogre::Math::ATan2(y, x).valueRadians();

    Ogre::Vector3 closestSoFar(0, 0, 0);
    float angleToClosest = Ogre::Math::TWO_PI;

    for(const auto& vertex : vertices2D) {
        if(Ogre::Math::Abs(
               Ogre::Math::ATan2(vertex.y, vertex.x).valueRadians() -
               organelleAngle) < angleToClosest) {
            closestSoFar = Ogre::Vector3(vertex.x, vertex.y, 0);
            angleToClosest = Ogre::Math::Abs(
                Ogre::Math::ATan2(vertex.y, vertex.x).valueRadians() -
                organelleAngle);
        }
    }

    // Swap to world coordinates from internal membrane coordinates
    return Ogre::Vector3(closestSoFar.x, 0, closestSoFar.y);
}

bool
    MembraneComponent::contains(float x, float y)
{
    bool crosses = false;

    int n = vertices2D.size();
    for(int i = 0; i < n - 1; i++) {
        if((vertices2D[i].y <= y && y < vertices2D[i + 1].y) ||
            (vertices2D[i + 1].y <= y && y < vertices2D[i].y)) {
            if(x < (vertices2D[i + 1].x - vertices2D[i].x) *
                           (y - vertices2D[i].y) /
                           (vertices2D[i + 1].y - vertices2D[i].y) +
                       vertices2D[i].x) {
                crosses = !crosses;
            }
        }
    }

    return crosses;
}

float
    MembraneComponent::calculateEncompassingCircleRadius() const
{
    float distanceSquared = 0;

    for(const auto& vertex : vertices2D) {

        const auto currentDistance = vertex.squaredLength();
        if(currentDistance >= distanceSquared)
            distanceSquared = currentDistance;
    }

    return std::sqrt(distanceSquared);
}

// ------------------------------------ //
//! Should set the colour of the membrane once working
void
    MembraneComponent::setColour(const Float4& value)
{
    colour = value;
    // Desaturate it here so it looks nicer (could implement as method thatcould
    // be called i suppose)
    Ogre::Real saturation;
    Ogre::Real brightness;
    Ogre::Real hue;
    colour.getHSB(&hue, &saturation, &brightness);
    colour.setHSB(hue, saturation * .75, brightness);

    // If we already have created a material we need to re-apply it
    if(coloredMaterial) {
        coloredMaterial->getTechnique(0)
            ->getPass(0)
            ->getFragmentProgramParameters()
            ->setNamedConstant("membraneColour", colour);
        coloredMaterial->getTechnique(0)
            ->getPass(0)
            ->getTextureUnitState(0)
            ->setHardwareGammaEnabled(true);
        coloredMaterial->compile();
    }
}

Float4
    MembraneComponent::getColour() const
{
    return colour;
}
// ------------------------------------ //
void
    MembraneComponent::Update(Ogre::SceneManager* scene,
        Ogre::SceneNode* parentcomponentpos)
{
    if(clearNeeded) {

        releaseOgreResourcesForClear(scene);
        clearNeeded = false;
    }

    // Skip if the mesh is already created //
    if(isInitialized)
        return;

    if(!isInitialized)
        Initialize();

    LEVIATHAN_ASSERT(!m_item,
        "Membrane code should always recreate item but it is already created.");

    // This is a triangle strip so we only need 2 + n vertices
    const auto bufferSize = vertices2D.size() + 2;

    Ogre::RenderSystem* renderSystem =
        Ogre::Root::getSingleton().getRenderSystem();
    Ogre::VaoManager* vaoManager = renderSystem->getVaoManager();

    Ogre::VertexElement2Vec vertexElements;
    vertexElements.push_back(
        Ogre::VertexElement2(Ogre::VET_FLOAT3, Ogre::VES_POSITION));
    vertexElements.push_back(
        Ogre::VertexElement2(Ogre::VET_FLOAT2, Ogre::VES_TEXTURE_COORDINATES));
    // vertexElements.push_back(Ogre::VertexElement2(Ogre::VET_FLOAT3,
    // Ogre::VES_NORMAL));

    // TODO: make this static (will probably need a buffer alternative for
    // generating the vertex data that will be then referenced here instead of
    // nullptr)
    Ogre::VertexBufferPacked* vertexBuffer = vaoManager->createVertexBuffer(
        vertexElements, bufferSize, Ogre::BT_DYNAMIC_DEFAULT, nullptr, false);

    Ogre::VertexBufferPackedVec vertexBuffers;
    vertexBuffers.push_back(vertexBuffer);

    // 1 to 1 index buffer mapping

    Ogre::uint16* indices = reinterpret_cast<Ogre::uint16*>(OGRE_MALLOC_SIMD(
        sizeof(Ogre::uint16) * bufferSize, Ogre::MEMCATEGORY_GEOMETRY));

    for(size_t i = 0; i < bufferSize; ++i) {

        indices[i] = static_cast<Ogre::uint16>(i);
    }

    // TODO: check if this is needed (when a 1 to 1 vertex and index mapping
    // is used)
    Ogre::IndexBufferPacked* indexBuffer = nullptr;

    try {
        indexBuffer = vaoManager->createIndexBuffer(
            Ogre::IndexBufferPacked::IT_16BIT, bufferSize, Ogre::BT_IMMUTABLE,
            // Could this be false like the vertex buffer to not keep a
            // shadow buffer
            indices, true);
    } catch(const Ogre::Exception& e) {

        // Avoid memory leak
        OGRE_FREE_SIMD(indices, Ogre::MEMCATEGORY_GEOMETRY);
        indexBuffer = nullptr;
        throw e;
    }

    Ogre::VertexArrayObject* vao = vaoManager->createVertexArrayObject(
        vertexBuffers, indexBuffer, Ogre::OT_TRIANGLE_FAN);

    m_subMesh->mVao[Ogre::VpNormal].push_back(vao);

    // This might be needed because we use a v2 mesh
    // Use the same geometry for shadow casting.
    // If m_item->setCastShadows(false); is set then this isn't needed
    m_subMesh->mVao[Ogre::VpShadow].push_back(vao);


    // Set the bounds to get frustum culling and LOD to work correctly.
    // TODO: make this more accurate by calculating the actual extents
    m_mesh->_setBounds(
        Ogre::Aabb(Ogre::Vector3::ZERO, Ogre::Vector3::UNIT_SCALE * 50)
        /*, false*/);
    m_mesh->_setBoundingSphereRadius(50);

    // Set the membrane material //
    // We need to create a new instance until the managing is moved to the
    // species (allowing the same species to share)
    if(!coloredMaterial) {
        Ogre::MaterialPtr baseMaterial = chooseMaterialByType();
        // TODO: find a way for the species to manage this to
        // avoid having tons of materials Maybe Use the species's
        // name instead. and let something like the
        // SpeciesComponent create and destroy this
        coloredMaterial = baseMaterial->clone(
            "Membrane_instance_" + std::to_string(++membraneNumber));

        coloredMaterial->getTechnique(0)
            ->getPass(0)
            ->getFragmentProgramParameters()
            ->setNamedConstant("membraneColour", colour);
        coloredMaterial->compile();
    }

    m_subMesh->setMaterialName(coloredMaterial->getName());

    // Update mesh data //
    // Map the buffer for writing //
    // DO NOT READ FROM THE MAPPED BUFFER
    MembraneVertex* RESTRICT_ALIAS meshVertices =
        reinterpret_cast<MembraneVertex * RESTRICT_ALIAS>(
            vertexBuffer->map(0, vertexBuffer->getNumElements()));

    // Creates a 3D prism from the 2D vertices.

    // initialize membrane
    size_t writeIndex = 0;
    writeIndex = InitializeCorrectMembrane(writeIndex, meshVertices);

    // This can be commented out when this works correctly, or maybe a
    // different macro for debug builds to include this check could
    // work, but it has to also work on linux
    LEVIATHAN_ASSERT(writeIndex == bufferSize, "Invalid array element math in "
                                               "fill vertex buffer");

    // Upload finished data to the gpu (unmap all needs to be used to
    // suppress warnings about destroying mapped buffers)
    vertexBuffer->unmap(Ogre::UO_UNMAP_ALL);

    // This needs the v2 mesh to contain data to work
    m_item = scene->createItem(m_mesh, Ogre::SCENE_DYNAMIC);
    m_item->setRenderQueueGroup(Leviathan::DEFAULT_RENDER_QUEUE);
    parentcomponentpos->attachObject(m_item);
}

void
    MembraneComponent::DrawCorrectMembrane()
{
    switch(membraneType) {
    case MEMBRANE_TYPE::MEMBRANE: DrawMembrane(); break;
    case MEMBRANE_TYPE::WALL: DrawCellWall(); break;
    case MEMBRANE_TYPE::CHITIN: DrawCellWall(); break;
    }
}

size_t
    MembraneComponent::InitializeCorrectMembrane(size_t writeIndex,
        MembraneVertex* meshVertices)
{
    // All of these floats were originally doubles. But to have more
    // performance they are now floats

    // common variables
    float height = .1;
    const Ogre::Vector2 center(0.5, 0.5);

    switch(membraneType) {
    case MEMBRANE_TYPE::MEMBRANE:
        meshVertices[writeIndex++] = {Ogre::Vector3(0, height / 2, 0), center};

        for(size_t i = 0, end = vertices2D.size(); i < end + 1; i++) {
            // Finds the UV coordinates be projecting onto a plane and
            // stretching to fit a circle.

            const double currentRadians = 2.0 * 3.1416 * i / end;

            meshVertices[writeIndex++] = {
                Ogre::Vector3(vertices2D[i % end].x,
                    vertices2D[i % end].z + height / 2, vertices2D[i % end].y),
                center +
                    Ogre::Vector2(cos(currentRadians), sin(currentRadians)) /
                        2};
        }
        break;
    case MEMBRANE_TYPE::WALL:
    case MEMBRANE_TYPE::CHITIN:
        // cell walls need obvious inner/outer memrbranes (we can worry about
        // chitin later)
        height = .05;
        meshVertices[writeIndex++] = {Ogre::Vector3(0, height / 2, 0), center};

        for(size_t i = 0, end = vertices2D.size(); i < end + 1; i++) {
            // Finds the UV coordinates be projecting onto a plane and
            // stretching to fit a circle.
            const double currentRadians = 3.1416 * i / end;
            meshVertices[writeIndex++] = {
                Ogre::Vector3(vertices2D[i % end].x,
                    vertices2D[i % end].z + height / 2, vertices2D[i % end].y),
                center +
                    Ogre::Vector2(cos(currentRadians), sin(currentRadians)) /
                        2};
        }
        break;
    }


    return writeIndex;
}

Ogre::MaterialPtr
    MembraneComponent::chooseMaterialByType()
{
    switch(membraneType) {
    case MEMBRANE_TYPE::MEMBRANE:
        return Ogre::MaterialManager::getSingleton().getByName("Membrane");
        break;
    case MEMBRANE_TYPE::WALL:
        return Ogre::MaterialManager::getSingleton().getByName("cellwall");
        break;
    case MEMBRANE_TYPE::CHITIN:
        return Ogre::MaterialManager::getSingleton().getByName("cellwall");
        break;
    }
    // default
    return Ogre::MaterialManager::getSingleton().getByName("cellwall");
}

void
    MembraneComponent::Initialize()
{
    for(Ogre::Vector3 pos : organellePositions) {
        if(abs(pos.x) + 1 > cellDimensions) {
            cellDimensions = abs(pos.x) + 1;
        }
        if(abs(pos.y) + 1 > cellDimensions) {
            cellDimensions = abs(pos.y) + 1;
        }
    }

    for(int i = membraneResolution; i > 0; i--) {
        vertices2D.emplace_back(-cellDimensions,
            cellDimensions - 2 * cellDimensions / membraneResolution * i, 0);
    }
    for(int i = membraneResolution; i > 0; i--) {
        vertices2D.emplace_back(
            cellDimensions - 2 * cellDimensions / membraneResolution * i,
            cellDimensions, 0);
    }
    for(int i = membraneResolution; i > 0; i--) {
        vertices2D.emplace_back(cellDimensions,
            -cellDimensions + 2 * cellDimensions / membraneResolution * i, 0);
    }
    for(int i = membraneResolution; i > 0; i--) {
        vertices2D.emplace_back(
            -cellDimensions + 2 * cellDimensions / membraneResolution * i,
            -cellDimensions, 0);
    }

    // Does this need to run 40*cellDimensions times. That seems to be
    // (reduced from 50 to 40 times, can probabbly be reduced more)
    for(int i = 0; i < 40 * cellDimensions; i++) {
        DrawCorrectMembrane();
    }

    // Subdivide();


    isInitialized = true;
}


// ------------------------------------ //
void
    MembraneComponent::DrawMembrane()
{
    // Stores the temporary positions of the membrane.
    auto newPositions = vertices2D;

    // Loops through all the points in the membrane and relocates them as
    // necessary.
    for(size_t i = 0, end = newPositions.size(); i < end; i++) {
        Ogre::Vector3 closestOrganelle = FindClosestOrganelles(vertices2D[i]);
        if(closestOrganelle == Ogre::Vector3(0, 0, -1)) {
            newPositions[i] =
                (vertices2D[(end + i - 1) % end] + vertices2D[(i + 1) % end]) /
                2;
        } else {
            Ogre::Vector3 movementDirection =
                GetMovement(vertices2D[i], closestOrganelle);
            newPositions[i].x -= movementDirection.x;
            newPositions[i].y -= movementDirection.y;
        }
    }

    // Allows for the addition and deletion of points in the membrane.
    for(size_t i = 0; i < newPositions.size() - 1; i++) {
        // Check to see if the gap between two points in the membrane is too
        // big.
        if(newPositions[i].distance(
               newPositions[(i + 1) % newPositions.size()]) >
            cellDimensions / membraneResolution) {
            // Add an element after the ith term that is the average of the i
            // and i+1 term.
            auto it = newPositions.begin();
            Ogre::Vector3 tempPoint =
                (newPositions[(i + 1) % newPositions.size()] +
                    newPositions[i]) /
                2;
            newPositions.insert(it + i + 1, tempPoint);

            i++;
        }

        // Check to see if the gap between two points in the membrane is too
        // small.
        if(newPositions[(i + 1) % newPositions.size()].distance(
               newPositions[(i - 1) % newPositions.size()]) <
            cellDimensions / membraneResolution) {
            // Delete the ith term.
            auto it = newPositions.begin();
            newPositions.erase(it + i);
        }
    }

    vertices2D = newPositions;
}

void
    MembraneComponent::sendOrganelles(double x, double y)
{
    organellePositions.emplace_back(x, y, 0);
}

bool
    MembraneComponent::removeSentOrganelle(double x, double y)
{
    for(auto iter = organellePositions.begin();
        iter != organellePositions.end(); ++iter) {

        if(iter->x == x && iter->y == y) {
            organellePositions.erase(iter);
            return true;
        }
    }

    return false;
}

void
    MembraneComponent::clear()
{
    clearNeeded = true;
}

void
    MembraneComponent::releaseOgreResourcesForClear(Ogre::SceneManager* scene)
{
    isInitialized = false;
    vertices2D.clear();

    if(m_item) {
        scene->destroyItem(m_item);
        m_item = nullptr;
    }

    if(m_mesh) {

        // If there is nothing in the mesh there isn't anything to destroy
        if(!m_subMesh->mVao[Ogre::VpNormal].empty()) {

            Ogre::RenderSystem* renderSystem =
                Ogre::Root::getSingleton().getRenderSystem();
            Ogre::VaoManager* vaoManager = renderSystem->getVaoManager();

            // Delete the index and vertex buffers
            Ogre::VertexArrayObject* vao =
                m_subMesh->mVao[Ogre::VpNormal].front();
            Ogre::IndexBufferPacked* indexBuffer = vao->getIndexBuffer();
            Ogre::VertexBufferPacked* vertexBuffer =
                vao->getVertexBuffers().front();

            vaoManager->destroyVertexArrayObject(vao);
            vaoManager->destroyIndexBuffer(indexBuffer);
            vaoManager->destroyVertexBuffer(vertexBuffer);

            // And make sure they aren't used
            m_subMesh->mVao[Ogre::VpNormal].clear();
            m_subMesh->mVao[Ogre::VpShadow].clear();
        }
    }
}

/*
Cell Wall Code Here
*/

// this is where the magic happens i think
Ogre::Vector3
    MembraneComponent::GetMovementForCellWall(Ogre::Vector3 target,
        Ogre::Vector3 closestOrganelle)
{
    double power = pow(2.7, (-target.distance(closestOrganelle) * 2) / 10) / 40;

    return (Ogre::Vector3(closestOrganelle) - Ogre::Vector3(target)) * power;
}

void
    MembraneComponent::DrawCellWall()
{
    // Stores the temporary positions of the membrane.
    auto newPositions = vertices2D;

    // Loops through all the points in the membrane and relocates them as
    // necessary.
    for(size_t i = 0, end = newPositions.size(); i < end; i++) {
        Ogre::Vector3 closestOrganelle = FindClosestOrganelles(vertices2D[i]);
        if(closestOrganelle == Ogre::Vector3(0, 0, -1)) {
            newPositions[i] =
                (vertices2D[(end + i - 1) % end] + vertices2D[(i + 1) % end]) /
                2;
        } else {
            Ogre::Vector3 movementDirection =
                GetMovementForCellWall(vertices2D[i], closestOrganelle);
            newPositions[i].x -= movementDirection.x;
            newPositions[i].y -= movementDirection.y;
        }
    }

    // Allows for the addition and deletion of points in the membrane.
    for(size_t i = 0; i < newPositions.size() - 1; i++) {
        // Check to see if the gap between two points in the membrane is too
        // big.
        if(newPositions[i].distance(
               newPositions[(i + 1) % newPositions.size()]) *
                2 >
            (cellDimensions / membraneResolution)) {
            // Add an element after the ith term that is the average of the i
            // and i+1 term.
            auto it = newPositions.begin();
            Ogre::Vector3 tempPoint =
                (newPositions[(i + 1) % newPositions.size()] +
                    newPositions[i]) /
                2;
            newPositions.insert(it + i + 1, tempPoint);
            i++;
        }

        // Check to see if the gap between two points in the membrane is too
        // small.
        if(newPositions[(i + 1) % newPositions.size()].distance(
               newPositions[(i - 1) % newPositions.size()]) <
            cellDimensions / membraneResolution) {
            // Delete the ith term.
            auto it = newPositions.begin();
            newPositions.erase(it + i);
        }

        // Check to see if the gap between two points in the membrane is too
        // small.
        if(newPositions[(i + 1) % newPositions.size()].distance(
               newPositions[(i - 1) % newPositions.size()]) <
            cellDimensions / membraneResolution) {
            // Delete the ith term.
            auto it = newPositions.begin();
            newPositions.erase(it + i);
        }
    }

    vertices2D = newPositions;
}
