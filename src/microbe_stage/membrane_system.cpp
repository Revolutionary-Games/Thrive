#include "membrane_system.h"

#include <OgreMaterial.h>
#include <OgreMaterialManager.h>
#include <OgreMesh2.h>
#include <OgreMeshManager2.h>
#include <OgreRoot.h>
#include <OgreSceneManager.h>
#include <OgreSubMesh2.h>
#include <OgreTechnique.h>

#include <atomic>

using namespace thrive;

////////////////////////////////////////////////////////////////////////////////
// Membrane Component
////////////////////////////////////////////////////////////////////////////////
static std::atomic<int> MembraneMeshNumber = {0};
std::atomic<int> MembraneComponent::membraneNumber = {0};

MembraneComponent::MembraneComponent() : Leviathan::Component(TYPE)
{
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
    MembraneComponent::Release(Ogre::SceneManager* scene)
{

    if(m_item) {
        scene->destroyItem(m_item);
        m_item = nullptr;
    }
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

    float organelleAngle = Ogre::Math::ATan2(y, x).valueRadians();

    Ogre::Vector3 closestSoFar(0, 0, 0);
    float angleToClosest = Ogre::Math::TWO_PI;

    for(size_t i = 0, end = vertices2D.size(); i < end; i++) {
        if(Ogre::Math::Abs(Ogre::Math::ATan2(vertices2D[i].y, vertices2D[i].x)
                               .valueRadians() -
                           organelleAngle) < angleToClosest) {
            closestSoFar = Ogre::Vector3(vertices2D[i].x, vertices2D[i].y, 0);
            angleToClosest = Ogre::Math::Abs(
                Ogre::Math::ATan2(vertices2D[i].y, vertices2D[i].x)
                    .valueRadians() -
                organelleAngle);
        }
    }

    return closestSoFar;
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

    // If we already have created a material we need to apply it
    if(coloredMaterial) {
        coloredMaterial->getTechnique(0)
            ->getPass(0)
            ->getFragmentProgramParameters()
            ->setNamedConstant("membraneColour", colour);
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
    // Skip if the mesh is already created //
    if(isInitialized)
        return;

    if(!isInitialized)
        Initialize();

    DrawMembrane();

    // 12 vertices added per index of vertices2D
    const auto bufferSize = vertices2D.size() * 12;

    if(!m_vertexBuffer) {

        Ogre::RenderSystem* renderSystem =
            Ogre::Root::getSingleton().getRenderSystem();
        Ogre::VaoManager* vaoManager = renderSystem->getVaoManager();

        Ogre::VertexElement2Vec vertexElements;
        vertexElements.push_back(
            Ogre::VertexElement2(Ogre::VET_FLOAT3, Ogre::VES_POSITION));
        vertexElements.push_back(Ogre::VertexElement2(
            Ogre::VET_FLOAT2, Ogre::VES_TEXTURE_COORDINATES));
        // vertexElements.push_back(Ogre::VertexElement2(Ogre::VET_FLOAT3,
        // Ogre::VES_NORMAL));

        m_vertexBuffer = vaoManager->createVertexBuffer(vertexElements,
            bufferSize, Ogre::BT_DYNAMIC_PERSISTENT, nullptr, false);

        Ogre::VertexBufferPackedVec vertexBuffers;
        vertexBuffers.push_back(m_vertexBuffer);

        // 1 to 1 index buffer mapping

        Ogre::uint16* indices =
            reinterpret_cast<Ogre::uint16*>(OGRE_MALLOC_SIMD(
                sizeof(Ogre::uint16) * bufferSize, Ogre::MEMCATEGORY_GEOMETRY));

        for(size_t i = 0; i < bufferSize; ++i) {

            indices[i] = static_cast<Ogre::uint16>(i);
        }

        // TODO: check if this is needed (when a 1 to 1 vertex and index mapping
        // is used)
        Ogre::IndexBufferPacked* indexBuffer = nullptr;

        try {
            indexBuffer =
                vaoManager->createIndexBuffer(Ogre::IndexBufferPacked::IT_16BIT,
                    bufferSize, Ogre::BT_IMMUTABLE,
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
            vertexBuffers, indexBuffer, Ogre::OT_TRIANGLE_LIST);

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
            Ogre::MaterialPtr baseMaterial =
                Ogre::MaterialManager::getSingleton().getByName("Membrane");

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
    }

    // Map the buffer for writing //
    // DO NOT READ FROM THE MAPPED BUFFER
    MembraneVertex* RESTRICT_ALIAS meshVertices =
        reinterpret_cast<MembraneVertex * RESTRICT_ALIAS>(
            m_vertexBuffer->map(0, m_vertexBuffer->getNumElements()));


    // Update mesh data //

    // Creates a 3D prism from the 2D vertices.

    // All of these floats were originally doubles. But to have more
    // performance they are now floats
    float height = .1;

    size_t writeIndex = 0;

    for(size_t i = 0, end = vertices2D.size(); i < end; i++) {
        // Finds the UV coordinates be projecting onto a plane and stretching to
        // fit a circle.
        const float x = vertices2D[i].x;
        const float y = vertices2D[i].y;
        const float z = vertices2D[i].z;

        const float ray = x * x + y * y + z * z;

        const float t = Ogre::Math::Sqrt(ray) / (2.0 * ray);
        const float a = t * x;
        const float b = t * y;
        // const float c = t*z;

        const Ogre::Vector2 uv(a + 0.5, b + 0.5);

        const Ogre::Vector2 center(0.5, 0.5);
        const double currentRadians = 2.0 * 3.1416 * i / end;
        const double nextRadians = 2.0 * 3.1416 * (i + 1) / end;

        // y and z coordinates are swapped to match the Ogre up direction

        // Bottom (or top?) half first triangle
        meshVertices[writeIndex++] = {Ogre::Vector3(0, 0, 0), uv};

        meshVertices[writeIndex++] = {
            Ogre::Vector3(vertices2D[(i + 1) % end].x,
                vertices2D[(i + 1) % end].z - height / 2,
                vertices2D[(i + 1) % end].y),
            uv};

        meshVertices[writeIndex++] = {
            Ogre::Vector3(vertices2D[i % end].x,
                vertices2D[i % end].z - height / 2, vertices2D[i % end].y),
            uv};

        // Second triangle
        meshVertices[writeIndex++] = {
            Ogre::Vector3(vertices2D[i % end].x,
                vertices2D[i % end].z + height / 2, vertices2D[i % end].y),
        };

        meshVertices[writeIndex++] = {
            Ogre::Vector3(vertices2D[(i + 1) % end].x,
                vertices2D[(i + 1) % end].z + height / 2,
                vertices2D[(i + 1) % end].y),
            uv};

        meshVertices[writeIndex++] = {
            Ogre::Vector3(vertices2D[(i + 1) % end].x,
                vertices2D[(i + 1) % end].z - height / 2,
                vertices2D[(i + 1) % end].y),
            uv};

        // This was originally a second loop
        // Top half first triangle
        // This seems to be the only one that is actually drawn to the screen,
        // at least with the current test membrane.
        meshVertices[writeIndex++] = {
            Ogre::Vector3(vertices2D[i % end].x,
                vertices2D[i % end].z + height / 2, vertices2D[i % end].y),
            center +
                Ogre::Vector2(cos(currentRadians), sin(currentRadians)) / 2};

        meshVertices[writeIndex++] = {Ogre::Vector3(0, height / 2, 0), center};

        meshVertices[writeIndex++] = {
            Ogre::Vector3(vertices2D[(i + 1) % end].x,
                vertices2D[(i + 1) % end].z + height / 2,
                vertices2D[(i + 1) % end].y),
            center + Ogre::Vector2(cos(nextRadians), sin(nextRadians)) / 2};

        // Second triangle
        meshVertices[writeIndex++] = {
            Ogre::Vector3(vertices2D[i % end].x,
                vertices2D[i % end].z - height / 2, vertices2D[i % end].y),
            uv};

        meshVertices[writeIndex++] = {
            Ogre::Vector3(vertices2D[(i + 1) % end].x,
                vertices2D[(i + 1) % end].z - height / 2,
                vertices2D[(i + 1) % end].y),
            uv};

        meshVertices[writeIndex++] = {Ogre::Vector3(0, -height / 2, 0), uv};
    }

    // LOG_INFO("Write index is: " + std::to_string(writeIndex) + ", buffer
    // size: " +
    //     std::to_string(bufferSize));
    // This can be commented out when this works correctly, or maybe a
    // different macro for debug builds to include this check could
    // work, but it has to also work on linux
    LEVIATHAN_ASSERT(writeIndex == bufferSize, "Invalid array element math in "
                                               "fill vertex buffer");

    // Upload finished data to the gpu (unmap all needs to be used to
    // suppress warnings about destroying mapped buffers)
    m_vertexBuffer->unmap(Ogre::UO_UNMAP_ALL);

    // TODO: apply the current colour to the material instance


    if(!m_item) {
        // This needs the v2 mesh to contain data to work
        m_item = scene->createItem(m_mesh, Ogre::SCENE_DYNAMIC);
        m_item->setRenderQueueGroup(Leviathan::DEFAULT_RENDER_QUEUE);
        parentcomponentpos->attachObject(m_item);
    }
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

    for(int i = 0; i < membraneResolution; i++) {
        vertices2D.emplace_back(
            -cellDimensions + 2 * cellDimensions / membraneResolution * i,
            -cellDimensions, 0);
    }
    for(int i = 0; i < membraneResolution; i++) {
        vertices2D.emplace_back(cellDimensions,
            -cellDimensions + 2 * cellDimensions / membraneResolution * i, 0);
    }
    for(int i = 0; i < membraneResolution; i++) {
        vertices2D.emplace_back(
            cellDimensions - 2 * cellDimensions / membraneResolution * i,
            cellDimensions, 0);
    }
    for(int i = 0; i < membraneResolution; i++) {
        vertices2D.emplace_back(-cellDimensions,
            cellDimensions - 2 * cellDimensions / membraneResolution * i, 0);
    }

    // Does this need to run 50*cellDimensions times. That seems to be
    // really high and probably causes some of the lag
    for(int i = 0; i < 50 * cellDimensions; i++) {
        DrawMembrane();
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

void
    MembraneComponent::clear()
{
    isInitialized = false;
    vertices2D.clear();

    if(m_item)
        m_item->detachFromParent();
}

////////////////////////////////////////////////////////////////////////////////
// MembraneSystem
////////////////////////////////////////////////////////////////////////////////
// void
// MembraneSystem::update(int, int) {
//     m_impl->m_entities.clearChanges();
//     for (auto& value : m_impl->m_entities) {
//         MembraneComponent* membraneComponent = std::get<0>(value.second);
//         OgreSceneNodeComponent* sceneNodeComponent =
//         std::get<1>(value.second);

//         if (membraneComponent->wantsMembrane &&
//         sceneNodeComponent->m_meshName.get().find("membrane") !=
//         std::string::npos)
//         {
//             membraneComponent->wantsMembrane = false;
//             // Get the vertex positions of the membrane.
//             if(!membraneComponent->isInitialized)
//             {
//                 membraneComponent->Initialize();
//             }
//             membraneComponent->Update();

//             membraneComponent->m_meshName =
//             sceneNodeComponent->m_meshName.get();

//             //If the mesh already exists, destroy the old one
//             Ogre::MeshManager::getSingleton().remove(sceneNodeComponent->m_meshName.get());
//             // Create a mesh and a submesh.
//             Ogre::MeshPtr msh =
//             Ogre::MeshManager::getSingleton().createManual(sceneNodeComponent->m_meshName.get(),
//             "General"); Ogre::SubMesh* sub = msh->createSubMesh();

//             // Define the vertices.
//             std::vector<double> vertexData;
//             for(size_t i=0, end=membraneComponent->MeshPoints.size(); i<end;
//             i++)
//             {
//                 // Vertex.
//                 vertexData.push_back(membraneComponent->MeshPoints[i].x);
//                 vertexData.push_back(membraneComponent->MeshPoints[i].y);
//                 vertexData.push_back(membraneComponent->MeshPoints[i].z);

//                 // Normal.
//                 //vertexData.push_back(component->MyMembrane.Normals[i].x);
//                 //vertexData.push_back(component->MyMembrane.Normals[i].y);
//                 //vertexData.push_back(component->MyMembrane.Normals[i].z);
//                 vertexData.push_back(0.0);
//                 vertexData.push_back(0.0);
//                 vertexData.push_back(1.0);

//                 // UV coordinates.
//                 vertexData.push_back(membraneComponent->UVs[i].x);
//                 vertexData.push_back(membraneComponent->UVs[i].y);
//             }

//             // Populate the vertex buffer.
//             const size_t vertexBufferSize = vertexData.size();
//             float vertices[vertexBufferSize];
//             for(size_t i=0; i<vertexBufferSize; i++)
//             {
//                 vertices[i] = vertexData[i];
//             }

//             // Populate the index buffer.
//             const size_t indexBufferSize = vertexData.size()/8;
//             unsigned short faces[indexBufferSize];
//             for(size_t i=0, end=indexBufferSize; i<end; i++)
//             {
//                 faces[i]=i;
//             }

//             // Create vertex data structure for 8 vertices shared between
//             submeshes. msh->sharedVertexData = new Ogre::VertexData();
//             msh->sharedVertexData->vertexCount = vertexData.size()/8;

//             /// Create declaration (memory format) of vertex data
//             Ogre::VertexDeclaration* decl =
//             msh->sharedVertexData->vertexDeclaration; size_t offset = 0;
//             // 1st buffer
//             decl->addElement(0, offset, Ogre::VET_FLOAT3,
//             Ogre::VES_POSITION); offset +=
//             Ogre::VertexElement::getTypeSize(Ogre::VET_FLOAT3);
//             decl->addElement(0, offset, Ogre::VET_FLOAT3, Ogre::VES_NORMAL);
//             offset += Ogre::VertexElement::getTypeSize(Ogre::VET_FLOAT3);
//             decl->addElement(0, offset, Ogre::VET_FLOAT2,
//             Ogre::VES_TEXTURE_COORDINATES); offset +=
//             Ogre::VertexElement::getTypeSize(Ogre::VET_FLOAT2);

//             /// Allocate vertex buffer of the requested number of vertices
//             (vertexCount)
//             /// and bytes per vertex (offset)
//             Ogre::HardwareVertexBufferSharedPtr vbuf =
//                 Ogre::HardwareBufferManager::getSingleton().createVertexBuffer(
//                 offset, msh->sharedVertexData->vertexCount,
//                 Ogre::HardwareBuffer::HBU_STATIC_WRITE_ONLY);
//             /// Upload the vertex data to the card
//             vbuf->writeData(0, vbuf->getSizeInBytes(), vertices, true);

//             /// Set vertex buffer binding so buffer 0 is bound to our vertex
//             buffer Ogre::VertexBufferBinding* bind =
//             msh->sharedVertexData->vertexBufferBinding; bind->setBinding(0,
//             vbuf);

//             /// Allocate index buffer of the requested number of vertices
//             (ibufCount) Ogre::HardwareIndexBufferSharedPtr ibuf =
//             Ogre::HardwareBufferManager::getSingleton().
//                 createIndexBuffer(
//                 Ogre::HardwareIndexBuffer::IT_16BIT,
//                 indexBufferSize,
//                 Ogre::HardwareBuffer::HBU_STATIC_WRITE_ONLY);

//             /// Upload the index data to the card
//             ibuf->writeData(0, ibuf->getSizeInBytes(), faces, true);

//             /// Set parameters of the submesh
//             sub->useSharedVertices = true;
//             sub->indexData->indexBuffer = ibuf;
//             sub->indexData->indexCount = indexBufferSize;
//             sub->indexData->indexStart = 0;

//             /// Set bounding information (for culling)
//             msh->_setBounds(Ogre::AxisAlignedBox(-50,-50,-50,50,50,50));
//             msh->_setBoundingSphereRadius(50);

//             /// Notify -Mesh object that it has been loaded
//             msh->load();

//             membraneComponent->m_entity =
//             m_impl->m_sceneManager->createEntity(sceneNodeComponent->m_meshName.get(),
//             "General");

//             Ogre::MaterialPtr baseMaterial =
//             Ogre::MaterialManager::getSingleton().getByName("Membrane");
//             Ogre::MaterialPtr materialPtr =
//             baseMaterial->clone(sceneNodeComponent->m_meshName.get());
//             materialPtr->compile();
//             Ogre::TextureUnitState* ptus =
//             materialPtr->getTechnique(0)->getPass(0)->getTextureUnitState(0);
//             ptus->setColourOperationEx(Ogre::LBX_MODULATE, Ogre::LBS_MANUAL,
//             Ogre::LBS_TEXTURE, membraneComponent->colour);
//             membraneComponent->m_entity->setMaterial(materialPtr);

//             sceneNodeComponent->m_sceneNode->setOrientation(sceneNodeComponent->m_transform.orientation);
//             sceneNodeComponent->m_sceneNode->setScale(sceneNodeComponent->m_transform.scale);
//             sceneNodeComponent->m_sceneNode->setPosition(sceneNodeComponent->m_transform.position);
//             sceneNodeComponent->m_sceneNode->attachObject(membraneComponent->m_entity);
//         }

//     }
// }
