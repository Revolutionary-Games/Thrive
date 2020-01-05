#include "membrane_system.h"

#include <Engine.h>
#include <Rendering/Graphics.h>
#include <bsfCore/Mesh/BsMesh.h>
#include <bsfCore/RenderAPI/BsVertexDataDesc.h>

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
    MembraneComponent::Release(Leviathan::Scene* scene)
{
    releaseCurrentMesh();

    if(m_item) {
        m_item->DetachFromParent();
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
    // Desaturate it here so it looks nicer (could implement as method that
    // could be called i suppose)
    float saturation;
    float brightness;
    float hue;

    value.ConvertToHSB(hue, saturation, brightness);
    colour = Float4::FromHSB(hue, saturation * .75, brightness);

    // If we already have created a material we need to re-apply it
    if(coloredMaterial) {

        coloredMaterial->SetFloat4("gTint", colour);
    }
}

void
    MembraneComponent::setHealthFraction(float value)
{
    healthFraction = std::clamp(value, 0.0f, 1.0f);

    // If we already have created a material we need to re-apply it
    if(coloredMaterial) {

        coloredMaterial->SetFloat("gHealthFraction", healthFraction);
    }
}

Float4
    MembraneComponent::getColour() const
{
    return colour;
}
// ------------------------------------ //
void
    MembraneComponent::Update(Leviathan::Scene* scene,
        const Leviathan::SceneNode::pointer& parentComponentPos,
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
    // This is actually a triangle list, but the index buffer is used to build
    // the indices (to emulate a triangle fan)
    const auto bufferSize = vertices2D.size() + 2;
    const auto indexSize = vertices2D.size() * 3;

    bs::MESH_DESC meshDesc;
    meshDesc.numVertices = bufferSize;
    meshDesc.numIndices = bufferSize;

    meshDesc.indexType = bs::IT_32BIT;
    // This is static as logic for detecting just moved vertices (no new
    // created) isn't done. This is recreated every time
    meshDesc.usage = bs::MU_STATIC;
    meshDesc.subMeshes.push_back(
        bs::SubMesh(0, indexSize, bs::DOT_TRIANGLE_LIST));

    meshDesc.vertexDesc = vertexDesc;

    // TODO: 16 bit indices would save memory
    bs::SPtr<bs::MeshData> meshData =
        bs::MeshData::create(bufferSize, indexSize, vertexDesc, bs::IT_32BIT);

    // Index mapping to build all triangles
    uint32_t* indexWrite = meshData->getIndices32();

    std::remove_pointer_t<decltype(indexWrite)> currentVertexIndex = 1;

    for(size_t i = 0; i < indexSize; i += 3) {
        indexWrite[i] = 0;
        indexWrite[i + 1] = currentVertexIndex + 1;
        indexWrite[i + 2] = currentVertexIndex;

        ++currentVertexIndex;
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


    m_mesh = Leviathan::Mesh::MakeShared<Leviathan::Mesh>(
        bs::Mesh::create(meshData, meshDesc));
    // // Set the bounds to get frustum culling and LOD to work correctly.
    // // TODO: make this more accurate by calculating the actual extents
    // m_mesh->_setBounds(Ogre::Aabb(Float3::ZERO, Float3::UNIT_SCALE * 50)
    //     /*, false*/);
    // m_mesh->_setBoundingSphereRadius(50);

    // TODO: the material needs to be only recreated when the species properties
    // change, not every time an organelle is added or removed
    // Set the membrane material //
    auto baseMaterial = chooseMaterialByType();

    LEVIATHAN_ASSERT(baseMaterial, "no material for membrane");

    // The baseMaterial fetch makes a new instance so this is fine without
    // cloning
    coloredMaterial = baseMaterial;

    coloredMaterial->SetFloat4("gTint", colour);
    coloredMaterial->SetFloat("gHealthFraction", healthFraction);

    if(!m_item) {

        m_item = Leviathan::Renderable::MakeShared<Leviathan::Renderable>(
            *parentComponentPos);
    }

    m_item->SetMaterial(coloredMaterial);
    m_item->SetMesh(m_mesh);
    // m_item->setLayer(1 << scene->GetInternal());
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
    const Float2 center(0.5, 0.5);

    switch(membraneType) {
    case MEMBRANE_TYPE::MEMBRANE:
    case MEMBRANE_TYPE::DOUBLEMEMBRANE:
        meshVertices[writeIndex++] = {Float3(0, height / 2, 0), center};

        for(size_t i = 0, end = vertices2D.size(); i < end + 1; i++) {
            // Finds the UV coordinates be projecting onto a plane and
            // stretching to fit a circle.

            const double currentRadians = 2.0 * 3.1416 * i / end;

            meshVertices[writeIndex++] = {
                bs::Vector3(
                    vertices2D[i % end].X, height / 2, vertices2D[i % end].Y),
                center +
                    Float2(std::cos(currentRadians), std::sin(currentRadians)) /
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
                center + Float2(cos(currentRadians), sin(currentRadians)) / 2};
        }
        break;
    }


    return writeIndex;
}

Leviathan::Material::pointer
    MembraneComponent::chooseMaterialByType()
{
    auto shader = Leviathan::Shader::MakeShared<Leviathan::Shader>(
        Engine::Get()->GetGraphics()->LoadShaderByName("membrane.bsl"));

    auto material =
        Leviathan::Material::MakeShared<Leviathan::Material>(shader);

    // This is just a tiny bit of graphics engine specific code so this is left
    // as is
    bs::HTexture normal;
    bs::HTexture damaged;
    // When true the shader adds animation to the membrane
    bool wiggly = true;

    switch(membraneType) {
    case MEMBRANE_TYPE::MEMBRANE:
        normal = Engine::Get()->GetGraphics()->LoadTextureByName(
            "FresnelGradient.png");
        damaged = Engine::Get()->GetGraphics()->LoadTextureByName(
            "FresnelGradientDamaged.png");
        break;
    case MEMBRANE_TYPE::DOUBLEMEMBRANE:
        normal = Engine::Get()->GetGraphics()->LoadTextureByName(
            "DoubleCellMembrane.png");
        damaged = Engine::Get()->GetGraphics()->LoadTextureByName(
            "DoubleCellMembraneDamaged.png");
        break;
    case MEMBRANE_TYPE::WALL:
        normal = Engine::Get()->GetGraphics()->LoadTextureByName(
            "CellWallGradient.png");
        damaged = Engine::Get()->GetGraphics()->LoadTextureByName(
            "CellWallGradientDamaged.png");
        wiggly = false;
        break;
    case MEMBRANE_TYPE::CHITIN:
        normal = Engine::Get()->GetGraphics()->LoadTextureByName(
            "ChitinCellWallGradient.png");
        damaged = Engine::Get()->GetGraphics()->LoadTextureByName(
            "ChitinCellWallGradientDamaged.png");
        wiggly = false;
        break;
    }

    LEVIATHAN_ASSERT(
        normal && damaged && shader, "failed to load some membrane resource");

    material->SetTexture("gAlbedoTex",
        Leviathan::Texture::MakeShared<Leviathan::Texture>(normal));
    material->SetTexture("gDamagedTex",
        Leviathan::Texture::MakeShared<Leviathan::Texture>(damaged));

    material->SetVariation("WIGGLY", wiggly);

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
        Leviathan::Scene* scene,
        const Leviathan::SceneNode::pointer& parentComponentPos)
{
    component.Update(scene, parentComponentPos, m_impl->m_vertexDesc);
}
