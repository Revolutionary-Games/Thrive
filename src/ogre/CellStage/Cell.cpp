#include "Cell.h"

Cell::Cell(Ogre::SceneManager* sceneMgr)
        :   mVelocity(0,0,0)
{
    mEntity = sceneMgr->createEntity(Ogre::SceneManager::PrefabType::PT_SPHERE);
    //    mEntity = sceneMgr->createEntity("Head", "ogrehead.mesh");
    mNode = sceneMgr->getRootSceneNode()->
            createChildSceneNode(Ogre::Vector3::ZERO, Ogre::Quaternion::IDENTITY);
    mEntity->setMaterialName("Examples/SphereMappedRustySteel");
    mNode->attachObject(mEntity);
    mNode->setScale(0.01f * Ogre::Vector3::UNIT_SCALE);
    
    Ogre::Root::getSingletonPtr()->addFrameListener(this);
}

Cell::~Cell()
{}

bool Cell::frameRenderingQueued(const Ogre::FrameEvent& evt)
{
    Update(evt.timeSinceLastFrame);
    return true;
}

bool Cell::Update(Ogre::Real deltaTime)
{
    mVelocity.x += Ogre::Math::SymmetricRandom() * deltaTime;
    mVelocity.z += Ogre::Math::SymmetricRandom() * deltaTime;
    
//    Ogre::Vector3 Move(Ogre::Math::SymmetricRandom(),0,Ogre::Math::SymmetricRandom());
    mNode->translate(mVelocity * deltaTime, Ogre::SceneNode::TransformSpace::TS_WORLD);
    
    return true;
}
