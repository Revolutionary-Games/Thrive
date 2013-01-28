#include "Cell.h"

Cell::Cell(Ogre::SceneManager* sceneMgr)
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
    Ogre::Vector3 Move(Ogre::Math::SymmetricRandom(),0,Ogre::Math::SymmetricRandom());
    mNode->translate(Move * evt.timeSinceLastFrame, Ogre::SceneNode::TransformSpace::TS_WORLD);
//    mNode->setPosition(mNode->getPosition() + Move);
    return true;
}
