#include "membrane_system.h"

#include <Engine.h>
#include <Rendering/Graphics.h>
#include <bsfCore/Components/BsCRenderable.h>
#include <bsfCore/Material/BsMaterial.h>
#include <bsfCore/Mesh/BsMesh.h>
#include <bsfCore/RenderAPI/BsVertexDataDesc.h>
#include <bsfCore/Scene/BsSceneObject.h>
// temporary
#include <bsfEngine/Resources/BsBuiltinResources.h>

#include <algorithm>
#include <atomic>

using namespace thrive;

////////////////////////////////////////////////////////////////////////////////
// Membrane Component
////////////////////////////////////////////////////////////////////////////////

//! This must be big enough that no organelle can be at this position
constexpr auto INVALID_FOUND_ORGANELLE = -999999.f;

MembraneComponent::MembraneComponent(MEMBRANE_TYPE type) :
    Leviathan::Component(TYPE)
{
    // membrane type
    membraneType = type;
}

MembraneComponent::~MembraneComponent()
{
    // Skip if no graphics
    if(!Engine::Get()->IsInGraphicalMode())
        return;

    LEVIATHAN_ASSERT(!m_item, "MembraneComponent not released");
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
    MembraneComponent::Release(bs::Scene* scene)
{
    releaseCurrentMesh();

    if(m_item && !m_item.isDestroyed()) {
        m_item->destroy();
        m_item = nullptr;
    }
}
// ------------------------------------ //
Float2
    MembraneComponent::FindClosestOrganelles(const Float2& target)
{
    // The distance we want the membrane to be from the organelles squared.
    double closestSoFar = 4;
    int closestIndex = -1;

    for(size_t i = 0, end = organellePositions.size(); i < end; i++) {
        double lenToObject = (target - organellePositions[i]).LengthSquared();

        if(lenToObject < 4 && lenToObject < closestSoFar) {
            closestSoFar = lenToObject;

            closestIndex = i;
        }
    }

    if(closestIndex != -1)
        return organellePositions[closestIndex];
    else
        return {INVALID_FOUND_ORGANELLE, INVALID_FOUND_ORGANELLE};
}

Float2
    MembraneComponent::GetMovement(const Float2& target,
        const Float2& closestOrganelle)
{
    double power = pow(2.7, (-(target - closestOrganelle).Length()) / 10) / 50;

    return (closestOrganelle - target) * power;
}

Float3
    MembraneComponent::GetExternalOrganelle(double x, double y)
{
    // This gets called by the flagella every frame as on the first call this
    // object is not initialized yet. TODO: do something about that

    // This was causing little regular-interval lag bursts
    /*if(vertices2D.empty())
        LOG_WARNING("MembraneComponent: GetExternalOrganelle: called before "
                    "membrane is initialized. Returning 0, 0");
    */



    float organelleAngle = std::atan2(y, x);

    Float3 closestSoFar(0, 0, 0);
    float angleToClosest = Leviathan::PI * 2;

    for(const auto& vertex : vertices2D) {
        if(std::abs(std::atan2(vertex.Y, vertex.X) - organelleAngle) <
            angleToClosest) {
            closestSoFar = Float3(vertex.X, vertex.Y, 0);
            angleToClosest =
                std::abs(std::atan2(vertex.Y, vertex.X) - organelleAngle);
        }
    }

    // Swap to world coordinates from internal membrane coordinates
    return Float3(closestSoFar.X, 0, closestSoFar.Y);
}

bool
    MembraneComponent::contains(float x, float y)
{
    bool crosses = false;

    int n = vertices2D.size();
    for(int i = 0; i < n - 1; i++) {
        if((vertices2D[i].Y <= y && y < vertices2D[i + 1].Y) ||
            (vertices2D[i + 1].Y <= y && y < vertices2D[i].Y)) {
            if(x < (vertices2D[i + 1].X - vertices2D[i].X) *
                           (y - vertices2D[i].Y) /
                           (vertices2D[i + 1].Y - vertices2D[i].Y) +
                       vertices2D[i].X) {
                crosses = !crosses;
            }
        }
    }

    return crosses;
}

float
    MembraneComponent::calculateEncompassingCircleRadius() const
{
    if(m_isEncompassingCircleCalculated)
        return m_encompassingCircleRadius;

    float distanceSquared = 0;

    for(const auto& vertex : vertices2D) {

        const auto currentDistance = vertex.LengthSquared();
        if(currentDistance >= distanceSquared)
            distanceSquared = currentDistance;
    }

    m_isEncompassingCircleCalculated = true;
    m_encompassingCircleRadius = std::sqrt(distanceSquared);

    return m_encompassingCircleRadius;
}

// ------------------------------------ //
//! Should set the colour of the membrane once working
void
    MembraneComponent::setColour(const Float4& value)
{
    colour = value;
    LOG_WRITE("TODO: MembraneComponent::setColour");
    // DEBUG_BREAK;

    // // Desaturate it here so it looks nicer (could implement as method
    // thatcould
    // // be called i suppose)
    // Ogre::Real saturation;
    // Ogre::Real brightness;
    // Ogre::Real hue;
    // colour.getHSB(&hue, &saturation, &brightness);
    // colour.setHSB(hue, saturation * .75, brightness);

    // // If we already have created a material we need to re-apply it
    // if(coloredMaterial) {
    //     coloredMaterial->getTechnique(0)
    //         ->getPass(0)
    //         ->getFragmentProgramParameters()
    //         ->setNamedConstant("membraneColour", colour);
    //     coloredMaterial->compile();
    // }
}

void
    MembraneComponent::setHealthFraction(float value)
{
    healthFraction = std::clamp(value, 0.0f, 1.0f);

    // If we already have created a material we need to re-apply it
    if(coloredMaterial) {
        LOG_WRITE("TODO: setHealthFraction");

        // coloredMaterial->getTechnique(0)
        //     ->getPass(0)
        //     ->getFragmentProgramParameters()
        //     ->setNamedConstant("healthPercentage", healthFraction);
        // coloredMaterial->compile();
    }
}

Float4
    MembraneComponent::getColour() const
{
    return colour;
}
// ------------------------------------ //
void
    MembraneComponent::Update(bs::Scene* scene,
        const bs::HSceneObject& parentComponentPos,
        const bs::SPtr<bs::VertexDataDesc>& vertexDesc)
{
    if(clearNeeded) {

        releaseCurrentMesh();
        clearNeeded = false;
    }

    // Skip if the mesh is already created //
    if(isInitialized)
        return;

    if(!isInitialized)
        Initialize();

    // Skip if no graphics
    if(!Engine::Get()->IsInGraphicalMode())
        return;

    // This is a triangle fan so we only need 2 + n vertices
    const auto bufferSize = vertices2D.size() + 2;

    LOG_WRITE("TODO: MembraneComponent::Update");

    bs::MESH_DESC meshDesc;
    meshDesc.numVertices = bufferSize;
    meshDesc.numIndices = bufferSize;

    meshDesc.indexType = bs::IT_32BIT;
    // This is static as logic for detecting just moved vertices (no new
    // created) isn't done. This is recreated every time
    meshDesc.usage = bs::MU_STATIC;
    meshDesc.subMeshes.push_back(
        bs::SubMesh(0, bufferSize, bs::DOT_TRIANGLE_FAN));

    meshDesc.vertexDesc = vertexDesc;

    // TODO: 16 bit indices would save memory
    bs::SPtr<bs::MeshData> meshData =
        bs::MeshData::create(bufferSize, bufferSize, vertexDesc, bs::IT_32BIT);

    // 1 to 1 index buffer mapping
    uint32_t* indexWrite = meshData->getIndices32();

    for(size_t i = 0; i < bufferSize; ++i) {
        indexWrite[i] = i;
    }

    // Write mesh data //
    size_t writeIndex = 0;
    MembraneVertex* meshVertices =
        reinterpret_cast<MembraneVertex*>(meshData->getStreamData(0));

    writeIndex = InitializeCorrectMembrane(writeIndex, meshVertices);

    // This can be commented out when this works correctly, or maybe a
    // different macro for debug builds to include this check could
    // work, but it has to also work on linux
    LEVIATHAN_ASSERT(writeIndex == bufferSize, "Invalid array element math in "
                                               "fill vertex buffer");


    m_mesh = bs::Mesh::create(meshData, meshDesc);

    // // Set the bounds to get frustum culling and LOD to work correctly.
    // // TODO: make this more accurate by calculating the actual extents
    // m_mesh->_setBounds(Ogre::Aabb(Float3::ZERO, Float3::UNIT_SCALE * 50)
    //     /*, false*/);
    // m_mesh->_setBoundingSphereRadius(50);


    // Set the membrane material //
    // species (allowing the same species to share)
    if(!coloredMaterial) {
        auto baseMaterial = chooseMaterialByType();

        LEVIATHAN_ASSERT(
            baseMaterial, "Failed to find base material for membrane");

        // The baseMaterial fetch makes a new instance so this is fine
        coloredMaterial = baseMaterial;
        //     coloredMaterial = baseMaterial->clone(
        //         "Membrane_instance_" + std::to_string(++membraneNumber));

        //     coloredMaterial->getTechnique(0)
        //         ->getPass(0)
        //         ->getFragmentProgramParameters()
        //         ->setNamedConstant("membraneColour", colour);

        //     coloredMaterial->getTechnique(0)
        //         ->getPass(0)
        //         ->getFragmentProgramParameters()
        //         ->setNamedConstant("healthPercentage", healthFraction);
        //     coloredMaterial->compile();

        //     coloredMaterial->getTechnique(0)
        //         ->getPass(0)
        //         ->getTextureUnitState(0)
        //         ->setHardwareGammaEnabled(true);

        //     coloredMaterial->compile();
    }

    if(!m_item)
        m_item = parentComponentPos->addComponent<bs::CRenderable>();

    m_item->setMaterial(coloredMaterial);
    m_item->setMesh(m_mesh);
}

void
    MembraneComponent::DrawCorrectMembrane()
{
    switch(membraneType) {
    case MEMBRANE_TYPE::MEMBRANE: DrawMembrane(); break;
    case MEMBRANE_TYPE::DOUBLEMEMBRANE: DrawMembrane(); break;
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
    const bs::Vector2 center(0.5, 0.5);

    switch(membraneType) {
    case MEMBRANE_TYPE::MEMBRANE:
    case MEMBRANE_TYPE::DOUBLEMEMBRANE:
        meshVertices[writeIndex++] = {bs::Vector3(0, height / 2, 0), center};

        for(size_t i = 0, end = vertices2D.size(); i < end + 1; i++) {
            // Finds the UV coordinates be projecting onto a plane and
            // stretching to fit a circle.

            const double currentRadians = 2.0 * 3.1416 * i / end;

            meshVertices[writeIndex++] = {
                bs::Vector3(
                    vertices2D[i % end].X, height / 2, vertices2D[i % end].Y),
                center + bs::Vector2(std::cos(currentRadians),
                             std::sin(currentRadians)) /
                             2};
        }
        break;
    case MEMBRANE_TYPE::WALL:
    case MEMBRANE_TYPE::CHITIN:
        // cell walls need obvious inner/outer memrbranes (we can worry
        // about chitin later)
        height = .05;
        meshVertices[writeIndex++] = {Float3(0, height / 2, 0), center};

        for(size_t i = 0, end = vertices2D.size(); i < end + 1; i++) {
            // Finds the UV coordinates be projecting onto a plane and
            // stretching to fit a circle.
            const double currentRadians = 3.1416 * i / end;
            meshVertices[writeIndex++] = {
                Float3(
                    vertices2D[i % end].X, height / 2, vertices2D[i % end].Y),
                center +
                    bs::Vector2(cos(currentRadians), sin(currentRadians)) / 2};
        }
        break;
    }


    return writeIndex;
}

bs::HMaterial
    MembraneComponent::chooseMaterialByType()
{
    LOG_WRITE("TODO: chooseMaterialByType");
    // DEBUG_BREAK;

    // switch(membraneType) {
    // case MEMBRANE_TYPE::MEMBRANE:
    //     return
    //     Ogre::MaterialManager::getSingleton().getByName("Membrane");
    //     break;
    // case MEMBRANE_TYPE::DOUBLEMEMBRANE:
    //     return Ogre::MaterialManager::getSingleton().getByName(
    //         "MembraneDouble");
    //     break;
    // case MEMBRANE_TYPE::WALL:
    //     return
    //     Ogre::MaterialManager::getSingleton().getByName("cellwall");
    //     break;
    // case MEMBRANE_TYPE::CHITIN:
    //     return Ogre::MaterialManager::getSingleton().getByName(
    //         "cellwallchitin");
    //     break;
    // }

    // auto texture =
    //     Engine::Get()->GetGraphics()->LoadTextureByName("CellWallGradient.png");
    auto texture =
        Engine::Get()->GetGraphics()->LoadTextureByName("flagella_texture.png");

    // bs::HShader shader = bs::gBuiltinResources().getBuiltinShader(
    //     bs::BuiltinShader::Transparent);
    bs::HShader shader =
        bs::gBuiltinResources().getBuiltinShader(bs::BuiltinShader::Standard);
    bs::HMaterial material = bs::Material::create(shader);
    material->setTexture("gAlbedoTex", texture);

    return material;
}

void
    MembraneComponent::Initialize()
{
    for(const auto& pos : organellePositions) {
        if(std::abs(pos.X) + 1 > cellDimensions) {
            cellDimensions = std::abs(pos.X) + 1;
        }
        if(std::abs(pos.Y) + 1 > cellDimensions) {
            cellDimensions = std::abs(pos.Y) + 1;
        }
    }

    for(int i = membraneResolution; i > 0; i--) {
        vertices2D.emplace_back(-cellDimensions,
            cellDimensions - 2 * cellDimensions / membraneResolution * i);
    }
    for(int i = membraneResolution; i > 0; i--) {
        vertices2D.emplace_back(
            cellDimensions - 2 * cellDimensions / membraneResolution * i,
            cellDimensions);
    }
    for(int i = membraneResolution; i > 0; i--) {
        vertices2D.emplace_back(cellDimensions,
            -cellDimensions + 2 * cellDimensions / membraneResolution * i);
    }
    for(int i = membraneResolution; i > 0; i--) {
        vertices2D.emplace_back(
            -cellDimensions + 2 * cellDimensions / membraneResolution * i,
            -cellDimensions);
    }

    // Does this need to run 40*cellDimensions times. That seems to be
    // (reduced from 50 to 40 times, can probabbly be reduced more)
    for(int i = 0; i < 40 * cellDimensions; i++) {
        DrawCorrectMembrane();
    }

    // Subdivide();

    // Reset this cached status as new points have just been generated
    m_isEncompassingCircleCalculated = false;

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
        const auto closestOrganelle = FindClosestOrganelles(vertices2D[i]);
        if(closestOrganelle ==
            Float2(INVALID_FOUND_ORGANELLE, INVALID_FOUND_ORGANELLE)) {
            newPositions[i] =
                (vertices2D[(end + i - 1) % end] + vertices2D[(i + 1) % end]) /
                2;
        } else {
            const auto movementDirection =
                GetMovement(vertices2D[i], closestOrganelle);
            newPositions[i].X -= movementDirection.X;
            newPositions[i].Y -= movementDirection.Y;
        }
    }

    // Allows for the addition and deletion of points in the membrane.
    for(size_t i = 0; i < newPositions.size() - 1; i++) {
        // Check to see if the gap between two points in the membrane is too
        // big.
        if((newPositions[i] - newPositions[(i + 1) % newPositions.size()])
                .Length() > cellDimensions / membraneResolution) {
            // Add an element after the ith term that is the average of the
            // i and i+1 term.
            auto it = newPositions.begin();
            const auto tempPoint =
                (newPositions[(i + 1) % newPositions.size()] +
                    newPositions[i]) /
                2;
            newPositions.insert(it + i + 1, tempPoint);

            i++;
        }

        // Check to see if the gap between two points in the membrane is too
        // small.
        if((newPositions[(i + 1) % newPositions.size()] -
               newPositions[(i - 1) % newPositions.size()])
                .Length() < cellDimensions / membraneResolution) {
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
    organellePositions.emplace_back(x, y);
}

bool
    MembraneComponent::removeSentOrganelle(double x, double y)
{
    for(auto iter = organellePositions.begin();
        iter != organellePositions.end(); ++iter) {

        if(iter->X == x && iter->Y == y) {
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
    MembraneComponent::releaseCurrentMesh()
{
    isInitialized = false;
    vertices2D.clear();
    m_mesh = nullptr;
}

/*
Cell Wall Code Here
*/

// this is where the magic happens i think
Float2
    MembraneComponent::GetMovementForCellWall(const Float2& target,
        const Float2& closestOrganelle)
{
    double power = pow(10.0f, (-(target - closestOrganelle).Length())) / 50;

    return (closestOrganelle - target) * power;
}

void
    MembraneComponent::DrawCellWall()
{
    // Stores the temporary positions of the membrane.
    auto newPositions = vertices2D;

    // Loops through all the points in the membrane and relocates them as
    // necessary.
    for(size_t i = 0, end = newPositions.size(); i < end; i++) {
        const auto closestOrganelle = FindClosestOrganelles(vertices2D[i]);
        if(closestOrganelle ==
            Float2(INVALID_FOUND_ORGANELLE, INVALID_FOUND_ORGANELLE)) {
            newPositions[i] =
                (vertices2D[(end + i - 1) % end] + vertices2D[(i + 1) % end]) /
                2;
        } else {
            const auto movementDirection =
                GetMovementForCellWall(vertices2D[i], closestOrganelle);
            newPositions[i].X -= movementDirection.X;
            newPositions[i].Y -= movementDirection.Y;
        }
    }

    // Allows for the addition and deletion of points in the membrane.
    for(size_t i = 0; i < newPositions.size() - 1; i++) {
        // Check to see if the gap between two points in the membrane is too
        // big.
        if((newPositions[i] - newPositions[(i + 1) % newPositions.size()])
                .Length() > cellDimensions / membraneResolution) {
            // Add an element after the ith term that is the average of the
            // i and i+1 term.
            auto it = newPositions.begin();
            const auto tempPoint =
                (newPositions[(i + 1) % newPositions.size()] +
                    newPositions[i]) /
                2;
            newPositions.insert(it + i + 1, tempPoint);

            // Check to see if the gap between two points in the wall is too
            // small.
            if((newPositions[(i + 1) % newPositions.size()] -
                   newPositions[(i - 1) % newPositions.size()])
                    .Length() < cellDimensions / membraneResolution) {
                // Delete the ith term.
                auto it = newPositions.begin();
                newPositions.erase(it + i);
            }
            i++;
        }

        // Check to see if the gap between two points in the membrane is too
        // small.
        if((newPositions[(i + 1) % newPositions.size()] -
               newPositions[(i - 1) % newPositions.size()])
                .Length() < cellDimensions / membraneResolution) {
            // Delete the ith term.
            auto it = newPositions.begin();
            newPositions.erase(it + i);
        }
    }

    vertices2D = newPositions;
}
// ------------------------------------ //
// MembraneSystem
struct MembraneSystem::Implementation {

    Implementation()
    {
        m_vertexDesc = bs::VertexDataDesc::create();
        m_vertexDesc->addVertElem(bs::VET_FLOAT3, bs::VES_POSITION);
        m_vertexDesc->addVertElem(bs::VET_FLOAT2, bs::VES_TEXCOORD);
    }

    bs::SPtr<bs::VertexDataDesc> m_vertexDesc;
};

MembraneSystem::MembraneSystem() : m_impl(std::make_unique<Implementation>()) {}
MembraneSystem::~MembraneSystem() {}

void
    MembraneSystem::UpdateComponent(MembraneComponent& component,
        bs::Scene* scene,
        const bs::HSceneObject& parentComponentPos)
{
    component.Update(scene, parentComponentPos, m_impl->m_vertexDesc);
}
