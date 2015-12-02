#include "sound_listener.h"
#include "sound_manager.h"

#include "caudio_include.h"

#include <assert.h>
#include <iostream>
#include "sound_emitter.h"

using namespace thrive;

struct SoundManager::Implementation{

    Implementation(
        const std::string &device
    ) : m_initialized(false)
    {

        // TODO: allow selecting a specific audio device

        m_audioManager = cAudio::createAudioManager(false);

        assert(m_audioManager && "Failed to create audio manager");

        cAudio::IAudioDeviceList* devices = cAudio::createAudioDeviceList();

        // GCC complains about unused variables if devices are not used
		//auto deviceCount = devices->getDeviceCount();
        //assert(deviceCount >= 1 && "No audio devices found");

        auto defaultDeviceName = devices->getDefaultDeviceName();

        const char* selectedDevice = device.empty() ? defaultDeviceName.c_str() : device.c_str();

        const bool succeeded = m_audioManager->initialize(selectedDevice);

        CAUDIO_DELETE devices;
		devices = 0;

        if(!succeeded)
            assert(false && "Failed to initialize openAL (cAudio)");

        m_listener = std::move(std::unique_ptr<SoundListener>(new SoundListener(m_audioManager->getListener())));

        m_initialized = true;
    }

    ~Implementation(){

        m_initialized = false;
        m_listener.reset();

        cAudio::destroyAudioManager(m_audioManager);
        m_audioManager = NULL;
    }


    bool m_initialized;
    cAudio::IAudioManager* m_audioManager;
    std::unique_ptr<SoundListener> m_listener;
};


SoundListener*
SoundManager::getListener(){

    return getSingleton()->m_impl->m_listener.get();
}

SoundManager*
SoundManager::getSingleton(){

    return staticAccess;
}

SoundManager* SoundManager::staticAccess = NULL;


SoundManager::SoundManager(){

    staticAccess = this;
}

SoundManager::~SoundManager(){

    staticAccess = NULL;
    m_impl.reset();
}

void
SoundManager::init(
    const std::string &device
) {

    m_impl = std::move(std::unique_ptr<Implementation>(new Implementation(device)));
    assert(m_impl->m_initialized && "Failed to initalize SoundManager");
}

void
SoundManager::destroySound(
    SoundEmitter* sound
) {

    sound->destroyInternal(m_impl->m_audioManager);

    delete sound;
}

SoundEmitter*
SoundManager::createSound(
    const std::string &name,
    const std::string &fileName,
    bool stream,
    bool loop,
    std::string namespacePrefix
) {
    const std::string finalPath = "../sounds/"+fileName;

    auto soundObj = m_impl->m_audioManager->create(std::string(namespacePrefix + name.c_str()).c_str(), finalPath.c_str(), stream);

    if(!soundObj){

        // TODO: Logger?
        std::cout << "File " << finalPath << " doesn't exist" << std::endl;
        return NULL;
    }

    soundObj->loop(loop);

    return new SoundEmitter(soundObj);
}

