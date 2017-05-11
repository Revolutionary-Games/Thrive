#include "gui_texture_helper.h"

#include <OgreRoot.h>
#include <OgreImage.h>

using namespace thrive;

std::shared_ptr<Ogre::Image> GUITextureHelper::getTexture(const std::string &name){

    // Use existing if found //
    auto find = m_loadedImages.find(name);

    if(find != m_loadedImages.end()){

        return find->second;
    }

    auto img = std::make_shared<Ogre::Image>();

    img->load(name, Ogre::ResourceGroupManager::DEFAULT_RESOURCE_GROUP_NAME);

    std::cout << "Loaded new AlphaHit texture: " << name << " (" <<
        img->getWidth() << "x" << img->getHeight() << ")" << std::endl;

    m_loadedImages[name] = img;

    return img;
}

