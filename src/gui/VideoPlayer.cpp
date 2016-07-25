#include "gui/VideoPlayer.h"

#include "sound/sound_manager.h"
#include "sound/sound_memory_stream.h"

extern "C"{
// FFMPEG includes
#include <libavcodec/avcodec.h>
#include <libavformat/avformat.h>
#include <libavutil/imgutils.h>
#include <libswscale/swscale.h>
#include <libswresample/swresample.h>
}

    
// Ogre
#include <OgreResourceGroupManager.h>
#include <OgreTextureManager.h>
#include <OgrePixelBox.h>
#include <OgreHardwarePixelBuffer.h>

// cAudio
#include <cAudio/IAudioSource.h>

#include <limits>
#include <fstream>
#include <mutex>
#include <chrono>

namespace thrive{

constexpr auto DEFAULT_READ_BUFFER = 32000;
constexpr auto OGRE_IMAGE_FORMAT = Ogre::PF_BYTE_RGBA;
/*Ogre::PF_BYTE_RGBA Ogre::PF_R8G8B8*/
// This must match OGRE_IMAGE_FORMAT otherwise videos are broken
constexpr auto FFMPEG_DECODE_TARGET = AV_PIX_FMT_RGBA;
/*AV_PIX_FMT_RGBA*/

/**
For unique texture names
*/
static int PlayerTextureNumber = 1;

/**
Ogre resource seeking code by
Copyright (c) 2014 Jannik Heller <scrawl@baseoftrash.de>, Chris Robinson

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
int
    OgreResource_Read(
        void *user_data,
        uint8_t *buf,
        int buf_size);

int
    OgreResource_Write(
        void *user_data,
        uint8_t *buf,
        int buf_size);

int64_t
    OgreResource_Seek(
        void *user_data,
        int64_t offset,
        int whence);


class VideoPlayerImpl{

    /**
    Holds converted audio data before being read by readAudioData
    */
    struct ReadAudioPacket{

        std::vector<uint8_t> DecodedData;
    };

    /**
    Holds raw video packets before decoding
    */
    struct ReadVideoPacket{

        ReadVideoPacket(AVPacket* src){

            av_packet_move_ref(&packet, src);
        }
        
        ~ReadVideoPacket(){

            av_packet_unref(&packet);
        }
        
        AVPacket packet;
    };
    
    
public:

    using ClockType = std::chrono::steady_clock;

    VideoPlayerImpl(VideoPlayer* outside) : OutsidePtr(outside)
    {
        auto const number = ++PlayerTextureNumber;

        std::stringstream stream;
        stream << "VideoPlayerImpl_inner_texture_" << number;
        
        TextureName = stream.str();
    }

    ~VideoPlayerImpl()
    {

        // Ensure all FFMPEG resources are closed
        close();
    }
    

    bool
        open(
            const std::string &resource
        )
    {
        Stream = Ogre::ResourceGroupManager::getSingleton().openResource(resource);
        
        if(Stream.isNull())
            throw std::runtime_error("Failed to open video resource");

        unsigned char* readBuffer = reinterpret_cast<unsigned char*>(
            av_malloc(DEFAULT_READ_BUFFER));

        if(!readBuffer)
            throw std::bad_alloc();
        
        ResourceReader = avio_alloc_context(readBuffer, DEFAULT_READ_BUFFER, 0, this,
            OgreResource_Read,
            OgreResource_Write,
            OgreResource_Seek);
        
        if(!ResourceReader)
            return false;

        Context = avformat_alloc_context();
        if(!Context)
            return false;

        Context->pb = ResourceReader;

        if(avformat_open_input(&Context, resource.c_str(), nullptr, nullptr) < 0){

            // Context was freed automatically
            Context = nullptr;
            return false;
        }

        if(avformat_find_stream_info(Context, nullptr) < 0){

            return false;
        }

        // Find audio and video streams //
        unsigned int videoStream = std::numeric_limits<unsigned int>::max();
        unsigned int audioStream = std::numeric_limits<unsigned int>::max();
        
        for(unsigned int i = 0; i < Context->nb_streams; ++i){

            if(Context->streams[i]->codec->codec_type == AVMEDIA_TYPE_VIDEO){

                videoStream = i;
                continue;
            }

            if(Context->streams[i]->codec->codec_type == AVMEDIA_TYPE_AUDIO){

                audioStream = i;
                continue;
            }
        }

        // Fail if didn't find a stream //
        if(videoStream >= Context->nb_streams)
            return false;

        
        if(videoStream < Context->nb_streams){

            // Found a video stream, play it
            openStream(videoStream, true);
        }

        if(audioStream < Context->nb_streams){

            // Found an audio stream, play it
            openStream(audioStream, false);
        }

        DecodedFrame = av_frame_alloc();
        ConvertedFrame = av_frame_alloc();
        DecodedAudio = av_frame_alloc();

        if(!DecodedFrame || !ConvertedFrame || !DecodedAudio)
            throw std::bad_alloc();

        FrameWidth = Context->streams[videoStream]->codec->width;
        FrameHeight = Context->streams[videoStream]->codec->height;
        
        // Calculate required size for the converted frame
        ConvertedBufferSize = av_image_get_buffer_size(FFMPEG_DECODE_TARGET,
            FrameWidth, FrameHeight, 1);

        ConvertedFrameBuffer = reinterpret_cast<uint8_t*>(av_malloc(
                ConvertedBufferSize * sizeof(uint8_t)));
        
        if(!ConvertedFrameBuffer)
            throw std::bad_alloc();
        
        if(av_image_fill_arrays(ConvertedFrame->data, ConvertedFrame->linesize,
            ConvertedFrameBuffer, FFMPEG_DECODE_TARGET,
                FrameWidth, FrameHeight, 1) < 0)
        {
            throw std::bad_alloc();
        }

        // Converting images to be Ogre compatible is done by this
        // TODO: allow controlling how good conversion is done
        // SWS_FAST_BILINEAR is the fastest
        ImageConverter = sws_getContext(FrameWidth, FrameHeight,
            Context->streams[VideoIndex]->codec->pix_fmt,
            FrameWidth, FrameHeight, FFMPEG_DECODE_TARGET, SWS_BICUBIC,
            nullptr, nullptr, nullptr);
        
        if(!ImageConverter){

            throw std::bad_alloc();
        }


        Texture = Ogre::TextureManager::getSingleton().createManual(
            TextureName,
            Ogre::ResourceGroupManager::DEFAULT_RESOURCE_GROUP_NAME,
            Ogre::TEX_TYPE_2D,
            FrameWidth, FrameHeight,
            0,
            OGRE_IMAGE_FORMAT,
            Ogre::TU_DYNAMIC_WRITE_ONLY_DISCARDABLE);

        if(Texture.isNull())
        {
            throw std::bad_alloc();
        }

        auto buffer = Texture->getBuffer();

        const auto ogreBufferSize = (buffer->getWidth() * buffer->getHeight()) *
            Ogre::PixelUtil::getNumElemBytes(OGRE_IMAGE_FORMAT);

        if(ogreBufferSize != ConvertedBufferSize){

            std::cerr << "OGRE_IMAGE_FORMAT is incompatible with FFMPEG_DECODE_TARGET"
                << std::endl;
            std::cerr << ogreBufferSize << " size is not" << ConvertedBufferSize <<
                std::endl;
            abort();
            return false;
        }

        if(AudioCodec){

            // Setup audio playing //
            SampleRate = AudioCodec->sample_rate;
            ChannelCount = AudioCodec->channels;

            if(ChannelCount <= 0 || ChannelCount > 2){

                throw std::runtime_error("Unsupported audio channel count, "
                    "only 1 or 2 are supported");
            }

            // cAudio expects AV_SAMPLE_FMT_S16
            //AudioCodec->sample_fmt;
            if(av_get_bytes_per_sample(AV_SAMPLE_FMT_S16) != 2){

                throw std::runtime_error("AV_SAMPLE_FMT_S16 size has changed");
            }

            AudioConverter = swr_alloc();

            const auto channelLayout = AudioCodec->channel_layout != 0 ?
                AudioCodec->channel_layout :
                // Guess
                av_get_default_channel_layout(AudioCodec->channels);
                

            AudioConverter = swr_alloc_set_opts(AudioConverter, channelLayout,
                AV_SAMPLE_FMT_S16, AudioCodec->sample_rate,
                channelLayout, AudioCodec->sample_fmt, AudioCodec->sample_rate,
                0, nullptr);

            if(!AudioConverter || swr_init(AudioConverter) < 0){

                throw std::runtime_error("Failed to initialize audio converter for stream");
            }

            // Create sound //
            PlayingSource = SoundManager::getSingleton()->createVideoSound(OutsidePtr,
                 TextureName + "_sound_source",
                 TextureName + ".video_sound");
        }
            
        
        //dumpInfo();
        resetClock();
        
        PassedTimeSeconds = 0.f;
        NextFrameReady = false;
        CurrentlyDecodedTimeStamp = 0.f;

        return true;
    }

    void
        openStream(
            unsigned int index,
            bool video
        )
    {
        auto* codec = avcodec_find_decoder(Context->streams[index]->codec->codec_id);

        if(!codec){

            throw std::runtime_error("unsupported codec used in video file");
        }

        auto* codecContext = Context->streams[index]->codec;

        // Open the codec this is important to avoid segfaulting //
        // FFMPEG documentation warns that this is not thread safe
        if(avcodec_open2(codecContext, codec, nullptr) < 0){

            avcodec_free_context(&codecContext);
            throw std::runtime_error("codec failed to open");
        }
        
        
        if(video){

            VideoCodec = codecContext;
            VideoIndex = static_cast<int>(index);
            VideoTimeBase = static_cast<float>(VideoCodec->time_base.num) /
                static_cast<float>(VideoCodec->time_base.den);
            
        } else {

            AudioCodec = codecContext;
            AudioIndex = static_cast<int>(index);
        }
    }

    bool
        isOpen() const
    {
        return VideoCodec != nullptr && ConvertedFrameBuffer != nullptr;
    }

    /**
    @brief Reads a single packet from either stream and pushes it into a queue
    */
    bool
        readOnePacket()
    {
        // Apparently Lua wants to call this after closing
        if(!Context)
            return false;

        std::lock_guard<std::mutex> lock(ReadPacketMutex);

        AVPacket packet;
        //av_init_packet(&packet);

        // Decode data until a frame has been read
        if(av_read_frame(Context, &packet) < 0){

            // Stream ended //
            //av_packet_unref(&packet);
            return false;
        }
            
        // Is this a packet from the video stream?
        if(packet.stream_index == VideoIndex) {

            // Store for decoding //
            std::lock_guard<std::mutex> lock(ReadVideoDataMutex);

            ReadVideoData.push_back(std::make_unique<ReadVideoPacket>(&packet));
                
        } else if(packet.stream_index == AudioIndex){

            // Audio packet //
            if(!AudioCodec){

                // Audio has been invalidated
                av_packet_unref(&packet);
                return false;
            }

            // TODO: move this to readAudioData
            AVPacket orig_pkt = packet;
            do {

                int got_frame = 0;
                auto len = avcodec_decode_audio4(AudioCodec, DecodedAudio,
                    &got_frame, &packet);

                if(len < 0){

                    std::cerr << "Invalid audio stream, stopping audio playback"
                        << std::endl;
                    AudioCodec = nullptr;
                    break;
                }

                if(got_frame){

                    // Add the data to the queue //
                    auto newBuffer = std::make_unique<ReadAudioPacket>();

                    // This is verified in open when setting up converting
                    const auto bytesPerSample = 2;

                    const auto totalSize = bytesPerSample * (DecodedAudio->nb_samples
                        * ChannelCount);

                    newBuffer->DecodedData.resize(totalSize);

                    //uint8_t* output[] = { &newBuffer->DecodedData[0], nullptr};
                    uint8_t* output = &newBuffer->DecodedData[0];
                    
                    // Convert into the output data
                    if(swr_convert(AudioConverter, &output, totalSize,
                            const_cast<const uint8_t**>(DecodedAudio->data),
                            DecodedAudio->nb_samples) < 0)
                    {
                        std::cerr << "Invalid audio stream, converting failed"
                            << std::endl;
                        AudioCodec = nullptr;
                        continue;
                    }
                    
                    //memcpy(&newBuffer->DecodedData[0],
                    //&DecodedAudio->data[0], totalSize);
                    
                    std::lock_guard<std::mutex> lock(AudioMutex);
                    ReadAudioData.push_back(std::move(newBuffer));
                }

                len = std::min(len, packet.size);

                packet.data += len;
                packet.size -= len;
                    
            } while (packet.size > 0);
                
            av_packet_unref(&orig_pkt);
        }
        
        return true;        
    }

    bool
        decodeFrame(AVPacket &packet)
    {
        int frameFinished = 0;
        
        // Decode video frame
        if(avcodec_decode_video2(VideoCodec, DecodedFrame, &frameFinished, &packet) < 0){

            std::cerr << "Decoding video frame failed" << std::endl;
            return false;
        }
                
        // Was it a complete frame
        if(frameFinished){
                    
            // Convert the image from its native format to RGB
            if(sws_scale(ImageConverter, DecodedFrame->data, DecodedFrame->linesize,
                    0, FrameHeight,
                    ConvertedFrame->data, ConvertedFrame->linesize) < 0)
            {

                // Failed to convert frame //
                std::cerr << "Converting video frame failed" << std::endl;
                return false;
            }

            // Seems like DecodedFrame->pts contains garbage
            // and packet.pts is the timestamp in VideoCodec->time_base
            CurrentlyDecodedTimeStamp = packet.pts * VideoTimeBase;
            return true;
        }

        return false;
    }

    void
        update()
    {
        const auto now = ClockType::now();

        const auto elapsed = now - LastUpdateTime;
        LastUpdateTime = now;
        
        PassedTimeSeconds += std::chrono::duration_cast<
            std::chrono::duration<float>>(elapsed).count();

        // Start playing audio. Hopefully at the same time as the first frame of the
        // video is decoded
        if(!IsPlayingAudio && PlayingSource){

            IsPlayingAudio = true;
            PlayingSource->play2d(false);
        }

        // Only decode if there isn't a frame ready
        while(!NextFrameReady){

            std::unique_lock<std::mutex> lock(ReadVideoDataMutex);

            if(ReadVideoData.empty()){

                ReadVideoDataMutex.unlock();
                
                // Decode a packet if none are in queue
                if(!readOnePacket()){

                    // There are no more frames, end the playback
                    endReached();
                    return;
                }

                ReadVideoDataMutex.lock();
            }

            // Decode packets until a frame is done
            NextFrameReady = decodeFrame(ReadVideoData.front()->packet);

            ReadVideoData.pop_front();
        }

        if(PassedTimeSeconds >= CurrentlyDecodedTimeStamp){
            // Update the Ogre texture //
            updateOgreTexture();
            NextFrameReady = false;
        }
    }

    void
        updateOgreTexture()
    {
        // Make sure the pixel format matches the one in create manual texture
        Ogre::PixelBox pixelView(FrameWidth, FrameHeight, 1,
            OGRE_IMAGE_FORMAT,
            // The data[0] buffer has some junk before the actual data so don't use that
            /*&ConvertedFrame->data[0]*/ ConvertedFrameBuffer);

        Ogre::HardwarePixelBufferSharedPtr buffer = Texture->getBuffer();
        buffer->blitFromMemory(pixelView);
    }
    
    void
        endReached()
    {
        // Maybe use stop here?
        // But VideoPlayer::update has to detect somehow that the end has been
        // reached
        close();
    }

    void
        seek(float time)
    {
        if(time < 0)
            time = 0;

        const auto seekPos = static_cast<uint64_t>(time * AV_TIME_BASE);

        const auto timeStamp = av_rescale_q(seekPos, AV_TIME_BASE_Q,
            Context->streams[VideoIndex]->time_base);
        
        av_seek_frame(Context, VideoIndex, timeStamp, AVSEEK_FLAG_BACKWARD);
        
    }
    
    void
        dumpInfo()
    {
        if(!Context)
            return;

        // Dump information about file onto standard error
        av_dump_format(Context, 0, TextureName.c_str(), 0);
    }

    void
        close()
    {

        // Dump remaining video frames //
        {
            std::lock_guard<std::mutex> lock(ReadVideoDataMutex);

            ReadVideoData.clear();
        }
        
        unhookAudio();
        
        // Video and Audio codecs are released by Context
        VideoCodec = nullptr;
        AudioCodec = nullptr;

        if(ImageConverter){
            
            sws_freeContext(ImageConverter);
            ImageConverter = nullptr;
        }

        if(AudioConverter)
            swr_free(&AudioConverter);

        if(DecodedFrame)
            av_frame_free(&DecodedFrame);
        if(DecodedAudio)
            av_frame_free(&DecodedAudio);
        if(ConvertedFrameBuffer)
            av_freep(&ConvertedFrameBuffer);
        if(ConvertedFrame)
            av_frame_free(&ConvertedFrame);

        if(ResourceReader){

            if(ResourceReader->buffer){

                av_free(ResourceReader->buffer);
                ResourceReader->buffer = nullptr;
            }
            
            av_free(ResourceReader);
            ResourceReader = nullptr;
        }

        if(Context){

            avformat_free_context(Context);
            Context = nullptr;
        }

        Texture.setNull();
    }

    size_t
        readAudioData(uint8_t* output,
            size_t amount)
    {
        if(amount < 1 || !AudioStreamer)
            return 0;
        
        std::unique_lock<std::mutex> lock(AudioMutex);

        while(ReadAudioData.empty()){

            // Will deadlock if we don't unlock this
            lock.unlock();
            
            if(!this->readOnePacket()){

                // Stream ended //
                return 0;
            }
            
            lock.lock();
        }

        auto& dataVector = ReadAudioData.front()->DecodedData;

        if(amount >= dataVector.size()){

            // Can move an entire packet //
            const auto movedDataCount = dataVector.size();

            memcpy(output, &dataVector[0], movedDataCount);

            ReadAudioData.pop_front();
            
            return movedDataCount;
        }

        // Need to return a partial packet //
        const auto movedDataCount = amount;
        const auto leftSize = dataVector.size() - movedDataCount;

        memcpy(output, &dataVector[0], movedDataCount);

        dataVector = std::vector<uint8_t>(
                dataVector.end() - leftSize, dataVector.end());
        
        return movedDataCount;
    }

    /**
    This method doesn't work correctly if the format is RGBA, it needs to be RGB
    */
    void
        takeScreenshot()
    {
        std::stringstream fileName;
        fileName << TextureName << "_snapshot_" << ++ScreenshotCount << ".ppm";
  
        // Open file
        std::ofstream stream(fileName.str(), std::ios::binary);

        if(!stream.is_open())
            return;
        
        // Write header printf format: "P6\n%d %d\n255\n", width, height
        stream << "P6\n" << FrameWidth << " " << FrameHeight << "\n255\n";
        
        // Then dump the pixel data
        for(int32_t y = 0; y < FrameHeight; ++y){

            stream.write(reinterpret_cast<char*>(ConvertedFrame->data[0] +
                    + (y * ConvertedFrame->linesize[0])), FrameWidth * 3);
        }

        stream.close();
    }

    void
        saveOgreTexture()
    {
        std::stringstream fileName;
        fileName << TextureName << "_texturestate_" << ++ScreenshotCount << ".png";

        auto readBuffer = Texture->getBuffer();
        readBuffer->lock(Ogre::HardwareBuffer::HBL_NORMAL );
        const Ogre::PixelBox& pb = readBuffer->getCurrentLock();    
 
        Ogre::Image img;
        img.loadDynamicImage (static_cast<uint8_t*>(pb.data), Texture->getWidth(),
            Texture->getHeight(), Texture->getFormat());
        
        img.save(fileName.str());
        readBuffer->unlock();
    }

    /**
    @todo Check can this deadlock. It might deadlock if the video is closed instantly
    after a stream object was created. Maybe use a try lock here
    */
    void
        unhookAudio()
    {
        IsPlayingAudio = false;
        
        auto* stream = AudioStreamer;
        AudioStreamer = nullptr;

        if(stream)
            stream->onStreamEnded();

        std::lock_guard<std::mutex> lock(AudioMutex);
        
        if(PlayingSource){
            
            SoundManager::getSingleton()->destroyAudioSource(PlayingSource);
            PlayingSource = nullptr;
        }

        ReadAudioData.clear();
    }

    /**
    @brief Resets passed time since last update, use when unpausing or starting playback
    */
    void
        resetClock()
    {
        LastUpdateTime = ClockType::now();
    }

    AVIOContext* ResourceReader = nullptr;
    Ogre::DataStreamPtr Stream;
    AVFormatContext* Context = nullptr;

    std::string TextureName;

    AVCodecContext* VideoCodec = nullptr;
    int VideoIndex = 0;
    /**
    How many timestamp units are in a second in the video stream
    */
    float VideoTimeBase = 1.f;
    
    AVCodecContext* AudioCodec = nullptr;
    int AudioIndex = 0;

    AVFrame* DecodedFrame = nullptr;
    AVFrame* DecodedAudio = nullptr;
    

    /**
    Once a frame has been loaded to DecodedFrame it is converted to a format that Ogre texture
    can accept into this frame
    */
    AVFrame* ConvertedFrame = nullptr;

    uint8_t* ConvertedFrameBuffer = nullptr;
    
    // Required size for a single converted frame
    size_t ConvertedBufferSize = 0;

    int32_t FrameWidth = 0;
    int32_t FrameHeight = 0;

    SwsContext* ImageConverter = nullptr;

    SwrContext* AudioConverter = nullptr;

    Ogre::TexturePtr Texture;

    int ScreenshotCount = 0;

    int SampleRate = 0;
    int ChannelCount = 0;

    std::list<std::unique_ptr<ReadAudioPacket>> ReadAudioData;
    std::mutex AudioMutex;

    SoundMemoryStream* AudioStreamer = nullptr;
    cAudio::IAudioSource* PlayingSource = nullptr;


    std::list<std::unique_ptr<ReadVideoPacket>> ReadVideoData;
    std::mutex ReadVideoDataMutex;

    VideoPlayer* OutsidePtr;

    //! Used to start the audio playback once
    bool IsPlayingAudio = false;

    // Timing control
    float PassedTimeSeconds = 0.f;
    float CurrentlyDecodedTimeStamp = 0.f;
    
    bool NextFrameReady = false;

    
    ClockType::time_point LastUpdateTime;

    std::mutex ReadPacketMutex;
};


int
    OgreResource_Read(
        void *user_data,
        uint8_t *buf,
        int buf_size)
{
    Ogre::DataStreamPtr stream = static_cast<VideoPlayerImpl*>(user_data)->Stream;
    try
    {
        return stream->read(buf, buf_size);
    }
    catch (std::exception& e)
    {
        return 0;
    }
}

int
    OgreResource_Write(
        void *user_data,
        uint8_t *buf,
        int buf_size)
{
    Ogre::DataStreamPtr stream = static_cast<VideoPlayerImpl*>(user_data)->Stream;
    try
    {
        return stream->write(buf, buf_size);
    }
    catch (std::exception& e)
    {
        return 0;
    }
}

int64_t
    OgreResource_Seek(
        void *user_data,
        int64_t offset,
        int whence)
{
    Ogre::DataStreamPtr stream = static_cast<VideoPlayerImpl*>(user_data)->Stream;

    whence &= ~AVSEEK_FORCE;
    if(whence == AVSEEK_SIZE)
        return stream->size();
    if(whence == SEEK_SET)
        stream->seek(offset);
    else if(whence == SEEK_CUR)
        stream->seek(stream->tell()+offset);
    else if(whence == SEEK_END)
        stream->seek(stream->size()+offset);
    else
        return -1;

    return stream->tell();
}


VideoPlayer::VideoPlayer() : p_impl(std::make_unique<VideoPlayerImpl>(this)){
    
}

VideoPlayer::~VideoPlayer(){
    p_impl.reset();
}

bool
    VideoPlayer::playVideo(
        const std::string &resourceName
    )
{
    close();
    return p_impl->open(resourceName);
}

void
    VideoPlayer::close()
{
    p_impl->close();
}

bool
    VideoPlayer::hasAudio() const
{
    return p_impl->AudioCodec != nullptr;
}

float
    VideoPlayer::getCurrentTime() const
{
    if(!p_impl->DecodedFrame)
        return 0.f;

    return p_impl->CurrentlyDecodedTimeStamp;
}

float
    VideoPlayer::getDuration() const
{
    if(!p_impl->Context)
        return 0.f;

    return p_impl->Context->duration / AV_TIME_BASE;
}

void
    VideoPlayer::seek(
        float time
    )
{
    if(!p_impl->Context)
        return;

    if(time >= getDuration())
    {
        pause();
        return;
    }

    p_impl->seek(time);
}

void
    VideoPlayer::play()
{
    
}

void
    VideoPlayer::pause()
{
    
}

bool
    VideoPlayer::isPaused() const
{
    return false;
}

bool
    VideoPlayer::update()
{
    p_impl->update();
    return p_impl->isOpen();
}

std::string
    VideoPlayer::getTextureName() const
{
    return p_impl->TextureName;
}

int32_t
    VideoPlayer::getVideoWidth() const
{
    return p_impl->FrameWidth;
}

int32_t
    VideoPlayer::getVideoHeight() const
{
    return p_impl->FrameHeight;
}

int
    VideoPlayer::getAudioChannelCount() const
{
    return p_impl->ChannelCount;
}

int
    VideoPlayer::getAudioSampleRate() const
{
    return p_impl->SampleRate;
}

size_t
    VideoPlayer::readAudioData(uint8_t* output,
        size_t amount)
{
    if(amount == 0 || output == nullptr)
        return 0;

    return p_impl->readAudioData(output, amount);
}


void
    VideoPlayer::loadFFMPEG()
{
    av_register_all();
}

void
    VideoPlayer::streamReportingIn(SoundMemoryStream* stream)
{
    // There might be a race condition with this
    if(!p_impl)
        return;
    
    std::lock_guard<std::mutex> lock(p_impl->AudioMutex);

    p_impl->AudioStreamer = stream;
}
}
