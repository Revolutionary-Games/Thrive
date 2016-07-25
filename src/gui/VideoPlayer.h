#pragma once

#include <string>
#include <memory>

namespace thrive{
class VideoPlayerImpl;

class SoundMemoryStream;

/**
@brief A video player that updates an Ogre texture with image data from FFMPEG stream

Based on Ogre FFMPEG videoplayer by Chris Robinson and
FFMPEG tutorial: http://dranger.com/ffmpeg/ffmpegtutorial_all.html
@todo Start using  avcodec_send_packet() once it starts to be available
*/
class VideoPlayer{
    friend SoundMemoryStream;
public:

    VideoPlayer();
    ~VideoPlayer();

    /**
    @brief Starts playing a video from resourceName

    Will unload previously loaded video
    @returns True if a video is now playing. False if an error occured
    @exception runtime_error If the resourceName doesn't exist or the file type is un
    supported
    @exception bad_alloc If some object that should have been allocated just fine
    is null
    @warning this is absolutely not thread safe
    */
    bool
        playVideo(
            const std::string &resourceName
        );
    /**
    @brief Stops and unloads a currently playing video
    */
    void close();

    /**
    @returns True if currently loaded file has an audio stream
    */
    bool hasAudio() const;

    /**
    @returns Current playback position, in seconds
    The return value is directly read from the last decoded frame timestamp
    */
    float getCurrentTime() const;

    /**
    @brief Seeks the video and audio streams to the specified time
    */
    void
        seek(
            float time
        );

    /**
    @returns The total duration of the loaded stream or 0
    */
    float getDuration() const;

    void play();
    void pause();
    bool isPaused() const;
        

    /**
    @brief Call this to do the actual frame update for the video
    @returns True if video is still playing
    */
    bool update();

    /**
    @returns The name of the texture that is used to play videos in this player
    */
    std::string getTextureName() const;

    /**
    @returns The width of the currently playing video
    */
    int32_t getVideoWidth() const;

    /**
    @returns The height of the currently playing video
    */
    int32_t getVideoHeight() const;


    /**
    @returns The number of audio channels
    */
    int getAudioChannelCount() const;

    /**
    @returns The number of samples per second of the audio stream, or -1 if no audio streams
    exist
    */
    int getAudioSampleRate() const;

    /**
    @brief Reads audio data to the buffer
    @returns The number of bytes read
    @param amount The maximum number of bytes to read
    */
    size_t
        readAudioData(uint8_t* output,
            size_t amount);
    
    /**
    @brief Should be called once during start up to load FFMPEG
    */
    static void loadFFMPEG();

protected:

    void
        streamReportingIn(SoundMemoryStream* stream);

private:

    std::unique_ptr<VideoPlayerImpl> p_impl;
};
}
