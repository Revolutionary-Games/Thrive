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
// Membrane Component
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
        .def("getColour", &MembraneComponent::getColour)
    ;
}

MembraneComponent::MembraneComponent()
{
    // Half the side length of the original square that is compressed to make the membrane.
    cellDimensions = 10;

    // Amount of segments on one side of the above described square.
	membraneResolution = 10;

	// The total amount of compounds.
	compoundAmount = 0;

	isInitialized = false;
	wantsMembrane = true;
}

void
MembraneComponent::setColour(float red = 1.0f, float green = 1.0f, float blue = 1.0f, float alpha = 1.0f)
{
    colour = Ogre::ColourValue(red, green, blue, alpha);
}

Ogre::Vector3
MembraneComponent::getColour()
{
    return Ogre::Vector3(colour.r, colour.g, colour.b);
}

void
MembraneComponent::load(
    const StorageContainer& storage
) {
    Component::load(storage);
    //m_emissionRadius = storage.get<Ogre::Real>("emissionRadius", 0.0);
}

StorageContainer
MembraneComponent::storage() const {
    StorageContainer storage = Component::storage();
   // storage.set<Ogre::Real>("emissionRadius", m_emissionRadius);
    return storage;
}

Ogre::Vector3 MembraneComponent::FindClosestOrganelles(Ogre::Vector3 target)
{
	double closestSoFar = 9;
	int closestIndex = -1;

	for (size_t i=0, end=organellePositions.size(); i<end; i++)
	{
		double lenToObject =  target.squaredDistance(organellePositions[i]);

		if(lenToObject < 9 && lenToObject < closestSoFar)
		{
			closestSoFar = lenToObject;

			closestIndex = i;
		}
	}

	if(closestIndex != -1)
		return (organellePositions[closestIndex]);
	else
		return Ogre::Vector3(0,0,-1);
}

Ogre::Vector3 MembraneComponent::GetMovement(Ogre::Vector3 target, Ogre::Vector3 closestOrganelle)
{
	double power = pow(2.7, (-target.distance(closestOrganelle))/10)/50;

	return (Ogre::Vector3(closestOrganelle)-Ogre::Vector3(target))*power;
}


void MembraneComponent::MakePrism()
{
	double height = .1;

	for(size_t i=0, end=vertices2D.size(); i<end; i++)
	{
		MeshPoints.emplace_back(vertices2D[i%end].x, vertices2D[i%end].y, vertices2D[i%end].z+height/2);
		MeshPoints.emplace_back(vertices2D[(i+1)%end].x, vertices2D[(i+1)%end].y, vertices2D[(i+1)%end].z-height/2);
		MeshPoints.emplace_back(vertices2D[i%end].x, vertices2D[i%end].y, vertices2D[i%end].z-height/2);
		MeshPoints.emplace_back(vertices2D[i%end].x, vertices2D[i%end].y, vertices2D[i%end].z+height/2);
		MeshPoints.emplace_back(vertices2D[(i+1)%end].x, vertices2D[(i+1)%end].y, vertices2D[(i+1)%end].z+height/2);
		MeshPoints.emplace_back(vertices2D[(i+1)%end].x, vertices2D[(i+1)%end].y, vertices2D[(i+1)%end].z-height/2);
	}

	for(size_t i=0, end=vertices2D.size(); i<end; i++)
	{
		MeshPoints.emplace_back(vertices2D[i%end].x, vertices2D[i%end].y, vertices2D[i%end].z+height/2);
		MeshPoints.emplace_back(0,0,height/2);
		MeshPoints.emplace_back(vertices2D[(i+1)%end].x, vertices2D[(i+1)%end].y, vertices2D[(i+1)%end].z+height/2);

		MeshPoints.emplace_back(vertices2D[i%end].x, vertices2D[i%end].y, vertices2D[i%end].z-height/2);
		MeshPoints.emplace_back(vertices2D[(i+1)%end].x, vertices2D[(i+1)%end].y, vertices2D[(i+1)%end].z-height/2);
		MeshPoints.emplace_back(0,0,-height/2);
	}
}

Ogre::Vector3 MembraneComponent::GetExternalOrganelle(double x, double y)
{

    float organelleAngle = Ogre::Math::ATan2(y,x).valueRadians();

    Ogre::Vector3 closestSoFar(0, 0, 0);
    float angleToClosest = Ogre::Math::TWO_PI;

    for(size_t i=0, end=vertices2D.size(); i<end; i++) {
        if(Ogre::Math::Abs(Ogre::Math::ATan2(vertices2D[i].y, vertices2D[i].x).valueRadians() - organelleAngle) < angleToClosest) {
            closestSoFar = Ogre::Vector3(vertices2D[i].x, vertices2D[i].y, 0);
            angleToClosest = Ogre::Math::Abs(Ogre::Math::ATan2(vertices2D[i].y, vertices2D[i].x).valueRadians() - organelleAngle);
        }
    }

    return closestSoFar;
}

bool MembraneComponent::contains(float x, float y)
{
    //if (x < -cellDimensions/2 || x > cellDimensions/2 || y < -cellDimensions/2 || y > cellDimensions/2) return false;

    bool crosses = false;

    int n = vertices2D.size();
    for (int i = 0; i < n-1; i++)
    {
        if ((vertices2D[i].y <= y && y < vertices2D[i+1].y) || (vertices2D[i+1].y <= y && y < vertices2D[i].y))
        {
            if (x < (vertices2D[i+1].x - vertices2D[i].x) * (y - vertices2D[i].y) / (vertices2D[i+1].y - vertices2D[i].y) + vertices2D[i].x)
            {
                crosses = !crosses;
            }
        }
    }

    return crosses;
}

void MembraneComponent::CalcUVCircle()
{
    UVs.clear();

    for(size_t i=0, end=MeshPoints.size(); i<end; i++)
    {
        double x, y, z, a, b, c;
        x = MeshPoints[i].x;
        y = MeshPoints[i].y;
        z = MeshPoints[i].z;

        double ray = x*x + y*y + z*z;

        double t = Ogre::Math::Sqrt(ray)/(2.0*ray);
        a = t*x;
        b = t*y;
        c = t*z;

        UVs.emplace_back(a+0.5,b+0.5,c+0.5);
    }
}

void MembraneComponent::Update()
{
    MeshPoints.clear();

    DrawMembrane();
	MakePrism();
    CalcUVCircle();
}


void MembraneComponent::Initialize()
{

    for (Ogre::Vector3 pos : organellePositions) {
        if (abs(pos.x) + 1 > cellDimensions) {
            cellDimensions = abs(pos.x) + 1;
        }
        if (abs(pos.y) + 1 > cellDimensions) {
            cellDimensions = abs(pos.y) + 1;
        }
    }

	for(int i=0; i<membraneResolution; i++)
	{
		vertices2D.emplace_back(-cellDimensions + 2*cellDimensions/membraneResolution*i, -cellDimensions, 0);
	}
	for(int i=0; i<membraneResolution; i++)
	{
		vertices2D.emplace_back(cellDimensions, -cellDimensions + 2*cellDimensions/membraneResolution*i, 0);
	}
	for(int i=0; i<membraneResolution; i++)
	{
		vertices2D.emplace_back(cellDimensions - 2*cellDimensions/membraneResolution*i, cellDimensions, 0);
	}
	for(int i=0; i<membraneResolution; i++)
	{
		vertices2D.emplace_back(-cellDimensions, cellDimensions - 2*cellDimensions/membraneResolution*i, 0);
	}

	for(int i=0; i<50*cellDimensions; i++)
    {
        DrawMembrane();
    }
	MakePrism();
	//Subdivide();
	CalcUVCircle();

	isInitialized = true;
}

void MembraneComponent::DrawMembrane()
{
    // Stores the temporary positions of the membrane.
	std::vector<Ogre::Vector3> newPositions = vertices2D;

    // Loops through all the points in the membrane and relocates them as necessary.
	for(size_t i=0, end=newPositions.size(); i<end; i++)
	{
		Ogre::Vector3 closestOrganelle = FindClosestOrganelles(vertices2D[i]);
		if(closestOrganelle == Ogre::Vector3(0,0,-1))
		{
			newPositions[i] = (vertices2D[(end+i-1)%end] + vertices2D[(i+1)%end])/2;
		}
		else
		{
			Ogre::Vector3 movementDirection = GetMovement(vertices2D[i], closestOrganelle);
			newPositions[i].x -= movementDirection.x;
			newPositions[i].y -= movementDirection.y;
		}
	}

	// Allows for the addition and deletion of points in the membrane.
	for(size_t i=0; i<newPositions.size()-1; i++)
	{
		// Check to see if the gap between two points in the membrane is too big.
		if(newPositions[i].distance(newPositions[(i+1)%newPositions.size()]) > cellDimensions/membraneResolution)
		{
			// Add an element after the ith term that is the average of the i and i+1 term.
			auto it = newPositions.begin();
			Ogre::Vector3 tempPoint = (newPositions[(i+1)%newPositions.size()] + newPositions[i])/2;
			newPositions.insert(it+i+1, tempPoint);

			i++;
		}

		// Check to see if the gap between two points in the membrane is too small.
		if(newPositions[(i+1)%newPositions.size()].distance(newPositions[(i-1)%newPositions.size()]) < cellDimensions/membraneResolution)
		{
			// Delete the ith term.
			auto it = newPositions.begin();
			newPositions.erase(it+i);
		}
	}

	vertices2D = newPositions;
}

void MembraneComponent::sendOrganelles(double x, double y)
{
    organellePositions.emplace_back(x,y,0);
}

luabind::object MembraneComponent::getExternOrganellePos(double x, double y)
{
    luabind::object externalOrganellePosition = luabind::newtable(Game::instance().engine().luaState());

    Ogre::Vector3 organelleCoords = GetExternalOrganelle(x, y);
    externalOrganellePosition[1] = organelleCoords.x;
    externalOrganellePosition[2] = organelleCoords.y;


    return externalOrganellePosition;
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
    System::initNamed("MembraneSystem", gameState);
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
            if(!membraneComponent->isInitialized)
            {
                membraneComponent->Initialize();
            }
            membraneComponent->Update();

            //If the mesh already exists, destroy the old one
            Ogre::MeshManager::getSingleton().remove(sceneNodeComponent->m_meshName.get());
            // Create a mesh and a submesh.
            Ogre::MeshPtr msh = Ogre::MeshManager::getSingleton().createManual(sceneNodeComponent->m_meshName.get(), "General");
            Ogre::SubMesh* sub = msh->createSubMesh();

            // Define the vertices.
            std::vector<double> vertexData;
            for(size_t i=0, end=membraneComponent->MeshPoints.size(); i<end; i++)
            {
                // Vertex.
                vertexData.push_back(membraneComponent->MeshPoints[i].x);
                vertexData.push_back(membraneComponent->MeshPoints[i].y);
                vertexData.push_back(membraneComponent->MeshPoints[i].z);

                // Normal.
                //vertexData.push_back(component->MyMembrane.Normals[i].x);
                //vertexData.push_back(component->MyMembrane.Normals[i].y);
                //vertexData.push_back(component->MyMembrane.Normals[i].z);
                vertexData.push_back(0.0);
                vertexData.push_back(0.0);
                vertexData.push_back(1.0);

                // UV coordinates.
                vertexData.push_back(membraneComponent->UVs[i].x);
                vertexData.push_back(membraneComponent->UVs[i].y);
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


