#include <OgreRoot.h>

#include "game.h"

#include <boost/thread.hpp>

#if OGRE_PLATFORM == OGRE_PLATFORM_WIN32
#define WIN32_LEAN_AND_MEAN
#include "windows.h"
#endif
 
#ifdef __cplusplus
extern "C" {
#endif
 
#if OGRE_PLATFORM == OGRE_PLATFORM_WIN32
    INT WINAPI WinMain( HINSTANCE hInst, HINSTANCE, LPSTR strCmdLine, INT )
#else
    int main(int argc, char *argv[])
#endif
    {
        using namespace thrive;
        Game& game = Game::instance();
        game.run();
        return 0;
    }
 
#ifdef __cplusplus
}
#endif
