# - Find FFMPEG
#
#  FFMPEG_FOUND		 - system has FFMPEG
#  FFMPEG_INCLUDE_DIR	 - the include directories
#  FFMPEG_LIBRARY_DIR	 - the directory containing the libraries
#  FFMPEG_LIBRARIES	 - link these to use FFMPEG
#  FFMPEG_SWSCALE_FOUND	 - FFMPEG also has SWSCALE
#

SET( FFMPEG_INCLUDE_SEARCH_DIR
       ${MINGW_ENV}/install/include
       ${MINGW_ENV}/install/include/ogre-ffmpeg
       /usr/local/include
       /usr/include
       /sw/include # Fink
       /opt/local/include # DarwinPorts
       /opt/csw/include # Blastwave
       /opt/include
       /usr/freeware/include
    )

SET( FFMPEG_LIBRARY_SEARCH_DIR
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
    
SET (FFMPEG_SWSCALE_FOUND ON)
SET (SWRESAMPLE_FOUND ON)

#NOTE: avdevice won't be found and we don't need it but for some reason the first lib in this list is ignored so keep it
SET( FFMPEG_LIBRARIES avdevice avcodec avformat avutil swscale swresample)

FOREACH( LIB_ ${FFMPEG_LIBRARIES} )
    
  FIND_LIBRARY( TMP_ NAMES ${LIB_} PATHS ${FFMPEG_LIBRARY_SEARCH_DIR} )
  IF ( TMP_ )
    SET( FFMPEG_LIBRARIES_FULL ${FFMPEG_LIBRARIES_FULL} ${TMP_} )
  ENDIF ( TMP_ )
  SET( TMP_ TMP-NOTFOUND )
ENDFOREACH( LIB_ )
SET ( FFMPEG_LIBRARIES ${FFMPEG_LIBRARIES_FULL} )

# handle the QUIETLY and REQUIRED arguments and set TINYXML_FOUND to TRUE if
# all listed variables are TRUE
INCLUDE( "FindPackageHandleStandardArgs" )
FIND_PACKAGE_HANDLE_STANDARD_ARGS( "FFMPEG" DEFAULT_MSG FFMPEG_LIBRARIES)


