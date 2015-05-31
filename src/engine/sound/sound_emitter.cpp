#ifndef USE_CAUDIO
#error CMake configured sound incorrectly
#endif

#include <assert.h>
#include <OgreSceneNode.h>

#include "node_attachable.h"
#include "sound_emitter.h"

#include "caudio_include.h"

using namespace thrive;

struct SoundEmitter::Implementation : public NodeAttachable{

    Implementation(cAudio::IAudioSource* audioObject) :
        m_play3D(true), m_autoRepeat(false), m_audioObject(audioObject)
    {

        assert(m_audioObject && "Tried to create SoundEmitter with null pointer");
    }

    ~Implementation(){

        assert(!m_audioObject && "SoundEmitter, sound source was not released properly");
    }

    void
    onMoved(
        Ogre::SceneNode* node
    ) override {

        if(!m_audioObject)
            return;
        
        const auto pos = node->getPosition();
        m_audioObject->move(cAudio::cVector3(pos.x, pos.y, pos.z));
    }

    void
    play(){

        if(m_play3D){

            auto node = getAttachedNode();

            if(node){
                
                const auto pos = node->getPosition();
            
                m_audioObject->play3d(cAudio::cVector3(pos.x, pos.y, pos.z));
                
            } else {

                m_audioObject->play3d(cAudio::cVector3(0, 0, 0));
            }

            if(m_autoRepeat)
                m_audioObject->loop(m_autoRepeat);
        
        } else {

            m_audioObject->play2d(m_autoRepeat);
        }
    }

    bool m_play3D;
    bool m_autoRepeat;
    cAudio::IAudioSource* m_audioObject;
};

SoundEmitter::SoundEmitter(
    cAudio::IAudioSource* audioObject
) : m_impl(new Implementation(audioObject))
{

}

SoundEmitter::~SoundEmitter(){

    m_impl.reset();
}


void
SoundEmitter::loop(
    bool actuallyLoop
) {

    m_impl->m_autoRepeat = actuallyLoop;
    m_impl->m_audioObject->loop(actuallyLoop);
}

void
SoundEmitter::play(
    bool forceRestart /*= false*/
) {

    if(m_impl->m_audioObject->isPlaying()){

        if(forceRestart){

            m_impl->m_audioObject->stop();
            
        } else {
        
            return;
        }
    }
    
    m_impl->play();
}

void
SoundEmitter::pause(){

    if(m_impl->m_audioObject->isPlaying())
        m_impl->m_audioObject->pause();
}

void
SoundEmitter::stop(){

    if(m_impl->m_audioObject->isPlaying())
        m_impl->m_audioObject->stop();
}

void
SoundEmitter::disable3D(
    bool disable
) {

    m_impl->m_play3D = !disable;

    if(m_impl->m_audioObject->isPlaying()){

        m_impl->play();
    }
}

void
SoundEmitter::setVolume(
    float volume
) {

    m_impl->m_audioObject->setVolume(volume);
}

void
SoundEmitter::setRolloffFactor(
    float rolloff
) {

    m_impl->m_audioObject->setRolloffFactor(rolloff);
}

void
SoundEmitter::setMaxDistance(
    float distance
) {

    m_impl->m_audioObject->setMaxAttenuationDistance(distance);
}

void
SoundEmitter::startFade(
    bool fadeIn,
    float time
) {

    // TODO: change the audio volume over time
    (void)fadeIn;
    (void)time;
}

float
SoundEmitter::getAudioLength(){

    return m_impl->m_audioObject->getTotalAudioTime();
}


void
SoundEmitter::detachFromNode(){

    m_impl->detachFromNode();
}

void
SoundEmitter::attachToNode(
    Ogre::SceneNode* node
) {

    m_impl->attachToNode(node);
}

void
SoundEmitter::destroyInternal(
    cAudio::IAudioManager* manager
) {
    m_impl->detachFromNode();
    
    manager->release(m_impl->m_audioObject);
    m_impl->m_audioObject = NULL;
}
