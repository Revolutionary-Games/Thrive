#pragma once

#include <cAudio/IAudioDecoder.h>
#include <cAudio/IAudioDecoderFactory.h>
#include <cAudio/IDataSourceFactory.h>

#include <boost/thread/mutex.hpp>
#include <boost/thread/lock_guard.hpp>
#include <map>


namespace thrive{

class VideoPlayer;
class VideoPlayerSource;
class VideoPlayerImpl;

/**
Factory class for VideoStream data source
*/
class SoundMemoryStreamFactory : public cAudio::IAudioDecoderFactory{
public:

    /**
    Called by cAudio when VideoPlayer opens the sound stream
    */
    cAudio::IAudioDecoder*
        CreateAudioDecoder(
            cAudio::IDataSource* stream) override;

};

/**
Factory class for SoundMemoryStream

*/
class MemoryDataSourceFactory : public cAudio::IDataSourceFactory{
public:

    MemoryDataSourceFactory();
    ~MemoryDataSourceFactory();


    cAudio::IDataSource*
        CreateDataSource(
            const char* filename,
            bool streamingRequested) override;

    /**
    @brief Reserves a stream name for VideoPlayer instance
    */
    void reserveStream(
        const std::string &fakeFileName,
        VideoPlayer* streamSource);

    /**
    @brief Removes a reserved stream, blocking CreateAudioDecoder from using the
    VideoPlayer

    Called by VideoPlayer if it is closing before it has detected that
    a SoundMemoryStream has been created for it
    */
    void unReserveStream(
        VideoPlayer* streamSource);

private:

    /**
    Locked when managing the VideoPlayer queue
    */
    boost::mutex m_Mutex;
    /**
    Contains streams that can be returned by CreateAudioDecoder to cAudio
    @note m_Mutex must be locked when changing this
    */
    std::map<std::string, VideoPlayer*> m_OpenStream;
};

class SoundMemoryStream : public cAudio::IAudioDecoder{
    friend VideoPlayerImpl;
public:

    SoundMemoryStream(VideoPlayerSource* source, VideoPlayer* audioSource);
    ~SoundMemoryStream();


    // cAudio interface //

    cAudio::AudioFormats
        getFormat() override;

    int
        getFrequency() override;

    bool
        isSeekingSupported() override;

    bool
        isValid() override;

    int
        readAudioData(void* output,
            int amount) override;

    bool
        setPosition(int position,
            bool relative) override;

    bool
        seek(float seconds,
            bool relative) override;

    float
        getTotalTime() override;

    int
        getTotalSize() override;

    int
        getCompressedSize() override;

    float
        getCurrentTime() override;

    int
        getCurrentPosition() override;

    int
        getCurrentCompressedPosition() override;

    cAudio::cAudioString
        getType() const override;

protected:

    /**
    Called when the stream should close or if the VideoPlayer has been closed
    */
    void onStreamEnded();

private:

    /**
    This is where the audio data is retrieved when streaming
    */
    VideoPlayer* m_VideoPlayer = nullptr;

    boost::mutex Mutex;
};
}
