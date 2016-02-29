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
    std::string name,
    float red,
    float green,
    float blue
) {
    compound = name;
    color = Ogre::ColourValue(red, green, blue);
}

void
CompoundCloudComponent::load(
    const StorageContainer& storage
) {
    Component::load(storage);
}

StorageContainer
CompoundCloudComponent::storage() const {
    StorageContainer storage = Component::storage();

    return storage;
}

void
CompoundCloudComponent::addCloud(float dens, int x, int y) {

    // The added cloud goes into the primary grid tile that the player is in.
    if ((x-offsetX)/gridSize+width/2 >= 0 && (x-offsetX)/gridSize+width/2 < width &&
        (y-offsetY)/gridSize+height/2 >= 0 && (y-offsetY)/gridSize+height/2 < height)
    {
        density[(x-offsetX)/gridSize+width/2][(y-offsetY)/gridSize+height/2] += dens;
    }
    // The added cloud goes into the 8 tiles surrounding the player. These will be "activated" once the player enters them.
    else if (x-offsetX > -width*gridSize*3/2 && x-offsetX < width*gridSize*3/2 &&
             y-offsetY > -height*gridSize*3/2 && y-offsetY < height*gridSize*3/2)
    {
        // Left column.
        if (x-offsetX > -width*gridSize*3/2 && x-offsetX < -width*gridSize/2)
        {
            // Top left.
            if (y-offsetY > height*gridSize/2 && y-offsetY < height*gridSize*3/2)
            {
                density_11[(x-offsetX)/gridSize+width*3/2][(y-offsetY)/gridSize-height/2] += dens;
            }
            // Middle left.
            else if (y-offsetY > -height*gridSize/2 && y-offsetY < height*gridSize/2)
            {
                density_21[(x-offsetX)/gridSize+width*3/2][(y-offsetY)/gridSize+height/2] += dens;
            }
            // Bottom left.
            else if (y-offsetY > -height*gridSize*3/2 && y-offsetY < -height*gridSize/2)
            {
                density_31[(x-offsetX)/gridSize+width*3/2][(y-offsetY)/gridSize+height*3/2] += dens;
            }
        }
        // Middle column.
        else if (x-offsetX > -width*gridSize/2 && x-offsetX < width*gridSize/2)
        {
            // Middle top.
            if (y-offsetY > height*gridSize/2 && y-offsetY < height*gridSize*3/2)
            {
                density_12[(x-offsetX)/gridSize+width/2][(y-offsetY)/gridSize+height*3/2] += dens;
            }
            // Middle bottom.
            else if (y-offsetY > -height*gridSize*3/2 && y-offsetY < -height*gridSize/2)
            {
                density_32[(x-offsetX)/gridSize+width/2][(y-offsetY)/gridSize-height/2] += dens;
            }

        }
        // Right column.
        else if (x-offsetX > width*gridSize/2 && x-offsetX < width*gridSize*3/2)
        {
            // Top right.
            if (y-offsetY > height*gridSize/2 && y-offsetY < height*gridSize*3/2)
            {
                density_13[(x-offsetX)/gridSize-width/2][(y-offsetY)/gridSize-height/2] += dens;
            }
            // Middle right.
            else if (y-offsetY > -height*gridSize/2 && y-offsetY < height*gridSize/2)
            {
                density_23[(x-offsetX)/gridSize-width/2][(y-offsetY)/gridSize+height/2] += dens;
            }
            // Bottom right.
            else if (y-offsetY > -height*gridSize*3/2 && y-offsetY < -height*gridSize/2)
            {
                density_33[(x-offsetX)/gridSize-width/2][(y-offsetY)/gridSize+height*3/2] += dens;
            }
        }
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
    width(50),
    height(50),
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
    delete f;
}


void
CompoundCloudSystem::init(
    GameState* gameState
) {
    System::init(gameState);
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
    // Delete all the temporary files created by this system.
    for (auto& value : m_impl->m_compounds.addedEntities()) {
        CompoundCloudComponent* compoundCloud = std::get<0>(value.second);
        std::string filename = "../tmp/" + compoundCloud->compound + ".bmp";
        remove(filename.c_str());
    }
    m_impl->m_compounds.setEntityManager(nullptr);
    m_impl->m_sceneManager = nullptr;
    System::shutdown();
}


void
CompoundCloudSystem::update(int renderTime, int) {
    std::cout << "Start" << std::endl;
    // If we do not have a reference to the player scene node, get it.
    if (playerNode == NULL) {
        playerNode = static_cast<OgreSceneNodeComponent*>(gameState->entityManager().getComponent(
            Entity(gameState->engine().playerData().playerName(), gameState).id(),
            OgreSceneNodeComponent::TYPE_ID));
    }

    // If the player moves out of the current grid, move the grid.
    if (playerNode->m_transform.position.x > offsetX + width*gridSize/2  ||
        playerNode->m_transform.position.y > offsetY + height*gridSize/2 ||
        playerNode->m_transform.position.x < offsetX - width*gridSize/2  ||
        playerNode->m_transform.position.y < offsetY - height*gridSize/2)
    {
        if (playerNode->m_transform.position.x > offsetX + width*gridSize/2 ) offsetX += width*gridSize;
        if (playerNode->m_transform.position.y > offsetY + height*gridSize/2) offsetY += height*gridSize;
        if (playerNode->m_transform.position.x < offsetX - width*gridSize/2 ) offsetX -= width*gridSize;
        if (playerNode->m_transform.position.y < offsetY - height*gridSize/2) offsetY -= height*gridSize;

        compoundCloudsPlane->getParentSceneNode()->setPosition(offsetX, offsetY, -1.0);
    }
    std::cout << "Player moved" << std::endl;

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

        compoundCloud->density_11.resize(width, std::vector<float>(height, 0));
        compoundCloud->density_12.resize(width, std::vector<float>(height, 0));
        compoundCloud->density_13.resize(width, std::vector<float>(height, 0));
        compoundCloud->density_21.resize(width, std::vector<float>(height, 0));
        compoundCloud->density_23.resize(width, std::vector<float>(height, 0));
        compoundCloud->density_31.resize(width, std::vector<float>(height, 0));
        compoundCloud->density_32.resize(width, std::vector<float>(height, 0));
        compoundCloud->density_33.resize(width, std::vector<float>(height, 0));

        // Modifies the material to draw this compound cloud in addition to the others.
        Ogre::MaterialPtr materialPtr = Ogre::MaterialManager::getSingleton().getByName("CompoundClouds", "General");
        Ogre::Pass* pass = materialPtr->getTechnique(0)->createPass();
        pass->setSceneBlending(Ogre::SBT_TRANSPARENT_ALPHA);
        pass->setVertexProgram("CompoundCloud_VS");
        pass->setFragmentProgram("CompoundCloud_PS");
        //Ogre::TexturePtr texturePtr = Ogre::TextureManager::getSingleton().load(compoundCloud->compound + ".bmp", "General");
        Ogre::TexturePtr texturePtr = Ogre::TextureManager::getSingleton().createManual(compoundCloud->compound, "General", Ogre::TEX_TYPE_2D, width, height,
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
                *pDest++ = 0; // R
                *pDest++ = 0; // G
                *pDest++ = 0; // B
                *pDest++ = 0; // A
            }
            pDest += pixelBox.getRowSkip() * Ogre::PixelUtil::getNumElemBytes(pixelBox.format);
        }
        // Unlock the pixel buffer
        cloud->unlock();
        pass->createTextureUnitState(compoundCloud->compound);



        texturePtr = Ogre::TextureManager::getSingleton().load("PerlinNoise.jpg", "General");
        pass->createTextureUnitState()->setTexture(texturePtr);
    }
    // Clear the list of newly added entities so that we don't reinitialize them next frame.
    m_impl->m_compounds.clearChanges();

    // For all types of compound clouds...
    for (auto& value : m_impl->m_compounds)
    {
        CompoundCloudComponent* compoundCloud = std::get<0>(value.second);
        std::cout << "entered update loop for: " << compoundCloud->compound << std::endl;

        // If the offset of the compound cloud is different from the fluid systems offset,
        // then the player must have moved, so we need to adjust the 3x3 grid.
        if (compoundCloud->offsetX != offsetX || compoundCloud->offsetY != offsetY)
        {
            std::cout << "Getting ready to move: " << std::to_string(compoundCloud->offsetX) << " to "
                      << std::to_string(offsetX) << " and " << std::to_string(compoundCloud->offsetY) << " to "
                      << std::to_string(offsetY) << std::endl;
            // If we moved to the top tile.
            if (compoundCloud->offsetX == offsetX && compoundCloud->offsetY < offsetY)
            {
                // Move bottom row up.
                compoundCloud->density_31.swap(compoundCloud->density_21);
                compoundCloud->density_32.swap(compoundCloud->density);
                compoundCloud->density_33.swap(compoundCloud->density_23);

                // Move middle row up.
                compoundCloud->density_21.swap(compoundCloud->density_11);
                compoundCloud->density.swap(compoundCloud->density_12);
                compoundCloud->density_23.swap(compoundCloud->density_13);

                // Create the new bottom row and old density .
                std::fill(compoundCloud->density_11.begin(), compoundCloud->density_11.begin()+width, std::vector<float>(height, 0.0));
                std::fill(compoundCloud->density_12.begin(), compoundCloud->density_12.begin()+width, std::vector<float>(height, 0.0));
                std::fill(compoundCloud->density_13.begin(), compoundCloud->density_13.begin()+width, std::vector<float>(height, 0.0));
                std::fill(compoundCloud->oldDens.begin(), compoundCloud->oldDens.begin()+width, std::vector<float>(height, 0.0));
            }
            // If we moved to the right tile.
            else if (compoundCloud->offsetX < offsetX && compoundCloud->offsetY == offsetY)
            {
                // Move left row right.
                compoundCloud->density_11.swap(compoundCloud->density_12);
                compoundCloud->density_21.swap(compoundCloud->density);
                compoundCloud->density_31.swap(compoundCloud->density_32);

                // Move middle row right.
                compoundCloud->density_12.swap(compoundCloud->density_13);
                compoundCloud->density.swap(compoundCloud->density_23);
                compoundCloud->density_32.swap(compoundCloud->density_33);

                // Create the new right row and old density.
                std::fill(compoundCloud->density_13.begin(), compoundCloud->density_13.begin()+width, std::vector<float>(height, 0.0));
                std::fill(compoundCloud->density_23.begin(), compoundCloud->density_23.begin()+width, std::vector<float>(height, 0.0));
                std::fill(compoundCloud->density_33.begin(), compoundCloud->density_33.begin()+width, std::vector<float>(height, 0.0));
                std::fill(compoundCloud->oldDens.begin(), compoundCloud->oldDens.begin()+width, std::vector<float>(height, 0.0));
            }
            // If we moved to the left tile.
            else if (compoundCloud->offsetX > offsetX && compoundCloud->offsetY == offsetY)
            {
                // Move right row left.
                compoundCloud->density_13.swap(compoundCloud->density_12);
                compoundCloud->density_23.swap(compoundCloud->density);
                compoundCloud->density_33.swap(compoundCloud->density_32);

                // Move middle row left.
                compoundCloud->density_12.swap(compoundCloud->density_11);
                compoundCloud->density.swap(compoundCloud->density_21);
                compoundCloud->density_32.swap(compoundCloud->density_31);

                // Create the new left row and old density.
                std::fill(compoundCloud->density_11.begin(), compoundCloud->density_11.begin()+width, std::vector<float>(height, 0.0));
                std::fill(compoundCloud->density_21.begin(), compoundCloud->density_21.begin()+width, std::vector<float>(height, 0.0));
                std::fill(compoundCloud->density_31.begin(), compoundCloud->density_31.begin()+width, std::vector<float>(height, 0.0));
                std::fill(compoundCloud->oldDens.begin(), compoundCloud->oldDens.begin()+width, std::vector<float>(height, 0.0));
            }
            // If we moved to the bottom tile.
            else if (compoundCloud->offsetX == offsetX && compoundCloud->offsetY > offsetY)
            {
                // Move top row down.
                compoundCloud->density_11.swap(compoundCloud->density_21);
                compoundCloud->density_12.swap(compoundCloud->density);
                compoundCloud->density_13.swap(compoundCloud->density_23);

                // Move middle row up.
                compoundCloud->density_21.swap(compoundCloud->density_31);
                compoundCloud->density.swap(compoundCloud->density_32);
                compoundCloud->density_23.swap(compoundCloud->density_33);

                // Create the new top row and old density.
                std::fill(compoundCloud->density_31.begin(), compoundCloud->density_31.begin()+width, std::vector<float>(height, 0.0));
                std::fill(compoundCloud->density_32.begin(), compoundCloud->density_32.begin()+width, std::vector<float>(height, 0.0));
                std::fill(compoundCloud->density_33.begin(), compoundCloud->density_33.begin()+width, std::vector<float>(height, 0.0));
                std::fill(compoundCloud->oldDens.begin(), compoundCloud->oldDens.begin()+width, std::vector<float>(height, 0.0));
            }
            compoundCloud->offsetX = offsetX;
            compoundCloud->offsetY = offsetY;
            std::cout << "Moved the tiles" << std::endl;
        }
        std::cout << "Left loop" << std::endl;

        // Compound clouds move from area of high concentration to area of low.
        diffuse(.01, compoundCloud->oldDens, compoundCloud->density, renderTime);
        // Move the compound clouds about the velocity field.
        advect(compoundCloud->oldDens, compoundCloud->density, renderTime);

        //auto start = std::chrono::high_resolution_clock::now();
        std::cout << "Before Buffer" << std::endl;
        Ogre::HardwarePixelBufferSharedPtr cloud;
        cloud = Ogre::TextureManager::getSingleton().getByName(compoundCloud->compound, "General")->getBuffer();
        cloud->lock(Ogre::HardwareBuffer::HBL_DISCARD);
        const Ogre::PixelBox& pixelBox = cloud->getCurrentLock();
        uint8_t* pDest = static_cast<uint8_t*>(pixelBox.data);
        // Fill in some pixel data. This will give a semi-transparent blue,
        // but this is of course dependent on the chosen pixel format.
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


                *pDest++ = compoundCloud->color.b; // B
                *pDest++ = compoundCloud->color.g; // G
                *pDest++ = compoundCloud->color.r; // R
                *pDest++ = intensity; // A
            }
            pDest += pixelBox.getRowSkip() * Ogre::PixelUtil::getNumElemBytes(pixelBox.format);
        }
        // Unlock the pixel buffer
        cloud->unlock();
        std::cout << "After buffer" << std::endl;

        //auto end = std::chrono::high_resolution_clock::now();
        //auto diff = std::chrono::duration_cast<std::chrono::microseconds>(end-start);
        //std::cout << diff.count() << std::endl;
    }
}

void
CompoundCloudSystem::CreateVelocityField() {
    float nxScale = noiseScale;
	float nyScale = nxScale * float(width) / float(height);
	float x0, y0, x1, y1, n0, n1, nx, ny;

	for (int x = 0; x<width; x++)
	{
		for (int y = 0; y<height; y++)
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

			xVelocity[x][y] = nx/3;
			yVelocity[x][y] = ny/3;
		}
	}
}

void
CompoundCloudSystem::diffuse(float diffRate, std::vector<  std::vector<float>  >& oldDens, const std::vector<  std::vector<float>  >& density, int dt) {
    dt = 1;
    float a = dt*diffRate;

    for (int x = 1; x<width - 1; x++)
    {
        for (int y = 1; y<height - 1; y++)
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
	for (int x = 0; x < width - 1; x++)
	{
		for (int y = 0; y < height - 1; y++)
		{
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


void
CompoundCloudSystem::writeToFile(std::vector<  std::vector<float>  >& density, std::string compoundName, Ogre::ColourValue color) {
    int w = width;
	int h = height;

	unsigned char* img = NULL;
	int filesize = 54 + 3*w*h;
	if (img)
		free(img);
	img = static_cast<unsigned char*>(malloc(3*w*h));
	memset(img, 0, sizeof(*img));

	int intensity;
	int red, green, blue;
	int x, y;


    for (int j = h-1; j >= 0; j--)
    {
        for (int i = 0; i < width; i++)
        {
			intensity = static_cast<int>(density[i][j]);

			if (intensity < 0)
			{
				red = 0; green = 0; blue = 0;
			}
			else if (intensity < 255)
			{
				red = intensity*color.r/256; green = intensity*color.g/256; blue = intensity*color.b/256;
			}
			else
			{
				red = 255*color.r/256; green = 255*color.g/256; blue = 255*color.b/256;
			}

			x = i; y = (h - 1) - j;

			img[(x + y*w) * 3 + 2] = static_cast<unsigned char>(red);
			img[(x + y*w) * 3 + 1] = static_cast<unsigned char>(green);
			img[(x + y*w) * 3 + 0] = static_cast<unsigned char>(blue);
		}
	}

	unsigned char bmpfileheader[14] = { 'B','M', 0,0,0,0, 0,0, 0,0, 54,0,0,0 };
	unsigned char bmpinfoheader[40] = { 40,0,0,0, 0,0,0,0, 0,0,0,0, 1,0, 24,0 };
	unsigned char bmppad[3] = { 0,0,0 };

	bmpfileheader[2] = static_cast<unsigned char>(filesize);
	bmpfileheader[3] = static_cast<unsigned char>(filesize >> 8);
	bmpfileheader[4] = static_cast<unsigned char>(filesize >> 16);
	bmpfileheader[5] = static_cast<unsigned char>(filesize >> 24);

	bmpinfoheader[4] = static_cast<unsigned char>(w);
	bmpinfoheader[5] = static_cast<unsigned char>(w >> 8);
	bmpinfoheader[6] = static_cast<unsigned char>(w >> 16);
	bmpinfoheader[7] = static_cast<unsigned char>(w >> 24);
	bmpinfoheader[8] = static_cast<unsigned char>(h);
	bmpinfoheader[9] = static_cast<unsigned char>(h >> 8);
	bmpinfoheader[10]= static_cast<unsigned char>(h >> 16);
	bmpinfoheader[11]= static_cast<unsigned char>(h >> 24);

    std::string filename = "../tmp/" + compoundName + ".bmp";
	errno_t err = fopen_s(&f, filename.c_str(), "wb");
	if (!err) {
		fwrite(bmpfileheader, 1, 14, f);
		fwrite(bmpinfoheader, 1, 40, f);
		for (int i = 0; i < h; i++)
		{
			fwrite(img + (w*(h - i - 1) * 3), 3, w, f);
			fwrite(bmppad, 1, (4 - (w * 3) % 4) % 4, f);
		}
		fclose(f);
	}
}

void
CompoundCloudSystem::initializeFile(std::string compoundName) {
    int w = width;
	int h = height;

	unsigned char* img = NULL;
	int filesize = 54 + 3*w*h;
	if (img)
		free(img);
	img = static_cast<unsigned char*>(malloc(3*w*h));
	memset(img, 0, sizeof(*img));

	int x, y;
    for (int j = h-1; j >= 0; j--)
    {
        for (int i = 0; i < width; i++)
        {
			x = i; y = (h - 1) - j;

			img[(x + y*w) * 3 + 2] = static_cast<unsigned char>(0);
			img[(x + y*w) * 3 + 1] = static_cast<unsigned char>(0);
			img[(x + y*w) * 3 + 0] = static_cast<unsigned char>(0);
		}
	}

	unsigned char bmpfileheader[14] = { 'B','M', 0,0,0,0, 0,0, 0,0, 54,0,0,0 };
	unsigned char bmpinfoheader[40] = { 40,0,0,0, 0,0,0,0, 0,0,0,0, 1,0, 24,0 };
	unsigned char bmppad[3] = { 0,0,0 };

	bmpfileheader[2] = static_cast<unsigned char>(filesize);
	bmpfileheader[3] = static_cast<unsigned char>(filesize >> 8);
	bmpfileheader[4] = static_cast<unsigned char>(filesize >> 16);
	bmpfileheader[5] = static_cast<unsigned char>(filesize >> 24);

	bmpinfoheader[4] = static_cast<unsigned char>(w);
	bmpinfoheader[5] = static_cast<unsigned char>(w >> 8);
	bmpinfoheader[6] = static_cast<unsigned char>(w >> 16);
	bmpinfoheader[7] = static_cast<unsigned char>(w >> 24);
	bmpinfoheader[8] = static_cast<unsigned char>(h);
	bmpinfoheader[9] = static_cast<unsigned char>(h >> 8);
	bmpinfoheader[10]= static_cast<unsigned char>(h >> 16);
	bmpinfoheader[11]= static_cast<unsigned char>(h >> 24);

    std::string filename = "../tmp/" + compoundName + ".bmp";
	errno_t err = fopen_s(&f, filename.c_str(), "wb");
	if (!err) {
		fwrite(bmpfileheader, 1, 14, f);
		fwrite(bmpinfoheader, 1, 40, f);
		for (int i = 0; i < h; i++)
		{
			fwrite(img + (w*(h - i - 1) * 3), 3, w, f);
			fwrite(bmppad, 1, (4 - (w * 3) % 4) % 4, f);
		}
		fclose(f);
	}
}
