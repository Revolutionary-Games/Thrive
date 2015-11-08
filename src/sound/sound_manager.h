#pragma once
#include <memory>
#include <string>

namespace Ogre {

    class SceneManager;
}

namespace thrive{

class SoundListener;
class SoundEmitter;
    
class SoundManager final{
public:

    SoundManager();
    ~SoundManager();

    /**
       @brief Sets up cAudio with the specified device

       @param device The device name or empty string for default 
    */
    void
        init(
        const std::string &device
        );

    //! @brief Destroys a sound object that was created before with createSound
    void
        destroySound(
            SoundEmitter* sound
        );

    /**
       @brief Creates a sound object for internal use by sound system

       @param ambient When true creates a 2D sound

       @param fileName Path to the file when ../sounds/ is added to the front
    */
    SoundEmitter*
        createSound(
            const std::string &name,
            const std::string &fileName,
            bool stream,
            bool loop,
            std::string namespacePrefix
        );

    /**
       @brief Returns the listener object which can be moved to change the position of
       the sound's listener
    */
    static SoundListener* getListener();

    /**
       @brief Returns a pointer to the global instance
    */
    static SoundManager* getSingleton();

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;

    //! @note Sound systems accessed OgreOggSound with getSingleton, this allows similar access
    //! to the new system
    static SoundManager* staticAccess;
};
}
