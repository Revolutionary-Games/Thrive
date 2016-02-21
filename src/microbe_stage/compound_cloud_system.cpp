#include "microbe_stage/compound_cloud_system.h"

#include "bullet/collision_system.h"
#include "bullet/rigid_body_system.h"
#include "engine/component_factory.h"
#include "engine/engine.h"
#include "engine/entity_filter.h"
#include "engine/entity.h"
#include "engine/game_state.h"
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
#include <OgreEntity.h>
#include <OgreSceneManager.h>
#include <OgreRoot.h>
#include <OgreSubMesh.h>

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
        .def("addCloud", &CompoundCloudComponent::addCloud)
        .def_readonly("width", &CompoundCloudComponent::width)
        .def_readonly("height", &CompoundCloudComponent::height)
        .def_readonly("gridSize", &CompoundCloudComponent::gridSize)
    ;
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

    if (x >= 0 && x < width && y >= 0 && y < height) {
        density[x][y] += dens;
    }

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

    EntityFilter<
        CompoundCloudComponent
    > m_entities = {true};

    Ogre::SceneManager* m_sceneManager = nullptr;
};


CompoundCloudSystem::CompoundCloudSystem()
  : m_impl(new Implementation()),
    noiseScale(5),
    width(50),
    height(50),
    gridSize(2),
    xVelocity(width, std::vector<float>(height, 0)),
    yVelocity(width, std::vector<float>(height, 0))
{
    CreateVelocityField();
}

CompoundCloudSystem::~CompoundCloudSystem() {}


void
CompoundCloudSystem::init(
    GameState* gameState
) {
    System::init(gameState);
    m_impl->m_entities.setEntityManager(&gameState->entityManager());
    m_impl->m_sceneManager = gameState->sceneManager();

    Ogre::Plane plane(Ogre::Vector3::UNIT_Z, -0.5);
    Ogre::MeshManager::getSingleton().createPlane("CompoundClouds", "General", plane, width*gridSize, height*gridSize, 1, 1, true, 1, 1, 1, Ogre::Vector3::UNIT_Y);
    compoundClouds = m_impl->m_sceneManager->createEntity("CompoundClouds", "General");
    m_impl->m_sceneManager->getRootSceneNode()->createChildSceneNode()->attachObject(compoundClouds);
    compoundClouds->setMaterialName("CompoundClouds");
}


void
CompoundCloudSystem::shutdown() {
    m_impl->m_entities.setEntityManager(nullptr);
    m_impl->m_sceneManager = nullptr;
    System::shutdown();
}


void
CompoundCloudSystem::update(int renderTime, int) {
    for (auto& value : m_impl->m_entities.addedEntities()) {
        std::cout << "AddedEntities" << std::endl;

        CompoundCloudComponent* compoundCloud = std::get<0>(value.second);

        compoundCloud->width = width;
        compoundCloud->height = height;
        compoundCloud->gridSize = gridSize;

        compoundCloud->density.resize(width, std::vector<float>(height, 0));
        compoundCloud->oldDens.resize(width, std::vector<float>(height, 0));
    }
    m_impl->m_entities.clearChanges();

    for (auto& value : m_impl->m_entities) {
        CompoundCloudComponent* compoundCloud = std::get<0>(value.second);

        diffuse(.01, compoundCloud->oldDens, compoundCloud->density, renderTime);
        advect(compoundCloud->oldDens, compoundCloud->density, renderTime);

        writeToFile(compoundCloud->density);
    }
    Ogre::TexturePtr texture = Ogre::Root::getSingletonPtr()->getTextureManager()->getByName("fluid.bmp");
    texture->reload();
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
CompoundCloudSystem::writeToFile(std::vector<  std::vector<float>  >& density) {
    int w = width;
	int h = height;

	FILE *f;
	unsigned char *img = NULL;
	int filesize = 54 + 3*w*h;
	if (img)
		free(img);
	img = static_cast<unsigned char*>(malloc(3*w*h));
	memset(img, 0, sizeof(*img));

	int intensity;
	int red, green, blue;
	int x, y;

	for (int i = 0; i < width; i++)
	{
		for (int j = 0; j < height; j++)
		{
			intensity = static_cast<int>(density[i][j] * 10);

			if (intensity < 0)
			{
				red = 0; green = 0; blue = 0;
			}
			else if (intensity < 255)
			{
				red = 0; green = intensity; blue = intensity;
			}
			else
			{
				red = 0; green = 255; blue = 255;
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

	errno_t err = fopen_s(&f, "../materials/textures/fluid.bmp", "wb");
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
