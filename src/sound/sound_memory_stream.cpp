#include "sound_memory_stream.h"

#include "gui/VideoPlayer.h"

#include <iostream>

using namespace thrive;

namespace thrive{
class VideoPlayerSource : public cAudio::IDataSource{
public:

    VideoPlayerSource(VideoPlayer* player) : Player(player){}

    bool isValid() override{

        return Player != nullptr;
    }
    
    int getCurrentPos() override{

        return 0;
    }

    int getSize() override{

        return 0;
    }
    
    int read(void* output, int size) override{

        (void)output;
        (void)size;
        return 0;
    }

    bool seek(int amount, bool relative) override{

        (void)amount;
        (void)relative;
        return false;
    }

    VideoPlayer* Player;
};
}

cAudio::IAudioDecoder*
    SoundMemoryStreamFactory::CreateAudioDecoder(
        cAudio::IDataSource* stream)
{
    return CAUDIO_NEW SoundMemoryStream(static_cast<VideoPlayerSource*>(stream),
        static_cast<VideoPlayerSource*>(stream)->Player);
}

MemoryDataSourceFactory::MemoryDataSourceFactory(){

}

MemoryDataSourceFactory::~MemoryDataSourceFactory(){

}

cAudio::IDataSource*
    MemoryDataSourceFactory::CreateDataSource(
        const char* filename,
        bool streamingRequested)
{
    if(!streamingRequested){

        std::cerr << "streaming NOT requested for a sound memory stream"
            << std::endl;
        abort();
        return nullptr;
    }
    
    std::lock_guard<std::mutex> lock(m_Mutex);

    auto iter = m_OpenStream.find(std::string(filename));
    
    if(iter == m_OpenStream.end()){

        std::cerr << "CreateDataSource trying to open stream for non-existant video"
            << std::endl;
        return nullptr;
    }

    VideoPlayer* player = iter->second;

    m_OpenStream.erase(iter);

    return CAUDIO_NEW VideoPlayerSource(player);
}

void
    MemoryDataSourceFactory::reserveStream(
    const std::string &fakeFileName,
    VideoPlayer* streamSource)
{
    std::lock_guard<std::mutex> lock(m_Mutex);
    
    m_OpenStream[fakeFileName] = streamSource;
}

void
    MemoryDataSourceFactory::unReserveStream(
    VideoPlayer* streamSource)
{
    std::lock_guard<std::mutex> lock(m_Mutex);

    for(auto iter = m_OpenStream.begin(); iter != m_OpenStream.end(); ++iter){

        if(iter->second == streamSource){

            m_OpenStream.erase(iter);
            return;
        }
    }
}




SoundMemoryStream::SoundMemoryStream(
    VideoPlayerSource* source,
    VideoPlayer* audioSource) :
    IAudioDecoder(static_cast<cAudio::IDataSource*>(source)),
    m_VideoPlayer(audioSource)
{
    std::lock_guard<std::mutex> lock(Mutex);

    if(m_VideoPlayer)
        m_VideoPlayer->streamReportingIn(this);
}

SoundMemoryStream::~SoundMemoryStream(){

    if(m_VideoPlayer){

        std::cerr << "SoundMemoryStream wasn't closed properly!" << std::endl;
        abort();
    }
}

// cAudio interface //
cAudio::AudioFormats
    SoundMemoryStream::getFormat()
{
    std::lock_guard<std::mutex> lock(Mutex);

    if(m_VideoPlayer && m_VideoPlayer->getAudioChannelCount() > 1)
        return cAudio::EAF_16BIT_STEREO;
    
    return cAudio::EAF_16BIT_MONO;
}

int
    SoundMemoryStream::getFrequency()
{
    std::lock_guard<std::mutex> lock(Mutex);

    if(!m_VideoPlayer)
        return -1;

    return m_VideoPlayer->getAudioSampleRate();
}

bool
    SoundMemoryStream::isSeekingSupported()
{
    return false;
}

bool
    SoundMemoryStream::isValid()
{
    std::lock_guard<std::mutex> lock(Mutex);
    return m_VideoPlayer != nullptr;
}

int
    SoundMemoryStream::readAudioData(void* output,
        int amount)
{
    std::lock_guard<std::mutex> lock(Mutex);

    if(!m_VideoPlayer)
        return 0;

    return m_VideoPlayer->readAudioData(reinterpret_cast<uint8_t*>(output), amount);
}

bool
    SoundMemoryStream::setPosition(int position,
        bool relative)
{
    (void)position;
    (void)relative;
    return false;
}

bool
    SoundMemoryStream::seek(float seconds,
        bool relative)
{
    (void)seconds;
    (void)relative;
    return false;
}

float
    SoundMemoryStream::getTotalTime()
{
    return -1.f;
}

int
    SoundMemoryStream::getTotalSize()
{
    return -1;
}

int
    SoundMemoryStream::getCompressedSize()
{
    return -1;
}

float
    SoundMemoryStream::getCurrentTime()
{
    return -1.f;
}

int
    SoundMemoryStream::getCurrentPosition()
{
    return -1;
}

int
    SoundMemoryStream::getCurrentCompressedPosition()
{
    return -1;
}

cAudio::cAudioString
    SoundMemoryStream::getType() const
{
    return "Thrive VideoPlayer Audio Stream";
}

void
    SoundMemoryStream::onStreamEnded()
{
    std::lock_guard<std::mutex> lock(Mutex);

    m_VideoPlayer->streamReportingIn(nullptr);
    m_VideoPlayer = nullptr;
}


