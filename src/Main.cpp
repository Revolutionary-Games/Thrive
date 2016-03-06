#include <boost/thread.hpp>
#include <cstdio>
#include <fstream>
#include "game.h"
#include <iostream>
#include <OgreRoot.h>
#include <string>

#if OGRE_PLATFORM == OGRE_PLATFORM_WIN32
#define WIN32_LEAN_AND_MEAN
#include "windows.h"
#endif

#ifdef __cplusplus
extern "C" {
#endif

    /**
    *
    * Init function. Anything that has to happen first goes here
    *
    * @return: 0 for success, 1 for failure
    **/
    int
    initThrive();

#if OGRE_PLATFORM == OGRE_PLATFORM_WIN32
    INT WINAPI WinMain( HINSTANCE hInst, HINSTANCE, LPSTR strCmdLine, INT )
#else
    int main(int argc, char *argv[])
#endif
    {
        if (initThrive() != 0){
            return -1;
        }
        using namespace thrive;
        Game& game = Game::instance();
        game.run();
        return 0;
    }

    int
    initThrive(){
        std::ifstream versionFile("thriveversion.ver");
        std::ifstream mainLayout("../gui/layouts/MainMenu.layout");
        std::ofstream tempFile("../gui/layouts/MainMenuTemp.layout");

        std::string version;
        std::getline(versionFile, version);

        std::string str;
        std::string needle = "<Property name=\"Text\" value=\"v";
        std::size_t found = 0;

        while (std::getline(mainLayout, str)){
            // Process str
            found = str.find(needle);
            if (found != std::string::npos)
            {
                tempFile << str.substr(0, str.find("\"v")) << "\"v" << version << "\" />" << '\n';
                continue;
                //std::cout << "<Property name=\"Text\" value=\"v" << version << "\" />" << '\n';
            }
            tempFile << str << '\n';
        }
        versionFile.close();
        mainLayout.close();
        tempFile.close();

        if( remove( "../gui/layouts/MainMenu.layout" ) != 0 ){
            perror( "Error deleting main menu layout" );
            return 1;
        }

        int result = std::rename("../gui/layouts/MainMenuTemp.layout", "../gui/layouts/MainMenu.layout");
        if ( result != 0 ){
            perror( "Error renaming file" );
            return 1;
        }
        return 0;

    }

#ifdef __cplusplus
}
#endif
