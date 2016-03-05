#include "microbe_stage/compound_cloud_system.h"
#include "microbe_stage/membrane_system.h"

#include "bullet/collision_system.h"
#include "bullet/rigid_body_system.h"
#include "engine/component_factory.h"
#include "engine/engine.h"
#include "engine/entity_filter.h"
#include "engine/entity.h"
#include "engine/game_state.h"
#include "engine/player_data.h"
#include "engine/serialization.h"
#include "game.h"
#include "ogre/scene_node_system.h"
#include "scripting/luabind.h"
#include "util/make_unique.h"

#include <iostream>
#include <errno.h>
#include <stdio.h>
#include <OgreMeshManager.h>
#include <OgreMaterialManager.h>
#include <OgreMaterial.h>
#include <OgreTextureManager.h>
#include <OgreTechnique.h>
#include <OgreRoot.h>
#include <OgreSubMesh.h>

#include <string.h>
#include <cstdio>

#include <chrono>

using namespace thrive;

////////////////////////////////////////////////////////////////////////////////
// CompoundCloudComponent
////////////////////////////////////////////////////////////////////////////////

luabind::scope
CompoundCloudComponent::luaBindings() {
    using namespace luabind;
    return class_<CompoundCloudComponent, Component>("CompoundCloudComponent")
        .enum_("ID") [
            value("TYPE_ID", CompoundCloudComponent::TYPE_ID)
        ]
        .scope [
            def("TYPE_NAME", &CompoundCloudComponent::TYPE_NAME)
        ]
        .def(constructor<>())
        .def("initialize", &CompoundCloudComponent::initialize)
        .def("addCloud", &CompoundCloudComponent::addCloud)
        .def_readonly("width", &CompoundCloudComponent::width)
        .def_readonly("height", &CompoundCloudComponent::height)
        .def_readonly("gridSize", &CompoundCloudComponent::gridSize)
    ;
}

void
CompoundCloudComponent::initialize(
    CompoundId id,
    float red,
    float green,
    float blue
) {
    m_compoundId = id;
    color = Ogre::ColourValue(red, green, blue);
}

void
CompoundCloudComponent::load(
    const StorageContainer& storage
) {
    Component::load(storage);

    m_compoundId = storage.get<CompoundId>("id", NULL_COMPOUND);
    color = storage.get<Ogre::ColourValue>("color", Ogre::ColourValue(0,0,0));
    width = storage.get<int>("width", 0);
    height = storage.get<int>("height", 0);
    gridSize = storage.get<float>("gridSize", 0.0);


}

StorageContainer
CompoundCloudComponent::storage() const {
    StorageContainer storage = Component::storage();

    storage.set<CompoundId>("id", m_compoundId);
    storage.set<Ogre::ColourValue>("color", color);
    storage.set<int>("width", width);
    storage.set<int>("height", height);
    storage.set<float>("gridSize", gridSize);

    return storage;
}

void
CompoundCloudComponent::addCloud(float dens, int x, int y) {
    if ((x-offsetX)/gridSize+width/2 >= 0 && (x-offsetX)/gridSize+width/2 < width &&
        (y-offsetY)/gridSize+height/2 >= 0 && (y-offsetY)/gridSize+height/2 < height)
    {
        density[(x-offsetX)/gridSize+width/2][(y-offsetY)/gridSize+height/2] += dens;
    }
}

int
CompoundCloudComponent::takeCompound(int x, int y, float rate) {

    if (x >= 0 && x < width && y >= 0 && y < height)
    {
        int amountToGive = static_cast<int>(density[x][y])*rate;
        density[x][y] -= amountToGive;
        if (density[x][y] < 1) density[x][y] = 0;

        return amountToGive;
    }

    return -1;

}

int
CompoundCloudComponent::amountAvailable(int x, int y, float rate) {

    if (x >= 0 && x < width && y >= 0 && y < height)
    {
        int amountToGive = static_cast<int>(density[x][y])*rate;

        return amountToGive;
    }

    return -1;

}

REGISTER_COMPONENT(CompoundCloudComponent)


////////////////////////////////////////////////////////////////////////////////
// CompoundCloudSystem
////////////////////////////////////////////////////////////////////////////////

luabind::scope
CompoundCloudSystem::luaBindings() {
    using namespace luabind;
    return class_<CompoundCloudSystem, System>("CompoundCloudSystem")
        .def(constructor<>())
    ;
}


struct CompoundCloudSystem::Implementation {
    // All entities that have a compoundCloudsComponent.
    // These should be the various compounds (glucose, ammonia) as well as toxins.
    EntityFilter<
        CompoundCloudComponent
    > m_compounds = {true};

    Ogre::SceneManager* m_sceneManager = nullptr;
};


CompoundCloudSystem::CompoundCloudSystem()
  : m_impl(new Implementation()),
    playerNode(NULL),
    noiseScale(5),
    width(60),
    height(60),
    offsetX(0),
    offsetY(0),
    gridSize(2),
    xVelocity(width, std::vector<float>(height, 0)),
    yVelocity(width, std::vector<float>(height, 0))
{
    // Use the curl of a Perlin noise field to create a turbulent velocity field.
    CreateVelocityField();
}

CompoundCloudSystem::~CompoundCloudSystem() {
}


void
CompoundCloudSystem::init(
    GameState* gameState
) {
    System::initNamed("CompoundCloudSystem", gameState);
    m_impl->m_compounds.setEntityManager(&gameState->entityManager());
    m_impl->m_sceneManager = gameState->sceneManager();
    this->gameState = gameState;

    // Create a background plane on which the fluid clouds will be drawn.
    Ogre::Plane plane(Ogre::Vector3::UNIT_Z, -1.0);
    Ogre::MeshManager::getSingleton().createPlane("CompoundCloudsPlane", "General", plane, width*gridSize, height*gridSize, 1, 1, true, 1, 1, 1, Ogre::Vector3::UNIT_Y);
    compoundCloudsPlane = m_impl->m_sceneManager->createEntity("CompoundCloudsPlane", "General");
    m_impl->m_sceneManager->getRootSceneNode()->createChildSceneNode()->attachObject(compoundCloudsPlane);
    compoundCloudsPlane->setMaterialName("CompoundClouds");
}


void
CompoundCloudSystem::shutdown() {
    m_impl->m_compounds.setEntityManager(nullptr);
    m_impl->m_sceneManager = nullptr;
    System::shutdown();
}


void
CompoundCloudSystem::update(int renderTime, int) {

//auto start = std::chrono::high_resolution_clock::now();

    // Get the player's position.
    playerNode = static_cast<OgreSceneNodeComponent*>(gameState->entityManager().getComponent(
        Entity(gameState->engine().playerData().playerName(), gameState).id(),
        OgreSceneNodeComponent::TYPE_ID));


    // If the player moves out of the current grid, move the grid.
    if (playerNode->m_transform.position.x > offsetX + width/3*gridSize/2  ||
        playerNode->m_transform.position.y > offsetY + height/3*gridSize/2 ||
        playerNode->m_transform.position.x < offsetX - width/3*gridSize/2  ||
        playerNode->m_transform.position.y < offsetY - height/3*gridSize/2)
    {
        if (playerNode->m_transform.position.x > offsetX + width/3*gridSize/2 ) offsetX += width/3*gridSize;
        if (playerNode->m_transform.position.y > offsetY + height/3*gridSize/2) offsetY += height/3*gridSize;
        if (playerNode->m_transform.position.x < offsetX - width/3*gridSize/2 ) offsetX -= width/3*gridSize;
        if (playerNode->m_transform.position.y < offsetY - height/3*gridSize/2) offsetY -= height/3*gridSize;

        compoundCloudsPlane->getParentSceneNode()->setPosition(offsetX, offsetY, -1.0);
    }

    // For all newly created entities, initialize their parameters.
    for (auto& value : m_impl->m_compounds.addedEntities()) {
        CompoundCloudComponent* compoundCloud = std::get<0>(value.second);

        // Set the size of each grid tile and its position.
        compoundCloud->width = width;
        compoundCloud->height = height;
        compoundCloud->offsetX = offsetX;
        compoundCloud->offsetY = offsetY;
        compoundCloud->gridSize = gridSize;

        compoundCloud->density.resize(width, std::vector<float>(height, 0));
        compoundCloud->oldDens.resize(width, std::vector<float>(height, 0));

        // Modifies the material to draw this compound cloud in addition to the others.
        Ogre::MaterialPtr materialPtr = Ogre::MaterialManager::getSingleton().getByName("CompoundClouds", "General");
        Ogre::Pass* pass = materialPtr->getTechnique(0)->createPass();
        pass->setSceneBlending(Ogre::SBT_TRANSPARENT_ALPHA);
        pass->setVertexProgram("CompoundCloud_VS");
        pass->setFragmentProgram("CompoundCloud_PS");
        //Ogre::TexturePtr texturePtr = Ogre::TextureManager::getSingleton().load(compoundCloud->compound + ".bmp", "General");
        Ogre::TexturePtr texturePtr = Ogre::TextureManager::getSingleton().createManual(CompoundRegistry::getCompoundInternalName(compoundCloud->m_compoundId), "General", Ogre::TEX_TYPE_2D, width, height,
                                                                                        0, Ogre::PF_BYTE_BGRA, Ogre::TU_DYNAMIC_WRITE_ONLY_DISCARDABLE);
        Ogre::HardwarePixelBufferSharedPtr cloud;
        cloud = texturePtr->getBuffer();
        cloud->lock(Ogre::HardwareBuffer::HBL_DISCARD);
        const Ogre::PixelBox& pixelBox = cloud->getCurrentLock();
        uint8_t* pDest = static_cast<uint8_t*>(pixelBox.data);
        // Fill in some pixel data. This will give a semi-transparent blue,
        // but this is of course dependent on the chosen pixel format.
        for (int i = 0; i < width; i++)
        {
            for(int j = 0; j < height; j++)
            {
                // Set the colors in Blue, Green, Red, Alpha format.
                *pDest++ = compoundCloud->color.b;
                *pDest++ = compoundCloud->color.g;
                *pDest++ = compoundCloud->color.r;
                *pDest++ = 0;
            }
            pDest += pixelBox.getRowSkip() * Ogre::PixelUtil::getNumElemBytes(pixelBox.format);
        }
        // Unlock the pixel buffer
        cloud->unlock();
        pass->createTextureUnitState(CompoundRegistry::getCompoundInternalName(compoundCloud->m_compoundId));

        texturePtr = Ogre::TextureManager::getSingleton().load("PerlinNoise.jpg", "General");
        pass->createTextureUnitState()->setTexture(texturePtr);

        compoundCloudsPlane->getSubEntity(0)->setCustomParameter(1, Ogre::Vector4(0.0f, 0.0f, 0.0f, 0.0f));
    }
    // Clear the list of newly added entities so that we don't reinitialize them next frame.
    m_impl->m_compounds.clearChanges();

    // For all types of compound clouds...
    for (auto& value : m_impl->m_compounds)
    {
        CompoundCloudComponent* compoundCloud = std::get<0>(value.second);
        // If the offset of the compound cloud is different from the fluid systems offset,
        // then the player must have moved, so we need to adjust the texture.
        if (compoundCloud->offsetX != offsetX || compoundCloud->offsetY != offsetY)
        {
            // If we moved up.
            if (compoundCloud->offsetX == offsetX && compoundCloud->offsetY < offsetY)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height/3; y++)
                    {
                        compoundCloud->density[x][y] = compoundCloud->density[x][y+height/3];
                        compoundCloud->density[x][y+height/3] = compoundCloud->density[x][y+height*2/3];
                        compoundCloud->density[x][y+height*2/3] = 0.0;
                    }
                }
                Ogre::Vector4 offset = compoundCloudsPlane->getSubEntity(0)->getCustomParameter(1);
                compoundCloudsPlane->getSubEntity(0)->setCustomParameter(1, Ogre::Vector4(offset.x, offset.y-1.0f/3, 0.0f, 0.0f));
            }
            // If we moved right.
            else if (compoundCloud->offsetX < offsetX && compoundCloud->offsetY == offsetY)
            {
                for (int x = 0; x < width/3; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        compoundCloud->density[x][y] = compoundCloud->density[x+height/3][y];
                        compoundCloud->density[x+height/3][y] = compoundCloud->density[x+height*2/3][y];
                        compoundCloud->density[x+height*2/3][y] = 0.0;
                    }
                }
                Ogre::Vector4 offset = compoundCloudsPlane->getSubEntity(0)->getCustomParameter(1);
                compoundCloudsPlane->getSubEntity(0)->setCustomParameter(1, Ogre::Vector4(offset.x-1.0f/3, offset.y, 0.0f, 0.0f));
            }
            // If we moved left.
            else if (compoundCloud->offsetX > offsetX && compoundCloud->offsetY == offsetY)
            {
                for (int x = 0; x < width/3; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        compoundCloud->density[x+height*2/3][y] = compoundCloud->density[x+height/3][y];
                        compoundCloud->density[x+height/3][y] = compoundCloud->density[x][y];
                        compoundCloud->density[x][y] = 0.0;
                    }
                }
                Ogre::Vector4 offset = compoundCloudsPlane->getSubEntity(0)->getCustomParameter(1);
                compoundCloudsPlane->getSubEntity(0)->setCustomParameter(1, Ogre::Vector4(offset.x+1.0f/3, offset.y, 0.0f, 0.0f));
            }
            // If we moved downwards.
            else if (compoundCloud->offsetX == offsetX && compoundCloud->offsetY > offsetY)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height/3; y++)
                    {
                        compoundCloud->density[x][y+height*2/3] = compoundCloud->density[x][y+height/3];
                        compoundCloud->density[x][y+height/3] = compoundCloud->density[x][y];
                        compoundCloud->density[x][y] = 0.0;
                    }
                }
                Ogre::Vector4 offset = compoundCloudsPlane->getSubEntity(0)->getCustomParameter(1);
                compoundCloudsPlane->getSubEntity(0)->setCustomParameter(1, Ogre::Vector4(offset.x, offset.y+1.0f/3, 0.0f, 0.0f));
            }
            compoundCloud->offsetX = offsetX;
            compoundCloud->offsetY = offsetY;
        }
        // Compound clouds move from area of high concentration to area of low.
        diffuse(.01, compoundCloud->oldDens, compoundCloud->density, renderTime);
        // Move the compound clouds about the velocity field.
        advect(compoundCloud->oldDens, compoundCloud->density, renderTime);

        // Store the pixel data in a hardware buffer for quick access.
        Ogre::HardwarePixelBufferSharedPtr cloud;
        cloud = Ogre::TextureManager::getSingleton().getByName(CompoundRegistry::getCompoundInternalName(compoundCloud->m_compoundId), "General")->getBuffer();
        cloud->lock(Ogre::HardwareBuffer::HBL_DISCARD);
        const Ogre::PixelBox& pixelBox = cloud->getCurrentLock();
        uint8_t* pDest = static_cast<uint8_t*>(pixelBox.data);
        pDest+=3;

        // Copy the density vector into the buffer.
        for (int j = 0; j < height; j++)
        {
            for(int i = 0; i < width; i++)
            {
                int intensity = static_cast<int>(compoundCloud->density[i][height-j-1]);

                if (intensity < 0)
                {
                    intensity = 0;
                }
                else if (intensity > 255)
                {
                    intensity = 255;
                }
                *pDest = intensity; // Alpha value of texture.
                pDest+=4;
            }
            pDest += pixelBox.getRowSkip() * Ogre::PixelUtil::getNumElemBytes(pixelBox.format);
        }
        // Unlock the pixel buffer.
        cloud->unlock();
    }


//auto end = std::chrono::high_resolution_clock::now();

//std::cout << "total: " << std::to_string(std::chrono::duration_cast<std::chrono::microseconds>(end-start).count()) << std::endl;
}

void
CompoundCloudSystem::CreateVelocityField() {
    float nxScale = noiseScale;
	float nyScale = nxScale * float(width) / float(height);
	float x0, y0, x1, y1, n0, n1, nx, ny;

	for (int x = 0; x < width; x++)
	{
		for (int y = 0; y < height; y++)
		{
			x0 = (float(x - 1) / float(width))  * nxScale;
			y0 = (float(y - 1) / float(height)) * nyScale;
			x1 = (float(x + 1) / float(width))  * nxScale;
			y1 = (float(y + 1) / float(height)) * nyScale;

			n0 = fieldPotential.noise(x0, y0, 0);
			n1 = fieldPotential.noise(x1, y0, 0);
			ny = n0 - n1;
			n0 = fieldPotential.noise(x0, y0, 0);
			n1 = fieldPotential.noise(x0, y1, 0);
			nx = n1 - n0;

			xVelocity[x][y] = nx/2;
			yVelocity[x][y] = ny/2;
		}
	}
}

void
CompoundCloudSystem::diffuse(float diffRate, std::vector<  std::vector<float>  >& oldDens, const std::vector<  std::vector<float>  >& density, int dt) {
    dt = 1;
    float a = dt*diffRate;

    for (int x = 1; x < width-1; x++)
    {
        for (int y = 1; y < height-1; y++)
        {
            oldDens[x][y] = (density[x][y] + a*(oldDens[x - 1][y] + oldDens[x + 1][y] +
                oldDens[x][y-1] + oldDens[x][y+1])) / (1 + 4 * a);
        }
    }
}

void
CompoundCloudSystem::advect(std::vector<  std::vector<float>  >& oldDens, std::vector<  std::vector<float>  >& density, int dt) {
    dt = 1;

    for (int x = 0; x < width; x++)
	{
		for (int y = 0; y < height; y++)
		{
			density[x][y] = 0;
		}
	}

    float dx, dy;
    int x0, x1, y0, y1;
    float s1, s0, t1, t0;
	for (int x = 1; x < width-1; x++)
	{
		for (int y = 1; y < height-1; y++)
		{
		    if (oldDens[x][y] > 1) {
                dx = x + dt*xVelocity[x][y];
                dy = y + dt*yVelocity[x][y];

                if (dx < 0.5) dx = 0.5;
                if (dx > width - 1.5) dx = width - 1.5f;

                if (dy < 0.5) dy = 0.5;
                if (dy > height - 1.5) dy = height - 1.5f;

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
