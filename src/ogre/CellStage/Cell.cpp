#include "Cell.h"

Cell::Cell(Ogre::SceneManager* sceneMgr, Ogre::Vector3 startPosition)
        :   mVelocity(0,0,0)
{
    mEntity = sceneMgr->createEntity(Ogre::SceneManager::PrefabType::PT_SPHERE);
    //    mEntity = sceneMgr->createEntity("Head", "ogrehead.mesh");
    mNode = sceneMgr->getRootSceneNode()->
            createChildSceneNode(startPosition, Ogre::Quaternion::IDENTITY);
    mEntity->setMaterialName("Examples/SphereMappedRustySteel");
    mNode->attachObject(mEntity);
    mNode->setScale(0.1f * Ogre::Vector3::UNIT_SCALE);
    
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
    mVelocity.x += Ogre::Math::SymmetricRandom() * 5 * deltaTime;
    mVelocity.y += Ogre::Math::SymmetricRandom() * 5 * deltaTime;
    
    mNode->translate(mVelocity * 5 * deltaTime, Ogre::SceneNode::TransformSpace::TS_WORLD);
    
    return true;
}
