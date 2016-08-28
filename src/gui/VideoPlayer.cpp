#include "gui/VideoPlayer.h"

#include "sound/sound_manager.h"
#include "sound/sound_memory_stream.h"
#include "util/make_unique.h"

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
#include <OgrePixelFormat.h>

// cAudio
#include <cAudio/IAudioSource.h>

#include <limits>
#include <fstream>
#include <boost/thread/mutex.hpp>
#include <chrono>

namespace thrive{

constexpr auto DEFAULT_READ_BUFFER = 32000;
constexpr Ogre::PixelFormat OGRE_IMAGE_FORMAT = Ogre::PF_BYTE_RGBA;
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
    Holds converted audio data that could not be immediately returned by readAudioData
    */
    struct ReadAudioPacket{

        std::vector<uint8_t> DecodedData;
    };

    /**
    Holds raw packets before sending
    */
    struct ReadPacket{

        ReadPacket(AVPacket* src){

            av_packet_move_ref(&packet, src);
        }

        ~ReadPacket(){

            av_packet_unref(&packet);
        }

        AVPacket packet;
    };


public:

    using ClockType = std::chrono::steady_clock;

    enum class DECODE_PRIORITY {

        VIDEO,
        AUDIO
    };

    enum class PACKET_READ_RESULT {

        ENDED,
        OK,
        QUEUE_FULL
    };

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
            throw std::bad_alloc();

        Context = avformat_alloc_context();
        if(!Context)
            throw std::bad_alloc();

        Context->pb = ResourceReader;

        if(avformat_open_input(&Context, resource.c_str(), nullptr, nullptr) < 0){

            // Context was freed automatically
            Context = nullptr;
            std::cerr << "FFMPEG failed to open video stream file resource" << std::endl;
            return false;
        }

        if(avformat_find_stream_info(Context, nullptr) < 0){

            std::cerr << "Failed to read video stream info" << std::endl;
            return false;
        }

        // Find audio and video streams //
        unsigned int videoStream = std::numeric_limits<unsigned int>::max();
        unsigned int audioStream = std::numeric_limits<unsigned int>::max();

        for(unsigned int i = 0; i < Context->nb_streams; ++i){

            if(Context->streams[i]->codecpar->codec_type == AVMEDIA_TYPE_VIDEO){

                videoStream = i;
                continue;
            }

            if(Context->streams[i]->codecpar->codec_type == AVMEDIA_TYPE_AUDIO){

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

        FrameWidth = Context->streams[videoStream]->codecpar->width;
        FrameHeight = Context->streams[videoStream]->codecpar->height;

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
            static_cast<AVPixelFormat>(Context->streams[VideoIndex]->codecpar->format),
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

        const auto ogreBufferSize = (buffer->getWidth() * buffer->getHeight()) * 4;

        if(ogreBufferSize != ConvertedBufferSize){

            std::cerr << "OGRE_IMAGE_FORMAT is incompatible with FFMPEG_DECODE_TARGET"
                << std::endl;
            std::cerr << ogreBufferSize << " size is not " << ConvertedBufferSize <<
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

        StreamValid = true;

        return true;
    }

    void
        openStream(
            unsigned int index,
            bool video
        )
    {
        auto* codec = avcodec_find_decoder(Context->streams[index]->codecpar->codec_id);

        if(!codec){

            throw std::runtime_error("unsupported codec used in video file");
        }

        auto* codecContext = avcodec_alloc_context3(codec);

        if(!codecContext){

            throw std::runtime_error("failed to allocate codec context");
        }

        // Try copying parameters //
        if(avcodec_parameters_to_context(codecContext,
                Context->streams[index]->codecpar) < 0)
        {
            avcodec_free_context(&codecContext);
            throw std::runtime_error("failed to copy parameters to codec context");
        }

        // Open the codec this is important to avoid segfaulting //
        // FFMPEG documentation warns that this is not thread safe
        const auto codecOpenResult = avcodec_open2(codecContext, codec, nullptr);

        if(codecOpenResult < 0){

            std::string errorMessage;
            errorMessage.resize(40);
            memset(&errorMessage[0], ' ', errorMessage.size());
            av_strerror(codecOpenResult, &errorMessage[0], errorMessage.size());
            
            std::cerr << "Error opening codec context: " << codecOpenResult <<
                "(" << errorMessage << ")" << std::endl;
            avcodec_free_context(&codecContext);
            throw std::runtime_error("codec failed to open");
        }

        if(video){

            VideoCodec = codecContext;
            VideoIndex = static_cast<int>(index);
            VideoTimeBase = static_cast<float>(Context->streams[index]->time_base.num) /
               static_cast<float>(Context->streams[index]->time_base.den);
            // VideoTimeBase = static_cast<float>(VideoCodec->time_base.num) /
            //     static_cast<float>(VideoCodec->time_base.den);

        } else {

            AudioCodec = codecContext;
            AudioIndex = static_cast<int>(index);
        }
    }

    bool
        isOpen() const
    {
        return StreamValid && VideoCodec != nullptr && ConvertedFrameBuffer != nullptr;
    }

    /**
    @brief Reads a single packet from either stream and pushes it into a queue
    */
    PACKET_READ_RESULT
        readOnePacket(DECODE_PRIORITY priority)
    {
        // Apparently Lua wants to call this after closing
        if(!Context || !StreamValid)
            return PACKET_READ_RESULT::ENDED;

        boost::lock_guard<boost::mutex> lock(ReadPacketMutex);

        // Decode queued packets first
        if(priority == DECODE_PRIORITY::VIDEO && !WaitingVideoPackets.empty()){

            // Try to send it //
            const auto result = avcodec_send_packet(VideoCodec,
                &WaitingVideoPackets.front()->packet);
            
            if(result == AVERROR(EAGAIN)){

                // Still wailing to send //
                return PACKET_READ_RESULT::QUEUE_FULL;
            }

            if(result < 0){

                // An error occured //
                std::cerr << "Video stream send error from queue, stopping playback"
                    << std::endl;
                StreamValid = false;
                return PACKET_READ_RESULT::ENDED;
            }

            // Successfully sent the first in the queue //
            WaitingVideoPackets.pop_front();
            return PACKET_READ_RESULT::OK;
            
        }
        if(priority == DECODE_PRIORITY::AUDIO && !WaitingAudioPackets.empty()){

            // Try to send it //
            const auto result = avcodec_send_packet(AudioCodec,
                &WaitingAudioPackets.front()->packet);
            
            if(result == AVERROR(EAGAIN)){

                // Still wailing to send //
                return PACKET_READ_RESULT::QUEUE_FULL;
            } 

            if(result < 0){

                // An error occured //
                std::cerr << "Audio stream send error from queue, stopping playback"
                    << std::endl;
                StreamValid = false;
                return PACKET_READ_RESULT::ENDED;
            }

            // Successfully sent the first in the queue //
            WaitingAudioPackets.pop_front();
            return PACKET_READ_RESULT::OK;
        }

        // If we had nothing in the right queue try to read more frames //

        AVPacket packet;
        //av_init_packet(&packet);

        if(av_read_frame(Context, &packet) < 0){

            // Stream ended //
            //av_packet_unref(&packet);
            return PACKET_READ_RESULT::ENDED;
        }

        if(!StreamValid){

            av_packet_unref(&packet);
            return PACKET_READ_RESULT::ENDED;
        }

        // Is this a packet from the video stream?
        if(packet.stream_index == VideoIndex) {

            // If not wanting this stream don't send it //
            if(priority != DECODE_PRIORITY::VIDEO){

                WaitingVideoPackets.push_back(make_unique<ReadPacket>(&packet));
                return PACKET_READ_RESULT::OK;
            }

            // Send it to the decoder //
            const auto result = avcodec_send_packet(VideoCodec, &packet);

            if(result == AVERROR(EAGAIN)){

                // Add to queue //
                WaitingVideoPackets.push_back(make_unique<ReadPacket>(&packet));
                return PACKET_READ_RESULT::QUEUE_FULL;
            }

            av_packet_unref(&packet);
            
            if(result < 0){

                std::cerr << "Video stream send error, stopping playback"
                    << std::endl;
                StreamValid = false;
                return PACKET_READ_RESULT::ENDED;
            }

            return PACKET_READ_RESULT::OK;

        } else if(packet.stream_index == AudioIndex && AudioCodec){
            
            // If audio codec is null audio playback is disabled //
            
            // If not wanting this stream don't send it //
            if(priority != DECODE_PRIORITY::AUDIO){

                WaitingAudioPackets.push_back(make_unique<ReadPacket>(&packet));
                return PACKET_READ_RESULT::OK;
            }
            
            const auto result = avcodec_send_packet(AudioCodec, &packet);

            if(result == AVERROR(EAGAIN)){

                // Add to queue //
                WaitingAudioPackets.push_back(make_unique<ReadPacket>(&packet));
                return PACKET_READ_RESULT::QUEUE_FULL;
            }

            av_packet_unref(&packet);

            if(result < 0){

                std::cerr << "Audio stream send error, stopping audio playback"
                    << std::endl;
                StreamValid = false;
                return PACKET_READ_RESULT::ENDED;
            }

            av_packet_unref(&packet);
            return PACKET_READ_RESULT::OK;
        }

        // Unknown stream, ignore
        av_packet_unref(&packet);
        return PACKET_READ_RESULT::OK;
    }

    bool
        decodeFrame()
    {
        const auto result = avcodec_receive_frame(VideoCodec, DecodedFrame);

        if(result >= 0){

            // Worked //
            
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
            // so we access that through pkt_pts
            //CurrentlyDecodedTimeStamp = DecodedFrame->pkt_pts * VideoTimeBase;
            //VideoTimeBase = VideoCodec->time_base.num / VideoCodec->time_base.den;
            CurrentlyDecodedTimeStamp = DecodedFrame->pkt_pts * VideoTimeBase;
            return true;
        }

        if(result == AVERROR(EAGAIN)){

            // Waiting for data //
            return false;
        }

        std::cerr << "Video frame receive failed, error: " << result << std::endl;
        return false;
    }

    void
        update()
    {
        if(!StreamValid)
            return;
        
        const auto now = ClockType::now();

        const auto elapsed = now - LastUpdateTime;
        LastUpdateTime = now;

        PassedTimeSeconds += std::chrono::duration_cast<
            std::chrono::duration<float>>(elapsed).count();

        // Start playing audio. Hopefully at the same time as the first frame of the
        // video is decoded
        if(!IsPlayingAudio && PlayingSource && AudioCodec){

            IsPlayingAudio = true;
            PlayingSource->play2d(false);
        }

        // Only decode if there isn't a frame ready
        while(!NextFrameReady){

            // Decode a packet if none are in queue
            if(readOnePacket(DECODE_PRIORITY::VIDEO) == PACKET_READ_RESULT::ENDED){

                // There are no more frames, end the playback
                endReached();
                return;
            }

            NextFrameReady = decodeFrame();
        }

        if(PassedTimeSeconds >= CurrentlyDecodedTimeStamp){

            //std::cout << "Showing frame at: " << PassedTimeSeconds << std::endl;
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

        std::cerr << "Audio seeking not implemented. " << std::endl;
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
        StreamValid = false;

        // Dump remaining video frames //
        {
            boost::lock_guard<boost::mutex> lock(ReadPacketMutex);

            WaitingVideoPackets.clear();
            WaitingAudioPackets.clear();
        }

        unhookAudio();

        // Video and Audio codecs are released by Context
        if(VideoCodec)
            avcodec_free_context(&VideoCodec);
        if(AudioCodec)
            avcodec_free_context(&AudioCodec);
        
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
        boost::lock_guard<boost::mutex> lock(AudioMutex);
        
        if(amount < 1 || !AudioStreamer || !AudioCodec || !StreamValid)
            return 0;

        // Receive audio packet //
        while(true){

            // First return from queue //
            if(!ReadAudioData.empty()){

                // Try to read from the queue //
                const auto readAmount = readDataFromAudioQueue(lock, output, amount);

                if(readAmount == 0){

                    // Queue is invalid... //
                    std::cerr << "Invalid audio queue, emptying the queue" << std::endl;
                    ReadAudioData.clear();
                    continue;
                }

                return readAmount;
            }

            const auto readResult = avcodec_receive_frame(AudioCodec, DecodedAudio);
            
            if(readResult == AVERROR(EAGAIN)){

                if(this->readOnePacket(DECODE_PRIORITY::AUDIO) == PACKET_READ_RESULT::ENDED){

                    // Stream ended //
                    return 0;
                }

                continue;
            }

            if(readResult < 0){

                // Some error //
                std::cerr << "Failed receiving audio packet, stopping audio playback"
                    << std::endl;
                StreamValid = false;
                return 0;
            }

            // Received audio data //

            // This is verified in open when setting up converting
            const auto bytesPerSample = 2;

            const auto totalSize = bytesPerSample * (DecodedAudio->nb_samples
                * ChannelCount);

            if(amount >= static_cast<size_t>(totalSize)){
                
                // Lets try to directly feed the converted data to the requester //
                if(swr_convert(AudioConverter, &output, totalSize,
                        const_cast<const uint8_t**>(DecodedAudio->data),
                        DecodedAudio->nb_samples) < 0)
                {
                    std::cerr << "Invalid audio stream, converting to audio read buffer failed"
                        << std::endl;
                    StreamValid = false;
                    return 0;
                }

                return totalSize;
            }
            
            // We need a temporary buffer //
            auto newBuffer = make_unique<ReadAudioPacket>();

            newBuffer->DecodedData.resize(totalSize);

            uint8_t* decodeOutput = &newBuffer->DecodedData[0];

            // Convert into the output data
            if(swr_convert(AudioConverter, &decodeOutput, totalSize,
                    const_cast<const uint8_t**>(DecodedAudio->data),
                    DecodedAudio->nb_samples) < 0)
            {
                std::cerr << "Invalid audio stream, converting failed"
                    << std::endl;
                StreamValid = false;
                return 0;
            }
                    
            ReadAudioData.push_back(std::move(newBuffer));
            continue;
        }

        // Execution never reaches here
    }

    size_t
        readDataFromAudioQueue(boost::lock_guard<boost::mutex> &audioLock,
            uint8_t* output,
            size_t amount)
    {
        (void)audioLock;

        if(ReadAudioData.empty())
            return 0;
        
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

        boost::lock_guard<boost::mutex> lock(AudioMutex);

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
    boost::mutex AudioMutex;

    SoundMemoryStream* AudioStreamer = nullptr;
    cAudio::IAudioSource* PlayingSource = nullptr;

    VideoPlayer* OutsidePtr;

    //! Used to start the audio playback once
    bool IsPlayingAudio = false;

    // Timing control
    float PassedTimeSeconds = 0.f;
    float CurrentlyDecodedTimeStamp = 0.f;

    bool NextFrameReady = false;

    //! Set to false if an error occurs and playback should stop
    bool StreamValid = false;


    ClockType::time_point LastUpdateTime;

    boost::mutex ReadPacketMutex;
    std::list<std::unique_ptr<ReadPacket>> WaitingVideoPackets;
    std::list<std::unique_ptr<ReadPacket>> WaitingAudioPackets;
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


VideoPlayer::VideoPlayer() : p_impl(make_unique<VideoPlayerImpl>(this)){

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

    boost::lock_guard<boost::mutex> lock(p_impl->AudioMutex);

    p_impl->AudioStreamer = stream;
}
}
