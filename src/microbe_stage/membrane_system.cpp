#include "membrane_system.h"

#include "bullet/collision_system.h"
#include "bullet/rigid_body_system.h"
#include "engine/component_factory.h"
#include "engine/engine.h"
#include "engine/entity_filter.h"
#include "engine/game_state.h"
#include "engine/serialization.h"
#include "game.h"
#include "ogre/scene_node_system.h"
#include "scripting/luabind.h"
#include "util/make_unique.h"

#include "microbe_stage/membrane.h"

#include <OgreMeshManager.h>
#include <OgreMaterialManager.h>
#include <OgreMaterial.h>
#include <OgreTechnique.h>
#include <OgreEntity.h>
#include <OgreSceneManager.h>
#include <OgreRoot.h>
#include <OgreSubMesh.h>
#include <stdexcept>

using namespace thrive;

////////////////////////////////////////////////////////////////////////////////
// CompoundEmitterComponent
////////////////////////////////////////////////////////////////////////////////

luabind::scope
MembraneComponent::luaBindings() {
    using namespace luabind;
    return class_<MembraneComponent, Component>("MembraneComponent")
        .enum_("ID") [
            value("TYPE_ID", MembraneComponent::TYPE_ID)
        ]
        .scope [
            def("TYPE_NAME", &MembraneComponent::TYPE_NAME)
        ]
        .def(constructor<>())
        .def("sendOrganelles", &MembraneComponent::sendOrganelles)
        .def("getExternOrganellePos", &MembraneComponent::getExternOrganellePos)
        .def("setColour", &MembraneComponent::setColour)
        .def("getAbsorbedCompounds", &MembraneComponent::getAbsorbedCompounds)
    ;
}

void
MembraneComponent::setColour(float red = 1.0f, float green = 1.0f, float blue = 1.0f, float alpha = 1.0f)
{
    colour = Ogre::ColourValue(red, green, blue, alpha);
}

void
MembraneComponent::load(
    const StorageContainer& storage
) {
    Component::load(storage);
    //m_emissionRadius = storage.get<Ogre::Real>("emissionRadius", 0.0);
}

int
MembraneComponent::getAbsorbedCompounds() {
    int amount = m_membrane.compoundAmount;
    m_membrane.compoundAmount = 0;
    return amount;
}

StorageContainer
MembraneComponent::storage() const {
    StorageContainer storage = Component::storage();
   // storage.set<Ogre::Real>("emissionRadius", m_emissionRadius);
    return storage;
}

REGISTER_COMPONENT(MembraneComponent)


////////////////////////////////////////////////////////////////////////////////
// MembraneSystem
////////////////////////////////////////////////////////////////////////////////

luabind::scope
MembraneSystem::luaBindings() {
    using namespace luabind;
    return class_<MembraneSystem, System>("MembraneSystem")
        .def(constructor<>())
    ;
}


struct MembraneSystem::Implementation {

    EntityFilter<
        MembraneComponent,
        OgreSceneNodeComponent
    > m_entities;

    Ogre::SceneManager* m_sceneManager = nullptr;


};


MembraneSystem::MembraneSystem()
  : m_impl(new Implementation())
{
}


MembraneSystem::~MembraneSystem() {}


void
MembraneSystem::init(
    GameState* gameState
) {
    System::init(gameState);
    m_impl->m_entities.setEntityManager(&gameState->entityManager());
    m_impl->m_sceneManager = gameState->sceneManager();
}


void
MembraneSystem::shutdown() {
    m_impl->m_entities.setEntityManager(nullptr);
    m_impl->m_sceneManager = nullptr;
    System::shutdown();
}


void
MembraneSystem::update(int, int) {
    m_impl->m_entities.clearChanges();
    for (auto& value : m_impl->m_entities) {
        MembraneComponent* membraneComponent = std::get<0>(value.second);
        OgreSceneNodeComponent* sceneNodeComponent = std::get<1>(value.second);

        if (membraneComponent->wantsMembrane && sceneNodeComponent->m_meshName.get().find("membrane") != std::string::npos)
        {
            membraneComponent->wantsMembrane = false;
            // Get the vertex positions of the membrane.
            if(!membraneComponent->m_membrane.isInitialized)
            {
                membraneComponent->m_membrane.Initialize(membraneComponent->organellePositions);
            }
            membraneComponent->m_membrane.Update(membraneComponent->organellePositions);

            //If the mesh already exists, destroy the old one
            Ogre::MeshManager::getSingleton().remove(sceneNodeComponent->m_meshName.get());
            // Create a mesh and a submesh.
            Ogre::MeshPtr msh = Ogre::MeshManager::getSingleton().createManual(sceneNodeComponent->m_meshName.get(), "General");
            Ogre::SubMesh* sub = msh->createSubMesh();

            // Define the vertices.
            std::vector<double> vertexData;
            for(size_t i=0, end=membraneComponent->m_membrane.MeshPoints.size(); i<end; i++)
            {
                // Vertex.
                vertexData.push_back(membraneComponent->m_membrane.MeshPoints[i].x);
                vertexData.push_back(membraneComponent->m_membrane.MeshPoints[i].y);
                vertexData.push_back(membraneComponent->m_membrane.MeshPoints[i].z);

                // Normal.
                //vertexData.push_back(component->MyMembrane.Normals[i].x);
                //vertexData.push_back(component->MyMembrane.Normals[i].y);
                //vertexData.push_back(component->MyMembrane.Normals[i].z);
                vertexData.push_back(0.0);
                vertexData.push_back(0.0);
                vertexData.push_back(1.0);

                // UV coordinates.
                vertexData.push_back(membraneComponent->m_membrane.UVs[i].x);
                vertexData.push_back(membraneComponent->m_membrane.UVs[i].y);
            }

            // Populate the vertex buffer.
            const size_t vertexBufferSize = vertexData.size();
            float vertices[vertexBufferSize];
            for(size_t i=0; i<vertexBufferSize; i++)
            {
                vertices[i] = vertexData[i];
            }

            // Populate the index buffer.
            const size_t indexBufferSize = vertexData.size()/8;
            unsigned short faces[indexBufferSize];
            for(size_t i=0, end=indexBufferSize; i<end; i++)
            {
                faces[i]=i;
            }

            // Create vertex data structure for 8 vertices shared between submeshes.
            msh->sharedVertexData = new Ogre::VertexData();
            msh->sharedVertexData->vertexCount = vertexData.size()/8;

            /// Create declaration (memory format) of vertex data
            Ogre::VertexDeclaration* decl = msh->sharedVertexData->vertexDeclaration;
            size_t offset = 0;
            // 1st buffer
            decl->addElement(0, offset, Ogre::VET_FLOAT3, Ogre::VES_POSITION);
            offset += Ogre::VertexElement::getTypeSize(Ogre::VET_FLOAT3);
            decl->addElement(0, offset, Ogre::VET_FLOAT3, Ogre::VES_NORMAL);
            offset += Ogre::VertexElement::getTypeSize(Ogre::VET_FLOAT3);
            decl->addElement(0, offset, Ogre::VET_FLOAT2, Ogre::VES_TEXTURE_COORDINATES);
            offset += Ogre::VertexElement::getTypeSize(Ogre::VET_FLOAT2);

            /// Allocate vertex buffer of the requested number of vertices (vertexCount)
            /// and bytes per vertex (offset)
            Ogre::HardwareVertexBufferSharedPtr vbuf =
                Ogre::HardwareBufferManager::getSingleton().createVertexBuffer(
                offset, msh->sharedVertexData->vertexCount, Ogre::HardwareBuffer::HBU_STATIC_WRITE_ONLY);
            /// Upload the vertex data to the card
            vbuf->writeData(0, vbuf->getSizeInBytes(), vertices, true);

            /// Set vertex buffer binding so buffer 0 is bound to our vertex buffer
            Ogre::VertexBufferBinding* bind = msh->sharedVertexData->vertexBufferBinding;
            bind->setBinding(0, vbuf);

            /// Allocate index buffer of the requested number of vertices (ibufCount)
            Ogre::HardwareIndexBufferSharedPtr ibuf = Ogre::HardwareBufferManager::getSingleton().
                createIndexBuffer(
                Ogre::HardwareIndexBuffer::IT_16BIT,
                indexBufferSize,
                Ogre::HardwareBuffer::HBU_STATIC_WRITE_ONLY);

            /// Upload the index data to the card
            ibuf->writeData(0, ibuf->getSizeInBytes(), faces, true);

            /// Set parameters of the submesh
            sub->useSharedVertices = true;
            sub->indexData->indexBuffer = ibuf;
            sub->indexData->indexCount = indexBufferSize;
            sub->indexData->indexStart = 0;

            /// Set bounding information (for culling)
            msh->_setBounds(Ogre::AxisAlignedBox(-50,-50,-50,50,50,50));
            msh->_setBoundingSphereRadius(50);

            /// Notify -Mesh object that it has been loaded
            msh->load();

            Ogre::Entity* thisEntity = m_impl->m_sceneManager->createEntity(sceneNodeComponent->m_meshName.get(),  "General");

            Ogre::MaterialPtr baseMaterial = Ogre::MaterialManager::getSingleton().getByName("Membrane");
            Ogre::MaterialPtr materialPtr = baseMaterial->clone(sceneNodeComponent->m_meshName.get());
            materialPtr->compile();
            Ogre::TextureUnitState* ptus = materialPtr->getTechnique(0)->getPass(0)->getTextureUnitState(0);
            ptus->setColourOperationEx(Ogre::LBX_MODULATE, Ogre::LBS_MANUAL, Ogre::LBS_TEXTURE, membraneComponent->colour);
            thisEntity->setMaterial(materialPtr);
            //thisEntity->setMaterialName("Membrane");

            sceneNodeComponent->m_sceneNode->setOrientation(sceneNodeComponent->m_transform.orientation);
            sceneNodeComponent->m_sceneNode->setScale(sceneNodeComponent->m_transform.scale);
            sceneNodeComponent->m_sceneNode->setPosition(sceneNodeComponent->m_transform.position);
            sceneNodeComponent->m_sceneNode->attachObject(thisEntity);
        }

    }
}

void MembraneComponent::sendOrganelles(double x, double y)
{
    organellePositions.emplace_back(x,y,0);
}

luabind::object MembraneComponent::getExternOrganellePos(double x, double y)
{
    luabind::object externalOrganellePosition = luabind::newtable(Game::instance().engine().luaState());

    Ogre::Vector3 organelleCoords = m_membrane.GetExternalOrganelle(x, y);
    externalOrganellePosition[1] = organelleCoords.x;
    externalOrganellePosition[2] = organelleCoords.y;


    return externalOrganellePosition;
}
