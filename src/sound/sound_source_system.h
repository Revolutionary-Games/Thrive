#pragma once

#include "engine/component.h"
#include "engine/system.h"
#include "engine/touchable.h"

#include <list>
#include <memory>

namespace luabind {
class scope;
}

namespace OgreOggSound {
class OgreOggISound;
}

namespace thrive {

/**
* @brief Represents a single sound
*/
class Sound {

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - Sound(name, filename)
    * - @link m_properties Properties @endlink
    * - Properties
    *   - Properties::playState
    *   - Properties::loop
    *   - Properties::volume
    *   - Properties::maxDistance
    *   - Properties::rolloffFactor
    *   - Properties::referenceDistance
    *   - Properties::priority
    * - Enum PlayState
    *   - PlayState::Play
    *   - PlayState::Pause
    *   - PlayState::Stop
    * - Sound::name
    * - Sound::pause
    * - Sound::play
    * - Sound::stop
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Play mode of the sound
    */
    enum PlayState {
        Play,
        Pause,
        Stop
    };

    /**
    * @brief Sound properties
    */
    struct Properties : public Touchable {
        PlayState playState = PlayState::Stop;
        bool loop = false;
        float volume = 1.0f;
        float maxDistance = -1.0f;
        float rolloffFactor = -1.0f;
        float referenceDistance = 100.0f;
        uint8_t priority = 0;
    };

    /**
    * @brief Default constructor for loading
    */
    Sound();

    /**
    * @brief Constructor
    *
    * @param name
    *   The name of the sound (must be unique)
    * @param filename
    *   The name of the sound file
    */
    Sound(
        std::string name,
        std::string filename
    );

    /**
    * @brief The file that the sound is playing
    *
    * @return
    */
    std::string
    filename() const;

    /**
    * @brief Loads a sound from storage
    *
    * @param storage
    */
    void
    load(
        const StorageContainer& storage
    );

    /**
    * @brief The name of the sound
    *
    * @return
    */
    std::string
    name() const;

    /**
    * @brief Pauses the sound during the next frame
    */
    void
    pause();

    /**
    * @brief Starts (or resumes) playing the sound
    */
    void
    play();

    /**
    * @brief Stops the sound during the next frame
    */
    void
    stop();

    /**
    * @brief Constructs a storage container for serialization
    *
    * @return
    */
    StorageContainer
    storage() const;

    /**
    * @brief Properties
    */
    Properties m_properties;

    /**
    * @brief Pointer to internal sound
    */
    OgreOggSound::OgreOggISound* m_sound = nullptr;

private:

    std::string m_filename;

    std::string m_name;


};

/**
* @brief A component for sound sources
*
*/
class SoundSourceComponent : public Component {
    COMPONENT(SoundSource)

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - SoundSourceComponent()
    * - SoundSourceComponent::addSound()
    * - SoundSourceComponent::removeSound()
    * - SoundSourceComponent::interpose()
    * - SoundSourceComponent::queueSound()
    * - SoundSourceComponent::volumeMultiplier
    * - SoundSourceComponent::ambientSoundSource
    *
    * @return
    **/
    static luabind::scope
    luaBindings();

    /**
    * @brief Adds a new sound
    *
    * @param name
    *   The name of the sound (must be unique)
    * @param filename
    *   The file to play
    *
    * @return A reference to the new sound
    */
    Sound*
    addSound(
        std::string name,
        std::string filename
    );


    /**
    * @brief Removes a sound by name
    *
    * @param name
    */
    void
    removeSound(
        std::string name
    );


    /**
    * @brief Interrupts the current sound and interposes a new one
    *
    * @param name
    *  The name of the song that is to interpose
    *
    * @param fadeTime
    *  The time the transition should take
    */
    void
    interpose(
        std::string name,
        int fadeTime
    );

    /**
    * @brief Queues up a sound to be played after the current one
    *
    * @param name
    *  The name of the song to queue up
    */
    void
    queueSound(
        std::string name
    );


    void
    load(
        const StorageContainer& storage
    ) override;


    StorageContainer
    storage() const override;

    /**
    * @brief Whether this source as an ambient soundsource
    *
    *  Ambient sound sources convert all sounds to ambient ones and automatically fade between them
    */
    TouchableValue<bool> m_ambientSoundSource = false;

    /**
    * @brief Volume multiplier that is applied to all associated sounds
    */
    TouchableValue<float> m_volumeMultiplier = 1.0f;


private:

    friend class SoundSourceSystem;

    std::list<Sound*> m_addedSounds;

    std::list<Sound*> m_removedSounds;

    Sound* m_ambientActiveSound = nullptr;
    Sound* m_queuedSound = nullptr;
    int m_ambientSoundCountdown = 0;
    bool m_isTransitioningAmbient = false;

    std::unordered_map<std::string, std::unique_ptr<Sound>> m_sounds;

};


/**
* @brief Creates, updates and removes sounds
*/
class SoundSourceSystem : public System {

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - SoundSourceSystem()
    *
    * @return
    **/
    static luabind::scope
    luaBindings();

    /**
    * @brief Constructor
    */
    SoundSourceSystem();

    /**
    * @brief Destructor
    */
    ~SoundSourceSystem();

    void
    activate() override;

    void
    deactivate() override;

    /**
    * @brief Initializes the system
    *
    */
    void init(GameState* gameState) override;

    /**
    * @brief Shuts the system down
    */
    void shutdown() override;

    /**
    * @brief Updates the system
    */
    void update(int) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}


