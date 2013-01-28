#include "World.h"
#include <memory>

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
    mSceneMgr->setSkyPlane(true,mBackgroundPlane,materialName,10,15);
}

//bool World::mousePressed(const OIS::MouseEvent& arg, OIS::MouseButtonID id)
//{
//    if (id == OIS::MouseButtonID::MB_Right)
//    {
//    }
//}