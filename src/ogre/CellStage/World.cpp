#include "World.h"

World::World(Ogre::SceneManager* sceneMgr)
        :   mSceneMgr(sceneMgr)
{
    mBackgroundPlane.d = 10;
    mBackgroundPlane.normal = Ogre::Vector3::UNIT_Y;
    setBackground("Background/Blue1");
}

World::~World()
{}

bool World::setBackground(Ogre::String materialName)
{
    mSceneMgr->setSkyPlane(true,mBackgroundPlane,materialName,1,10);
}