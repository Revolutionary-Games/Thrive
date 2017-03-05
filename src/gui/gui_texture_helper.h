#pragma once

#include <memory>
#include <map>

namespace Ogre{
class Image;
}

namespace thrive {

//! \brief Loads a texture once for GUI widgets that need to do pixel checking
class GUITextureHelper{
public:

    
    std::shared_ptr<Ogre::Image> getTexture(const std::string &name);

private:

    std::map<std::string, std::shared_ptr<Ogre::Image>> m_loadedImages;
};

}
