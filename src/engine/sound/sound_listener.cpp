#include "node_attachable.h"
#include "sound_listener.h"

#include <OgreSceneManager.h>
#include <OgreSceneNode.h>

#include "caudio_include.h"

using namespace thrive;

struct SoundListener::Implementation : public NodeAttachable{

    Implementation(cAudio::IListener* listener) : m_listener(listener)
    {

        // Ogre up vector //
        m_listener->setUpVector(cAudio::cVector3(0, 1, 0));
    }

    ~Implementation(){

        m_listener = NULL;
    }

    void
    onMoved(
        Ogre::SceneNode* node
    ) override {

        const auto pos = node->getPosition();
        m_listener->move(cAudio::cVector3(pos.x, pos.y, pos.z));

        const auto quaternion = node->getOrientation();

        Ogre::Radian angle;
        Ogre::Vector3 vector;

        quaternion.ToAngleAxis(angle, vector);
        
        m_listener->setDirection(cAudio::cVector3(vector.x, vector.y, vector.z));
        
    }

    cAudio::IListener* m_listener;
};



SoundListener::SoundListener(
    cAudio::IListener* controlledListener
) :
    m_impl(new Implementation(controlledListener))
{


}

SoundListener::~SoundListener(){

    m_impl.reset();
}

void
SoundListener::detachFromNode(){

    m_impl->detachFromNode();
}

void
SoundListener::attachToNode(
    Ogre::SceneNode* node
) {

    m_impl->attachToNode(node);
}


