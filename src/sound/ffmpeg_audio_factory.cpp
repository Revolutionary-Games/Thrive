#include "sound/ffmpeg_audio_factory.h"

#include <stdexcept>

using namespace thrive;

class FFMPEGAudioDecoder : public Video::MovieAudioDecoder
{
public:

    virtual void adjustAudioSettings(AVSampleFormat& sampleFormat, uint64_t& channelLayout, int& sampleRate);

    FFMPEGAudioDecoder(Video::VideoState* videoState);
    ~FFMPEGAudioDecoder();

private:
//    SDL_AudioDeviceID mDeviceId;
};

//void MyAudioCallback(void*, Uint8*, int);

void FFMPEGAudioDecoder::adjustAudioSettings(AVSampleFormat& sampleFormat, uint64_t& channelLayout, int& /*sampleRate*/)
{

    sampleFormat = AV_SAMPLE_FMT_FLT;
    channelLayout = AV_CH_LAYOUT_STEREO;
  //  if (SDL_WasInit(SDL_INIT_AUDIO) == 0)
    //    SDL_Init(SDL_INIT_AUDIO);

    // For simplicity, we use fixed settings here and have SDL handle resampling if necessary.
    // This is not optimal, since we might have resampling done on SDL's side *and* the audio decoder side in the worst case.

    // We could also write the function parameters to the "want" struct,
    // then adjust the function parameters based on the "obtained" struct SDL_OpenAudio returns us.
    // This means SDL wouldn't have to do any resampling. The MovieAudioDecoder may still have to do resampling if the HW doesn't support the wanted format.

/*
    SDL_AudioSpec want, have;
    want.freq = sampleRate;
    want.format = AUDIO_F32;
    want.channels = 2;
    want.samples = 4096;
    want.userdata = this;
    want.callback = MyAudioCallback;

    mDeviceId = SDL_OpenAudioDevice(NULL, 0, &want, &have, 0);
    if (mDeviceId < 0)
        throw std::runtime_error(std::string("Failed to open audio: ") + SDL_GetError());
    else
        SDL_PauseAudioDevice(mDeviceId, 0);*/
}

FFMPEGAudioDecoder::FFMPEGAudioDecoder(Video::VideoState* videoState)
    : MovieAudioDecoder(videoState)
{
}

FFMPEGAudioDecoder::~FFMPEGAudioDecoder()
{
}

boost::shared_ptr<Video::MovieAudioDecoder> FFMPEGAudioFactory::createDecoder(Video::VideoState* videoState)
{
    boost::shared_ptr<Video::MovieAudioDecoder> ptr (new FFMPEGAudioDecoder(videoState));
    return ptr;
}


/*
void MyAudioCallback(void *userdata, Uint8 *stream, int len)
{
    FFMPEGAudioFactory *decoder = (FFMPEGAudioFactory *)userdata;
    size_t read = decoder->read((char*)stream, len);
    if (read < len)
    {
        // we don't have enough data, which means EOF was reached
        // fill the rest of the buffer with silence
        size_t sampleSize = av_get_bytes_per_sample(decoder->getOutputSampleFormat());
        Uint8* data[1];
        data[0] = stream+read;
        av_samples_set_silence((uint8_t**)data, 0, (len-read)/sampleSize, 1, decoder->getOutputSampleFormat());
    }
}*/
