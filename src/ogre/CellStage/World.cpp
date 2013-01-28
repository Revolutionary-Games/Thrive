#include "World.h"
#include <math.h>

World::World(Ogre::SceneManager* sceneMgr)
        :   mSceneMgr(sceneMgr)
{
    mBackgroundEnt = new Ogre::Entity*[4];
    mBackgroundNode = new Ogre::SceneNode*[4];
    for(int i = 0; i < 4; i++)
    {
        mBackgroundEnt[i] = mSceneMgr->createEntity(Ogre::SceneManager::PrefabType::PT_PLANE);
        mBackgroundNode[i] = mSceneMgr->getRootSceneNode()->createChildSceneNode();
        mBackgroundNode[i]->attachObject(mBackgroundEnt[i]);
        mBackgroundNode[i]->setScale(Ogre::Vector3::UNIT_SCALE);
    }
    setBackground("Background/Blue1");
    Update(Ogre::Vector3::ZERO);
}

World::~World()
{       
    if(mBackgroundEnt)  delete mBackgroundEnt;
    if(mBackgroundNode) delete mBackgroundNode;
}

bool World::Update(Ogre::Vector3 camNodePosition)
{
    Ogre::Real Spacing = 200.0f;
    
    Ogre::Vector3 scaledCamPos = camNodePosition / Spacing;
    Ogre::Real x = fmodf(scaledCamPos.x, 1.0f);
    Ogre::Real y = fmodf(scaledCamPos.y, 1.0f);
    
    Ogre::Vector3 basePos(scaledCamPos.x - x, scaledCamPos.y - y, 0);
    basePos.x += 0.5f;
    basePos.y += 0.5f;
    basePos *= Spacing;
    
    mBackgroundNode[0]->setPosition(basePos);
    
    if(x > .5f)
    {
        mBackgroundNode[1]->setPosition(basePos + Spacing * Ogre::Vector3::UNIT_X);
        if (y > .5f)
        {
            mBackgroundNode[2]->setPosition(basePos + Spacing * Ogre::Vector3::UNIT_Y);
            mBackgroundNode[3]->setPosition(basePos + Spacing * Ogre::Vector3::UNIT_X
                                                    + Spacing * Ogre::Vector3::UNIT_Y);
        }
        else
        {
            mBackgroundNode[2]->setPosition(basePos - Spacing * Ogre::Vector3::UNIT_Y);
            mBackgroundNode[3]->setPosition(basePos + Spacing * Ogre::Vector3::UNIT_X
                                                    - Spacing * Ogre::Vector3::UNIT_Y);
        }
    }
    else
    {
        mBackgroundNode[1]->setPosition(basePos - Spacing * Ogre::Vector3::UNIT_X);
        if (y > .5f)
        {
            mBackgroundNode[2]->setPosition(basePos + Spacing * Ogre::Vector3::UNIT_Y);
            mBackgroundNode[3]->setPosition(basePos - Spacing * Ogre::Vector3::UNIT_X
                                                    + Spacing * Ogre::Vector3::UNIT_Y);
        }
        else
        {
            mBackgroundNode[2]->setPosition(basePos - Spacing * Ogre::Vector3::UNIT_Y);
            mBackgroundNode[3]->setPosition(basePos - Spacing * Ogre::Vector3::UNIT_X
                                                    - Spacing * Ogre::Vector3::UNIT_Y);
        }
    }
    
    return true;
}

bool World::setBackground(Ogre::String materialName)
{
//    for (int i = 0; i < 4; i++)
//        mBackgroundEnt[i]->setMaterialName(materialName);
    mBackgroundEnt[0]->setMaterialName(materialName);
    mBackgroundEnt[1]->setMaterialName("Background/Brown1");
    mBackgroundEnt[2]->setMaterialName("Background/Red1");
    mBackgroundEnt[3]->setMaterialName("Background/Green1");
    
    return true;
}