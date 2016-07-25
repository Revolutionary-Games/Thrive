#include "sound_listener.h"
#include "sound_manager.h"
#include "sound_memory_stream.h"

#include "caudio_include.h"

#include <assert.h>
#include <iostream>
#include "sound_emitter.h"

#include <chrono>
#include <thread>
#include <boost/thread/thread.hpp>

using namespace thrive;

struct SoundManager::Implementation{

    Implementation(
      //  const std::string &device
    ) : m_initialized(false)
    {


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

    std::unique_ptr<SoundMemoryStreamFactory> m_SoundMemoryStreamFactory;
    std::unique_ptr<MemoryDataSourceFactory> m_MemoryDataSourceFactory;
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
    m_impl = std::move(std::unique_ptr<Implementation>(new Implementation()));
    // TODO: allow selecting a specific audio device

    m_impl->m_audioManager = cAudio::createAudioManager(false);

    assert(m_impl->m_audioManager && "Failed to create audio manager");

    cAudio::IAudioDeviceList* devices = cAudio::createAudioDeviceList();

    // GCC complains about unused variables if devices are not used
    //auto deviceCount = devices->getDeviceCount();
    //assert(deviceCount >= 1 && "No audio devices found");

    int attempts = 0;
    bool succeeded = false;


   std::cout << "\nAvailable Playback Devices: \n";
    cAudio::IAudioDeviceList* pDeviceList = cAudio::createAudioDeviceList();
    unsigned int deviceCount = pDeviceList->getDeviceCount();
    cAudio::cAudioString defaultDevice1Name = pDeviceList->getDefaultDeviceName();
    for(unsigned int i=0; i<deviceCount; ++i)
    {
        cAudio::cAudioString deviceName = pDeviceList->getDeviceName(i);
        if(deviceName.compare(defaultDevice1Name) == 0)
            std::cout << i << "): " << deviceName.c_str() << " [DEFAULT] \n";
        else
            std::cout << i << "): " << deviceName.c_str() << " \n";
    }
    std::cout << std::endl;
    defaultDevice1Name = pDeviceList->getDefaultDeviceName();
    //auto defaultDeviceName = devices->getDeviceName(0);
    std::cout << "Attempting default device: " << defaultDevice1Name << std::endl;
    while (!succeeded && attempts < 10)
    {
        if (attempts > 0)
        {
            std::cout << "Failed to get init openAL, retrying attempt " << attempts << std::endl;
          //  std::this_thread::sleep_for(std::chrono::milliseconds(500*attempts));
            boost::this_thread::sleep( boost::posix_time::milliseconds(300) );
        }
        attempts++;
       // defaultDeviceName = devices->getDefaultDeviceName();
        const char* selectedDevice;
        if (attempts == 1)
        {

        selectedDevice = device.empty() ? defaultDevice1Name.c_str() : device.c_str();
        }else
        {
            selectedDevice = pDeviceList->getDeviceName(attempts-1).c_str();
        }

        succeeded = m_impl->m_audioManager->initialize(selectedDevice);

        CAUDIO_DELETE devices;
        devices = 0;
    }
    if(!succeeded)
        assert(false && "Failed to initialize openAL (cAudio) after 10 attempts");

    m_impl->m_listener = std::make_unique<SoundListener>(
        m_impl->m_audioManager->getListener());

    // Setup audio for video playback
    m_impl->m_SoundMemoryStreamFactory = std::make_unique<SoundMemoryStreamFactory>();
    m_impl->m_MemoryDataSourceFactory = std::make_unique<MemoryDataSourceFactory>();

    if(!m_impl->m_audioManager->registerAudioDecoder(m_impl->m_SoundMemoryStreamFactory.get(),
            "video_sound"))
    {
        std::cerr << "Failed to register audio decoder for video" << std::endl;
        abort();
    }
    
    if(!m_impl->m_audioManager->registerDataSource(m_impl->m_MemoryDataSourceFactory.get(),
            "Thrive FFMPEG Video Sound", 1))
    {
        std::cerr << "Failed to register audio source for video" << std::endl;
        abort();        
    }

    

    m_impl->m_initialized = true;


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


cAudio::IAudioSource*
    SoundManager::createVideoSound(
        VideoPlayer* player,
        const std::string &nameIdentifier,
        const std::string &fileName
    )
{
    m_impl->m_MemoryDataSourceFactory->reserveStream(fileName, player);

    return m_impl->m_audioManager->create(nameIdentifier.c_str(), fileName.c_str(),
        true);
}

void
    SoundManager::cancelVideoSoundCreation(
        VideoPlayer* player)
{
    m_impl->m_MemoryDataSourceFactory->unReserveStream(player);
}

void
    SoundManager::destroyAudioSource(
        cAudio::IAudioSource* source)
{
    m_impl->m_audioManager->release(source);
}

