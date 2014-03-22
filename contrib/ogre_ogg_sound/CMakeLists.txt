PROJECT(OgreOggSound)

CMAKE_MINIMUM_REQUIRED(VERSION 2.6)

INCLUDE (CheckIncludeFileCXX)

# define header and source files for the library
SET (HEADER_FILES
	include/LocklessQueue.h
    include/OgreOggISound.h
    include/OgreOggListener.h
    include/OgreOggSoundCallback.h
    include/OgreOggSoundFactory.h
    include/OgreOggSound.h
    include/OgreOggSoundManager.h
    include/OgreOggSoundPlugin.h
    include/OgreOggSoundPrereqs.h
    include/OgreOggSoundRecord.h
    include/OgreOggStaticSound.h
    include/OgreOggStaticWavSound.h
    include/OgreOggStreamSound.h
    include/OgreOggStreamWavSound.h
)

SET (SOURCE_FILES
	src/OgreOggISound.cpp
    src/OgreOggListener.cpp
    src/OgreOggSoundFactory.cpp
    src/OgreOggSoundManager.cpp
    src/OgreOggSoundPlugin.cpp
    src/OgreOggSoundPluginDllStart.cpp
    src/OgreOggSoundRecord.cpp
    src/OgreOggStaticSound.cpp
    src/OgreOggStaticWavSound.cpp
    src/OgreOggStreamSound.cpp
    src/OgreOggStreamWavSound.cpp
)
  
# Option.
SET(OGGSOUND_THREADED YES CACHE BOOL "Enable multi-threaded streamed sounds")
SET(USE_POCO YES CACHE BOOL "Use POCO Threads?")

IF(CMAKE_BUILD_TYPE STREQUAL Debug)

    # Enable all compiler warnings and debug support.
    ADD_DEFINITIONS(-D_DEBUG -g -Wall)
	SET(CMAKE_DEBUG_POSTFIX "_d")
	
ENDIF(CMAKE_BUILD_TYPE STREQUAL Debug)

IF(OGGSOUND_THREADED)
	IF(USE_POCO)
		CHECK_INCLUDE_FILE_CXX(Poco/ScopedLock.h HAVE_POCO_H)
		IF(HAVE_POCO_H)
			ADD_DEFINITIONS(-DOGGSOUND_THREADED=1)
			ADD_DEFINITIONS(-DPOCO_THREAD=1)
		ENDIF()
	ELSE()
		SET(Boost_ADDITIONAL_VERSIONS "1.47" "1.44" "1.43" "1.42")
		FIND_PACKAGE(Boost 1.42.0 COMPONENTS thread)
		IF (Boost_FOUND)
			INCLUDE_DIRECTORIES(${Boost_INCLUDE_DIRS})
			LINK_DIRECTORIES(${Boost_LIBRARY_DIRS})
			LINK_LIBRARIES(${Boost_LIBRARIES})
			ADD_DEFINITIONS(-DOGGSOUND_THREADED=1)
		ENDIF()
	ENDIF()	 
ELSE()
    ADD_DEFINITIONS(-DOGGSOUND_THREADED=0)
ENDIF()

IF (APPLE)
   FIND_PACKAGE(OPENAL REQUIRED)
   FIND_LIBRARY(OGRE OGRE REQUIRED)
   FIND_LIBRARY(OGG Ogg REQUIRED)
   FIND_LIBRARY(VORBIS Vorbis REQUIRED)

   set(OGG_LIBRARIES ${OGG})
   set(VORBISFILE_LIBRARIES ${VORBIS})
   set(VORBISFILE_INCLUDE_DIRS ${VORBIS_INCLUDE_DIRS})   
   set(OPENAL_INCLUDE_DIRS ${OPENAL_INCLUDE_DIRS} ${OPENAL_INCLUDE_DIR})
   set(OPENAL_LIBRARIES ${OPENAL_LIBRARIES} ${OPENAL_LIBRARY})
ELSE (APPLE) 
	IF (UNIX)
		FIND_PACKAGE(PkgConfig REQUIRED)
		PKG_CHECK_MODULES(OGRE REQUIRED OGRE)
		PKG_CHECK_MODULES(OPENAL REQUIRED openal)
		PKG_CHECK_MODULES(OGG REQUIRED ogg)
		PKG_CHECK_MODULES(VORBISFILE REQUIRED vorbisfile)
	ENDIF()
ENDIF (APPLE)

CHECK_INCLUDE_FILE_CXX(efx.h HAVE_EFX)

INCLUDE_DIRECTORIES(include
                    ${OGRE_INCLUDE_DIRS}
                    ${OPENAL_INCLUDE_DIRS}
                    ${OGG_INCLUDE_DIRS}
                    ${VORBISFILE_INCLUDE_DIRS}
                    /usr/include/AL
                    /usr/local/include/AL)

LINK_DIRECTORIES(${OGRE_LIBRARY_DIRS}
                 ${OPENAL_LIBRARY_DIRS}
                 ${OGG_INCLUDE_DIRS}
                 ${VORBISFILE_INCLUDE_DIRS})

LINK_LIBRARIES(${OGRE_LIBRARIES}
               ${OPENAL_LIBRARIES}
               ${OGG_LIBRARIES}
               ${VORBISFILE_LIBRARIES})

ADD_LIBRARY(${PROJECT_NAME} SHARED ${SOURCE_FILES} ${HEADER_FILES})

INSTALL(FILES include/LocklessQueue.h
              include/OgreOggISound.h
              include/OgreOggListener.h
              include/OgreOggSoundCallback.h
              include/OgreOggSoundFactory.h
              include/OgreOggSound.h
              include/OgreOggSoundManager.h
              include/OgreOggSoundPlugin.h
              include/OgreOggSoundPrereqs.h
              include/OgreOggSoundRecord.h
              include/OgreOggStaticSound.h
              include/OgreOggStaticWavSound.h
              include/OgreOggStreamSound.h
              include/OgreOggStreamWavSound.h
        DESTINATION include/OgreOggSound)

INSTALL(TARGETS ${PROJECT_NAME} 
		LIBRARY DESTINATION lib${LIB_SUFFIX}
		ARCHIVE DESTINATION lib${LIB_SUFFIX} 
		RUNTIME DESTINATION bin)
