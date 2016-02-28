# - Find Ogre FFMPEG
#
# OgreFFMPEG_FOUND - True if OgreFFMPEG found.
# OgreFFMPEG_INCLUDE_DIR - where to find videoplayer.hpp, etc.
# OgreFFMPEG_LIBRARIES - List of libraries when using OgreFFMPEG.
#

SET( OgreFFMPEG_INCLUDE_SEARCH_DIR
       ${MINGW_ENV}/install/include
       ${MINGW_ENV}/install/include/ogre-ffmpeg
       /usr/local/include
       /usr/local/include/ogre-ffmpeg
       /usr/include
       /sw/include # Fink
       /opt/local/include # DarwinPorts
       /opt/csw/include # Blastwave
       /opt/include
       /usr/freeware/include
    )

SET( OgreFFMPEG_LIBRARY_SEARCH_DIR
        ${MINGW_ENV}/install/lib
        ${MINGW_ENV}/install/lib/Debug
        ${MINGW_ENV}/install/lib/Release
        /usr/local/lib
        /usr/lib
        /sw/lib
        /opt/local/lib
        /opt/csw/lib
        /opt/lib
        /usr/freeware/lib64
    )
    
    
FIND_PATH( OgreFFMPEG_INCLUDE_DIR "videoplayer.hpp"
PATHS ${OgreFFMPEG_INCLUDE_SEARCH_DIR})

FIND_LIBRARY( OgreFFMPEG_LIBRARIES
NAMES "libogre-ffmpeg-videoplayer.a"
PATHS ${OgreFFMPEG_LIBRARY_SEARCH_DIR})


# handle the QUIETLY and REQUIRED arguments and set OgreFFMPEG_FOUND to TRUE if
# all listed variables are TRUE
INCLUDE( "FindPackageHandleStandardArgs" )
FIND_PACKAGE_HANDLE_STANDARD_ARGS( "OgreFFMPEG" DEFAULT_MSG OgreFFMPEG_INCLUDE_DIR OgreFFMPEG_LIBRARIES)


