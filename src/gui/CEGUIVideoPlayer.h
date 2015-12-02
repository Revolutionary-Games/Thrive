#pragma once

#include <CEGUI/CEGUI.h>
#include <luabind/object.hpp>
#include <OgreVector2.h>
#include <OISKeyboard.h>

#include "gui/CEGUIWindow.h"

namespace luabind {
class scope;
}

namespace Video {
class VideoPlayer;
}

namespace Ogre {
    class BitmapImage;
    class Pass;
}

namespace thrive {

//Note, currently doesn't support multiple video players (crashes) but since this isn't needed atm it's left unfixed

class CEGUIVideoPlayer : public CEGUIWindow{

public:

    /**
    * @brief Constructor
    *
    * @param name
    * @param width
    * @param height
    *
    **/
    CEGUIVideoPlayer(
        std::string name,
        int width,
        int height
    );

    /**
    * @brief Constructor
    *
    * @param name
    *
    * @note This constructor makes a fullscreen player
    *
    **/
    CEGUIVideoPlayer(
        std::string name
    );


    /**
    * @brief Destructor
    **/
    virtual
    ~CEGUIVideoPlayer();


    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - CEGUIWindow::CEGUIWindow(string, int, int)
    * - CEGUIWindow::isNull
    * - CEGUIWindow::play
    * - CEGUIWindow::pause
    * - CEGUIWindow::setVideo
    * - CEGUIWindow::update
    * - CEGUIWindow::getDuration
    * - CEGUIWindow::getCurrentTime
    * - CEGUIWindow::seek
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Sets the video to play
    *
    * @param videoName
    *  The name of the video to play. Must be loaded by ogres resource manager
    *
    */
    void
    setVideo(
        std::string videoName
    );

    void
    play();

    void
    pause();

    void
    update();

    bool
    isPaused();

    double
    getDuration();

    double
    getCurrentTime();

    void
    seek(
        double time
    );

    //Necessary to avoid ogre error when exiting game
    static void
    destroyVideoPlayer(CEGUIVideoPlayer* player);

private:

    std::unique_ptr<Video::VideoPlayer> m_videoPlayer;
    Ogre::Pass * m_videoMaterialPass = nullptr;
    CEGUI::BitmapImage* m_videoImage = nullptr;
    Ogre::TextureUnitState* m_tex = nullptr;
};

}
