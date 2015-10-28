#pragma once
#include "ogre-ffmpeg/audiofactory.hpp"

namespace thrive{



class FFMPEGAudioFactory : public Video::MovieAudioFactory
{
    virtual boost::shared_ptr<Video::MovieAudioDecoder> createDecoder(Video::VideoState* videoState);
};

}
