#include <OgreRoot.h>

#include "ogre/ogre_engine.h"

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
        // Graphics engine
        OgreEngine ogreEngine;
        EngineRunner ogreRunner(ogreEngine);
        ogreRunner.start();
        boost::this_thread::sleep(boost::posix_time::seconds(3));
        ogreRunner.stop();
        return 0;
    }
 
#ifdef __cplusplus
}
#endif
