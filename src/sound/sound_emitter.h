#pragma once

#include <stdint.h>
#include <memory>

namespace Ogre {

    class SceneNode;
}

namespace cAudio {

    class IAudioSource;
    class IAudioManager;
}

namespace thrive {

    class SoundEmitter final{
public:

    SoundEmitter(cAudio::IAudioSource* audioObject);
    ~SoundEmitter();

    void loop(bool actuallyLoop);

    //! @brief Starts playing this sound if not already
    void
        play(
            bool forceRestart = false
        );

    //! @brief Pauses the audio if playing
    void pause();

    //! @brief Stops the audio (losing the current spot)
    void stop();

    //! @brief Disables 3D audio
    void disable3D(bool disable);


    //! @brief Sets volume, multiplier default is 1.f
    void setVolume(float volume);

    //! @brief Sets how fast the sound fades with distance
    void setRolloffFactor(float rolloff);

    //! @brief Sets the distance at which attenuation will stop
    void setMaxDistance(float distance);
    
    //! @note Not implemented, don't use
    void startFade(bool fadeIn, float time);

    /**
       @brief Returns the length of the audio attached

       @note If the length cannot be determined for some reason
       returns a negative value
    */
    float getAudioLength();

    //! @copydoc NodeAttachable::detachFromNode
    void detachFromNode();

    //! @copydoc NodeAttachable::attachToNode
    void
        attachToNode(
            Ogre::SceneNode* node
        );

    void
        destroyInternal(
            cAudio::IAudioManager* manager
        );
    
private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}

