#include "gui/CEGUIVideoPlayer.h"

#include "engine/engine.h"
#include "game.h"
#include "script_wrappers.h"
#include "scripting/luajit.h"

#include <OgreVector3.h>
#include <OgreMaterialManager.h>
#include <OgreMaterial.h>
#include <OgreTechnique.h>
#include <OgreTextureManager.h>
#include <functional>

#include <CEGUI/Element.h>
#include <CEGUI/InputEvent.h>
#include <CEGUI/Image.h>
#include <CEGUI/RendererModules/Ogre/Texture.h>

#include "VideoPlayer.h"

using namespace thrive;

CEGUIVideoPlayer::CEGUIVideoPlayer(
    std::string name,
    int width,
    int height
) : CEGUIWindow("Thrive/Image",name)
{
    Ogre::MaterialPtr videoMaterial = Ogre::MaterialManager::getSingleton().create(
                "VideoMaterial"+name, Ogre::ResourceGroupManager::DEFAULT_RESOURCE_GROUP_NAME);
    m_videoMaterialPass = videoMaterial->getTechnique( 0 )->getPass( 0 );
    m_videoPlayer = std::unique_ptr<VideoPlayer>(new VideoPlayer());

    m_videoImage = static_cast<CEGUI::BitmapImage*>(
        &CEGUI::ImageManager::getSingleton().create(
            "BitmapImage", "ThriveGeneric/VideoImage"));

    m_window->setWidth(CEGUI::UDim(0,width));
    m_window->setHeight(CEGUI::UDim(0,height));
}

CEGUIVideoPlayer::CEGUIVideoPlayer(
    std::string name
) : CEGUIVideoPlayer(name, 0, 0)
{
    m_window->setWidth(CEGUI::UDim(1.0,0));
    m_window->setHeight(CEGUI::UDim(1.0,0));
}


CEGUIVideoPlayer::~CEGUIVideoPlayer()
{
    delete m_tex;
}


void CEGUIVideoPlayer::luaBindings(
    sol::state &lua
){
    lua.new_usertype<CEGUIVideoPlayer>("CEGUIVideoPlayer",

        sol::constructors<sol::types<std::string, int, int>,
        sol::types<std::string>>(),
        
        sol::base_classes, sol::bases<CEGUIWindow>(),

        "play", &CEGUIVideoPlayer::play,
        "close", &CEGUIVideoPlayer::close,
        "setVideo", &CEGUIVideoPlayer::setVideo,
        "update", &CEGUIVideoPlayer::update,
        "getDuration", &CEGUIVideoPlayer::getDuration,
        "getCurrentTime", &CEGUIVideoPlayer::getCurrentTime,
        "seek", &CEGUIVideoPlayer::seek,

        "destroyVideoPlayer", &destroyVideoPlayer //Static
    );
}


void
CEGUIVideoPlayer::play() {
    m_videoPlayer->play();
}

void
CEGUIVideoPlayer::destroyVideoPlayer(CEGUIVideoPlayer* player)
{
    delete player;
}

void
CEGUIVideoPlayer::pause() {

}

void
CEGUIVideoPlayer::close() {
    m_videoPlayer->close();
}

void
CEGUIVideoPlayer::update() {
    m_videoPlayer->update();
}

bool
CEGUIVideoPlayer::isPaused() {
    return false;
}

double
CEGUIVideoPlayer::getDuration(){
    return m_videoPlayer->getDuration();
}

double
CEGUIVideoPlayer::getCurrentTime(){
    return m_videoPlayer->getCurrentTime();
}

void
CEGUIVideoPlayer::setVideo(
  std::string videoName
) {
    m_window->setProperty("Image", "ThriveGeneric/VideoImage");
    m_videoPlayer->playVideo(videoName);
    m_videoMaterialPass->setLightingEnabled( false );
    Ogre::TextureUnitState *m_tex = m_videoMaterialPass->createTextureUnitState();
    m_tex->setTextureName(m_videoPlayer->getTextureName());
    auto* renderer = CEGUI::System::getSingleton().getRenderer();
    CEGUI::Texture& texture = renderer->createTexture("VideoTexture");

    CEGUI::OgreTexture& rendererTexture = static_cast<CEGUI::OgreTexture&>(texture);

    rendererTexture.setOgreTexture(Ogre::TextureManager::getSingleton().getByName(m_videoPlayer->getTextureName()), false);

    CEGUI::OgreRenderer* ogreRenderer = static_cast<CEGUI::OgreRenderer*>(CEGUI::System::getSingleton().getRenderer());
    bool isTextureTargetVerticallyFlipped = ogreRenderer->isTexCoordSystemFlipped();
    CEGUI::Rectf imageArea;
    int videoW = m_videoPlayer->getVideoWidth();
    int videoH = m_videoPlayer->getVideoHeight();
    if (isTextureTargetVerticallyFlipped){
        imageArea= CEGUI::Rectf(0.0f, videoW, videoH, 0.0f);
    }
    else {
        imageArea= CEGUI::Rectf(0.0f, 0.0f, videoW, videoH);
    }
    m_videoImage->setImageArea(imageArea);
    // You most likely don't want autoscaling for RTT images. If you display it in stretched-mode inside a button or Generic/Image widget, then this setting does not play a role anyways.
    m_videoImage->setAutoScaled(CEGUI::ASM_Disabled);
    m_videoImage->setTexture(&rendererTexture);
}

void
CEGUIVideoPlayer::seek(
  double time
) {
    m_videoPlayer->seek(time);
}
