#include "gui/AlphaHitWindow.h"

#include <CEGUI/CoordConverter.h>

#include <OgreRoot.h>
#include <OgreTextureManager.h>
#include <OgreHardwarePixelBuffer.h>

//----------------------------------------------------------------------------//
class TextureAlphaCheckArea{
public:

    TextureAlphaCheckArea(const Ogre::TexturePtr &texture,
        uint32_t x, uint32_t y,
        uint32_t width, uint32_t height) :
        m_texture(texture), m_x(x), m_y(y), m_width(width), m_height(height),
        m_readPixel(1, 1, 1, Ogre::PixelFormat::PF_A8R8G8B8)
    {
        if(m_texture.isNull()){

            throw std::runtime_error("TextureAlphaCheckArea given null texture");
        }
    }

    //! Starts loading m_texture if it isn't loading already
    //! \todo Actual background loading. Now this just loads it in the primary thread
    void
        makeSureIsLoading()
    {
        if(!isReady())
            m_texture->load();
    }

    //! \returns True if the texture is ready for use
    bool
        isReady() const
    {
        return m_texture->isLoaded();
    }

    //! \returns Pixel at position
    Ogre::ColourValue
        getPixel(uint32_t x, uint32_t y)
    {
        assert(isReady());
        
        auto buffer = m_texture->getBuffer();
    
        assert(buffer);

        const auto offsetX = x + m_x;
        const auto offsetY = y + m_y;

        // Return empty if out of range
        if(offsetX - 1 < m_x || offsetY - 1 < m_y ||
            offsetX + 1 > m_x + m_width ||
            offsetY + 1 > m_y + m_height)
        {
            return Ogre::ColourValue::ZERO;
        }
        
        // Single pixel from texture
        uint32_t pixelDataHolder;
        m_readPixel.data = &pixelDataHolder;

        try{
            buffer->blitToMemory(Ogre::Box(offsetX, offsetY, offsetX + 1, offsetY + 1),
            m_readPixel);
        
        } catch(const Ogre::Exception &e){

            // This seems to sometimes trigger completely randomly //
            // std::cout << "Error: AlphaHitWindow failed to read pixel data: " <<
            //     e.what() << std::endl;
            
            return Ogre::ColourValue::ZERO;
        }

        // We could also bit shift the alpha out of pixelDataHolder
        return m_readPixel.getColourAt(0, 0, 0);
    }

    const Ogre::TexturePtr m_texture;
    const uint32_t m_x;
    const uint32_t m_y;
    const uint32_t m_width;
    const uint32_t m_height;

    Ogre::PixelBox m_readPixel;
};


//----------------------------------------------------------------------------//
const CEGUI::String AlphaHitWindow::WidgetTypeName("AlphaHitWindow");

//----------------------------------------------------------------------------//
AlphaHitWindow::AlphaHitWindow(const CEGUI::String& type, const CEGUI::String& name) :
    CEGUI::PushButton(type, name)
{
}
//----------------------------------------------------------------------------//
AlphaHitWindow::~AlphaHitWindow()
{
}

//----------------------------------------------------------------------------//
bool
AlphaHitWindow::isHit(
    const CEGUIVector2& position,
    const bool allow_disabled
) const {

    // still do the rect test, since we only want to do the detailed test
    // if absolutely neccessary.
    if (!CEGUI::PushButton::isHit(position, allow_disabled))
        return false;

    // Retrieve the texture used for this window, if we don't have it already //
    if(!m_hitTestTexture){

        const auto imageProperty = this->getProperty("Image");

        if(imageProperty.empty()){

            // No image... //
            // Our rect already matched so return true //
            return true;
        }
    
        auto texture = getTextureFromCEGUIImageName(imageProperty);

        if(!texture){

            throw std::runtime_error("AlphaHitWindow: couldn't find source texture to "
                "use for checks");
        }
        
        m_hitTestTexture = std::move(texture);
    }

    assert(m_hitTestTexture);

    m_hitTestTexture->makeSureIsLoading();

    if(!m_hitTestTexture->isReady()){

        // Not ready yet, don't let user click //
        return false;
    }

    // Read the pixel under mouse pos //
    const CEGUIVector2 relativePos = CEGUI::CoordConverter::screenToWindow(*this, position); 

    const auto pixel = m_hitTestTexture->getPixel(relativePos.x, relativePos.y);
        
    // If it isn't completely transparent, hit //
    return pixel.a > 0.01f;
}

//----------------------------------------------------------------------------//
void
    skipUntilNumber(std::istream &stream);

void
    skipUntilNumber(std::istream &stream)
{

    while(!isdigit(stream.peek())){

        stream.get();
    }
}

std::unique_ptr<TextureAlphaCheckArea>
    AlphaHitWindow::getTextureFromCEGUIImageName(const CEGUI::String& name)
{
    // Extract the part of name after /.
    // Here's an example: ThriveGeneric/MenuNormal
    const auto slash = name.find_last_of('/');

    if(slash == std::string::npos)
        throw std::runtime_error("AlphaHitWindow: invalid image name "
            "(doesn't have '/' in it)");

    const auto namePart = name.substr(slash + 1);
    const auto schemaPart = name.substr(0, slash);

    if(namePart.empty())
        throw std::runtime_error("AlphaHitWindow: invalid image name "
            "(part after '/' is empty)");

    if(schemaPart.empty())
        throw std::runtime_error("AlphaHitWindow: invalid image name "
            "(part before '/' is empty)");

    const std::string setName = schemaPart.c_str();

    auto texture = std::get<0>(Ogre::Root::getSingleton().getTextureManager()->
        createOrRetrieve(setName + ".png",
            Ogre::ResourceGroupManager::DEFAULT_RESOURCE_GROUP_NAME)).
        dynamicCast<Ogre::Texture>();

    if(texture.isNull())
        throw std::runtime_error("AlphaHitWindow: didn't find texture file for image");
    
    // Find the offset into the file //
    std::ifstream imageset("../gui/imagesets/" + setName + ".imageset");

    if(!imageset.good())
        throw std::runtime_error("AlphaHitWindow: didn't find imageset file for image");

    // Read the file until a name="our image here" //
    while(imageset.good()){

        if(imageset.get() != 'n')
            continue;

        // We found an 'n'
        // We should get next "name=""
        if(imageset.get() != 'a')
            continue;
        if(imageset.get() != 'm')
            continue;
        if(imageset.get() != 'e')
            continue;
        if(imageset.get() != '=')
            continue;
        if(imageset.get() != '"')
            continue;

        // Now there should be the last part of name //
        bool matched = true;
        
        for(size_t i = 0; i < namePart.size(); ++i){

            if(namePart[i] != imageset.get()){

                matched = false;
                break;
            }
        }

        if(matched)
            break;
    }

    if(!imageset.good())
        throw std::runtime_error("AlphaHitWindow: didn't find image properties "
            "in imageset file");

    // We have found the right place. Now read 4 numbers //
    uint32_t x = 0, y = 0, width = 0, height = 0;

    skipUntilNumber(imageset);
    
    imageset >> x;

    skipUntilNumber(imageset);

    imageset >> y;

    skipUntilNumber(imageset);

    imageset >> width;
    
    skipUntilNumber(imageset);

    imageset >> height;

    if(!imageset.good() || x == 0 || y == 0 || width == 0 || height == 0)
        throw std::runtime_error("AlphaHitWindow: couldn't read numbers after "
            "image name");

    return std::make_unique<TextureAlphaCheckArea>(texture, x, y, width, height);
}
