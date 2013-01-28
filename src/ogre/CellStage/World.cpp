#include "World.h"
#include <math.h>

World::World(Ogre::SceneManager* sceneMgr)
        :   mSceneMgr(sceneMgr),
        mBgIndex(0)
{
    mBackgroundEnt = new Ogre::Entity*[4];
    mBackgroundNode = new Ogre::SceneNode*[4];
    for(int i = 0; i < 4; i++)
    {
        mBackgroundEnt[i] = mSceneMgr->createEntity(Ogre::SceneManager::PrefabType::PT_PLANE);
        mBackgroundNode[i] = mSceneMgr->getRootSceneNode()->createChildSceneNode();
        mBackgroundNode[i]->attachObject(mBackgroundEnt[i]);
        Ogre::Vector3 scale = Ogre::Vector3::UNIT_SCALE;
        // 1:1 scaling (y=3 for 4:3 scaling).  A plane primitive appears to be 200x200 units before scaling.
        scale.x *= 4.0f;
        scale.y *= 4.0f;
        scale *= 0.025f;
        mBackgroundNode[i]->setScale(scale);
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
    // 1:1 scaling
    Ogre::Real SpacingX = 20.0f;
    Ogre::Real SpacingY = 20.0f;
    
    Ogre::Vector3 scaledCamPos = camNodePosition;
    scaledCamPos.x /= SpacingX;
    scaledCamPos.y /= SpacingY;
    
    Ogre::Real x = fmodf(scaledCamPos.x, 1.0f);
    Ogre::Real y = fmodf(scaledCamPos.y, 1.0f);
    
    // Correct negative modulos
    x = x < 0 ? 1 + x : x;
    y = y < 0 ? 1 + y : y;
    
    Ogre::Vector3 basePos(scaledCamPos.x - x, scaledCamPos.y - y, 0);
    basePos.x += 0.5f;
    basePos.y += 0.5f;
    basePos.x *= SpacingX;
    basePos.y *= SpacingY;
    
    mBackgroundNode[0]->setPosition(basePos);
    
    if(x > .5f)
    {
        mBackgroundNode[1]->setPosition(basePos + SpacingX * Ogre::Vector3::UNIT_X);
        if (y > .5f)
        {
            mBackgroundNode[2]->setPosition(basePos + SpacingY * Ogre::Vector3::UNIT_Y);
            mBackgroundNode[3]->setPosition(basePos + SpacingX * Ogre::Vector3::UNIT_X
                                                    + SpacingY * Ogre::Vector3::UNIT_Y);
        }
        else
        {
            mBackgroundNode[2]->setPosition(basePos - SpacingY * Ogre::Vector3::UNIT_Y);
            mBackgroundNode[3]->setPosition(basePos + SpacingX * Ogre::Vector3::UNIT_X
                                                    - SpacingY * Ogre::Vector3::UNIT_Y);
        }
    }
    else
    {
        mBackgroundNode[1]->setPosition(basePos - SpacingX * Ogre::Vector3::UNIT_X);
        if (y > .5f)
        {
            mBackgroundNode[2]->setPosition(basePos + SpacingY * Ogre::Vector3::UNIT_Y);
            mBackgroundNode[3]->setPosition(basePos - SpacingX * Ogre::Vector3::UNIT_X
                                                    + SpacingY * Ogre::Vector3::UNIT_Y);
        }
        else
        {
            mBackgroundNode[2]->setPosition(basePos - SpacingY * Ogre::Vector3::UNIT_Y);
            mBackgroundNode[3]->setPosition(basePos - SpacingX * Ogre::Vector3::UNIT_X
                                                    - SpacingY * Ogre::Vector3::UNIT_Y);
        }
    }
    
    return true;
}

bool World::setBackground(Ogre::String materialName)
{
    Ogre::String Mat = materialName;
    mBgIndex++;
    switch (mBgIndex)
    {
            case 1:
                Mat = "Background/Blue1";
                break;
            case 2:
                Mat = "Background/Blue2";
                break;
            case 3:
                Mat = "Background/Brown1";
                break;
            case 4:
                Mat = "Background/Brown2";
                break;
            case 5:
                Mat = "Background/Green1";
                break;
            case 6:
                Mat = "Background/Red1";
                mBgIndex = 0;
                break;
        default:
            break;
    }
    for (int i = 0; i < 4; i++)
        mBackgroundEnt[i]->setMaterialName(Mat);
//    mBackgroundEnt[0]->setMaterialName(materialName);
//    mBackgroundEnt[1]->setMaterialName("Background/Brown2");
//    mBackgroundEnt[2]->setMaterialName("Background/Red1");
//    mBackgroundEnt[3]->setMaterialName("Background/Green1");
    
    return true;
}