# Based on a file from the Ogre Wiki Tutorial Framework
#      http://www.ogre3d.org/tikiwiki/
#
# Modified as part of the Thrive project
#-----------------------------------------------------------------------------

cmake_minimum_required(VERSION 2.6)

project(Thrive)

###################
# Cache Variables #
###################

# Ogre SDK
SET(OGRE_SDK ""
    CACHE STRING "Path to the Ogre SDK"
)

set(ASSET_DIRECTORY ${CMAKE_SOURCE_DIR}/assets
    CACHE PATH "Path to assets"
)

if(NOT IS_DIRECTORY ${ASSET_DIRECTORY}/models)
    message(FATAL_ERROR 
"Could not find assets in ${ASSET_DIRECTORY}.  
Please use Subversion to download the assets:
        svn co http://crovea.net/svn/thrive_assets/ ${ASSET_DIRECTORY}
Use 'thrive' for the username and password
"
    )
endif()


###############
# CMake Setup #
###############

# Configure search path for cmake modules
list(APPEND CMAKE_MODULE_PATH
    ${CMAKE_SOURCE_DIR}/cmake_modules
)

# Import utility functions
include(add_to_project)
include(utils)

# Search path for dynamic libraries at runtime. Only relevant for Linux.
set(CMAKE_INSTALL_RPATH ".")

# Assure a proper build type
if (NOT CMAKE_BUILD_TYPE)
  set(CMAKE_BUILD_TYPE
      "Debug"
      CACHE STRING "Choose the type of build, options are: None (CMAKE_CXX_FLAGS or CMAKE_C_FLAGS used) Debug Release."
      FORCE
    )
endif ()

# Install into dist subdirectory
set(CMAKE_INSTALL_PREFIX
    "${CMAKE_CURRENT_BINARY_DIR}/dist"
    CACHE STRING "Install path" FORCE
)


#############
# Find OGRE #
#############

find_package(OGRE REQUIRED)

# OGRE Plugins used
set(OGRE_PLUGINS
    Plugin_BSPSceneManager
    Plugin_OctreeSceneManager
    Plugin_OctreeZone
    Plugin_ParticleFX
    Plugin_PCZSceneManager
    RenderSystem_GL
)

#######
# OIS #
#######

find_package(OIS REQUIRED QUIET)


##############
# Find Boost #
##############

set(BOOST_COMPONENTS
    chrono
    date_time
    filesystem
    thread
    system
)

find_package(Boost COMPONENTS ${BOOST_COMPONENTS} REQUIRED QUIET)

###############
# Google Test #
###############

add_subdirectory(
    ${CMAKE_CURRENT_SOURCE_DIR}/contrib/googletest
)

include_directories(
    SYSTEM
    ${CMAKE_CURRENT_SOURCE_DIR}/contrib/googletest/include
)

##########
# Bullet #
##########

find_package(Bullet REQUIRED QUIET)

include_directories(SYSTEM ${BULLET_INCLUDE_DIRS})

##########
# TinyXML #
##########

find_package(TinyXML REQUIRED QUIET)

#######
# Lua #
#######

include_directories(
    SYSTEM
    ${CMAKE_CURRENT_SOURCE_DIR}/contrib/lua/lua/src
    ${CMAKE_CURRENT_SOURCE_DIR}/contrib/luabind/
)

#######
# CEGUI #
#######

find_package(CEGUI REQUIRED QUIET COMPONENTS OGRE)

include_directories(SYSTEM ${CEGUI_INCLUDE_DIRS})


if(WIN32)
    add_definitions(-DLUA_BUILD_AS_DLL)
endif()

add_subdirectory(
    ${CMAKE_CURRENT_SOURCE_DIR}/contrib/lua/
)

set(LUA_FOUND TRUE)
set(LUA_INCLUDE_DIRS ${CMAKE_CURRENT_SOURCE_DIR}/contrib/lua/lua/src)
set(LUA_LIBRARIES lua)
#set(BUILD_SHARED_LUABIND ON)
#set(INSTALL_LUABIND ON)
set(LIB_DIR bin)
add_definitions(-DLUABIND_CPLUSPLUS_LUA)

add_subdirectory(
    ${CMAKE_CURRENT_SOURCE_DIR}/contrib/luabind/
)

################
# OgreOggSound #
################

find_package(OpenAL REQUIRED QUIET)
find_package(Ogg REQUIRED)
find_package(Vorbis REQUIRED)

add_definitions(-DHAVE_EFX=0)

add_subdirectory(
    ${CMAKE_CURRENT_SOURCE_DIR}/contrib/ogre_ogg_sound
)

include_directories(
    SYSTEM
    ${CMAKE_CURRENT_SOURCE_DIR}/contrib/ogre_ogg_sound/include
    ${OpenAL_INCLUDE_DIRS}
    ${OGG_INCLUDE_DIRS}
    ${VORBIS_INCLUDE_DIRS}
)

######################
# Configure Compiler #
######################

include_directories(SYSTEM
    ${Boost_INCLUDE_DIRS}
    ${OIS_INCLUDE_DIRS}
    ${OGRE_INCLUDE_DIRS}
)

include_directories(
    ${CMAKE_CURRENT_SOURCE_DIR}/src
)

# Compile using c++11
set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -std=gnu++0x")


find_package(Threads)

##################
# Compile Thrive #
##################

# Collect sources from sub directories
add_subdirectory(src)
get_property(SOURCE_FILES GLOBAL PROPERTY SOURCE_FILES)

# Set extensive warning flags
include(compiler_flags)
set_source_files_properties(
    ${SOURCE_FILES}
    PROPERTIES COMPILE_FLAGS ${WARNING_FLAGS}
)

# Compile library
add_library(ThriveLib STATIC ${SOURCE_FILES})

target_link_libraries(ThriveLib
    ${Boost_LIBRARIES}
    ${OGRE_LIBRARIES}
    ${OIS_LIBRARIES}
    ${CMAKE_THREAD_LIBS_INIT}
    ${BULLET_DYNAMICS_LIBRARY}
    ${BULLET_COLLISION_LIBRARY}
    ${BULLET_MATH_LIBRARY}
    ${BULLET_SOFTBODY_LIBRARY}
    ${CEGUI_LIBRARY}
    ${TINYXML_LIBRARIES}
    ${OpenAL_LIBRARIES}
    ${Ogg_LIBRARIES}
    ${Vorbis_LIBRARIES}
    OgreOggSound
    luabind
)


set_target_properties(ThriveLib PROPERTIES
    OUTPUT_NAME Thrive
)

# Compile executable
add_executable(Thrive
#WIN32
 ${CMAKE_CURRENT_SOURCE_DIR}/src/Main.cpp)

target_link_libraries(Thrive ThriveLib)

#################
# Compile tests #
#################

# Collect sources from sub directories
get_property(TEST_SOURCE_FILES GLOBAL PROPERTY TEST_SOURCE_FILES)

set_source_files_properties(
    ${TEST_SOURCE_FILES}
    PROPERTIES COMPILE_FLAGS ${WARNING_FLAGS}
)

add_executable(RunTests ${TEST_SOURCE_FILES})
target_link_libraries(RunTests ThriveLib gtest_main)

#################
# Documentation #
#################

set(DOC_OUTPUT_DIR ${CMAKE_CURRENT_BINARY_DIR}/doc CACHE PATH "Documentation output directory.")
set(DOC_INPUT "${CMAKE_CURRENT_SOURCE_DIR}/src ${CMAKE_CURRENT_SOURCE_DIR}/doc")

set( DOXYGEN_CONFIG_FILE
    ${CMAKE_CURRENT_SOURCE_DIR}/doc/doxygen.cfg.in
)

find_package(Doxygen)

if(DOXYGEN_FOUND)
    configure_file(${DOXYGEN_CONFIG_FILE} ${CMAKE_CURRENT_BINARY_DIR}/doxygen.cfg @ONLY)
    add_custom_target(doc
        ${DOXYGEN_EXECUTABLE} ${CMAKE_CURRENT_BINARY_DIR}/doxygen.cfg
        WORKING_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}/doc
        COMMENT "Generating API documentation with Doxygen" VERBATIM
        SOURCES ${DOXYGEN_CONFIG_FILE}
    )
endif()


###########
# Install #
###########

# Executable
install(TARGETS Thrive
    RUNTIME DESTINATION bin
    LIBRARY DESTINATION bin
)

install(EXPORT lua
    DESTINATION bin
)

# OGRE config and media

install(FILES
    ${CMAKE_SOURCE_DIR}/ogre_cfg/resources.cfg
    DESTINATION bin
)

install(FILES
    ${CMAKE_SOURCE_DIR}/ogre_cfg/plugins.cfg
    DESTINATION bin
    CONFIGURATIONS Release
)

install(FILES
    ${CMAKE_SOURCE_DIR}/ogre_cfg/plugins_d.cfg
    DESTINATION bin
    CONFIGURATIONS Debug
    RENAME plugins.cfg
)

install(DIRECTORY ${ASSET_DIRECTORY}/models
    DESTINATION ./
    CONFIGURATIONS Release  Debug
    FILES_MATCHING
        PATTERN "*.mesh" 
        PATTERN "*.mesh.xml"
        PATTERN "*.skeleton" 
        PATTERN "*.skeleton.xml"
)

install(DIRECTORY ${ASSET_DIRECTORY}/materials
    DESTINATION ./
    CONFIGURATIONS Release  Debug
    FILES_MATCHING
        PATTERN "*.jpg"
        PATTERN "*.jpeg"
        PATTERN "*.material"
        PATTERN "*.png"
)

install(DIRECTORY
    ${ASSET_DIRECTORY}/fonts
    DESTINATION ./
    CONFIGURATIONS Release  Debug
    FILES_MATCHING
        PATTERN "*.ttf"
        PATTERN "*.fontdef"
        PATTERN "*.font"
)

install(DIRECTORY
    ${CMAKE_SOURCE_DIR}/scripts
    DESTINATION ./
    CONFIGURATIONS Release  Debug
)
install(DIRECTORY
    ${ASSET_DIRECTORY}/gui/animations
    DESTINATION ./gui/
    CONFIGURATIONS Release  Debug
    FILES_MATCHING
        PATTERN "*.anims"
)
install(DIRECTORY
    ${ASSET_DIRECTORY}/gui/imagesets
    DESTINATION ./gui/
    CONFIGURATIONS Release  Debug
    FILES_MATCHING
        PATTERN "*.imageset"
        PATTERN "*.jpg"
        PATTERN "*.png"
)
install(DIRECTORY
    ${ASSET_DIRECTORY}/gui/layouts
    DESTINATION ./gui/
    CONFIGURATIONS Release  Debug
    FILES_MATCHING
        PATTERN "*.layout"
        PATTERN "*.wnd"
)
install(DIRECTORY
    ${ASSET_DIRECTORY}/gui/looknfeel
    DESTINATION ./gui/
    CONFIGURATIONS Release  Debug
    FILES_MATCHING
        PATTERN "*.looknfeel"
)
install(DIRECTORY
    ${ASSET_DIRECTORY}/gui/schemes
    DESTINATION ./gui/
    CONFIGURATIONS Release  Debug
    FILES_MATCHING
        PATTERN "*.scheme"
)

install(DIRECTORY
    ${ASSET_DIRECTORY}/sounds
    DESTINATION ./
    CONFIGURATIONS Release  Debug
    FILES_MATCHING
        PATTERN "*.ogg"
        PATTERN "*.wav"
)

# Install Runtime Libraries
if(WIN32)

    if (SYSTEM_DLLS)
        INSTALL(FILES
            ${SYSTEM_DLLS}
            DESTINATION bin
        )
    endif()

    INSTALL(FILES
        ${Boost_LIBRARIES}
        DESTINATION bin
    )

#    INSTALL(FILES
#        ${IRRKLANG_LIBRARIES_DLL}
#        DESTINATION bin
#    )

    foreach(OGRE_PLUGIN ${OGRE_PLUGINS})
        # Release
        install(FILES
            ${OGRE_PLUGIN_DIR_REL}/${OGRE_PLUGIN}.dll
            DESTINATION bin
            CONFIGURATIONS Release
        )
        # Debug
        install(FILES
            ${OGRE_PLUGIN_DIR_DBG}/${OGRE_PLUGIN}_d.dll
            DESTINATION bin
            CONFIGURATIONS Debug
        )
    endforeach()

    install(FILES
        ${OGRE_PLUGIN_DIR_REL}/OgreMain.dll
		${OGRE_PLUGIN_DIR_REL}/RenderSystem_GL.dll
		${OGRE_PLUGIN_DIR_REL}/OIS.dll
        #${OGRE_PLUGIN_DIR_REL}/cg.dll
		DESTINATION bin
		CONFIGURATIONS Release
	)
    
	install(FILES
        ${OGRE_PLUGIN_DIR_DBG}/OgreMain_d.dll
		${OGRE_PLUGIN_DIR_DBG}/RenderSystem_GL_d.dll
		${OGRE_PLUGIN_DIR_DBG}/OIS_d.dll
        #${OGRE_PLUGIN_DIR_DBG}/cg.dll
		DESTINATION bin
		CONFIGURATIONS Debug
	)
    
    install(FILES
        ${MINGW_ENV}/install/bin/libCEGUIBase-0_d.dll
        ${MINGW_ENV}/install/bin/libCEGUIOgreRenderer-0_d.dll
        ${MINGW_ENV}/install/bin/libCEGUITinyXMLParser_d.dll
        ${MINGW_ENV}/install/bin/libCEGUICoreWindowRendererSet_d.dll
        ${MINGW_ENV}/install/bin/Debug/libfreetype_d.dll
        ${MINGW_ENV}/install/bin/Debug/libjpeg_d.dll
        ${MINGW_ENV}/install/bin/Debug/liblibpng_d.dll
        ${MINGW_ENV}/install/bin/Debug/libSILLY_d.dll
        ${MINGW_ENV}/install/bin/Debug/libzlib_d.dll
        ${MINGW_ENV}/install/bin/OpenAL32.dll
        DESTINATION bin
		CONFIGURATIONS Debug
    )
    
    install(FILES
        ${MINGW_ENV}/install/bin/libCEGUIBase-0.dll
        ${MINGW_ENV}/install/bin/libCEGUIOgreRenderer-0.dll
        ${MINGW_ENV}/install/bin/libCEGUITinyXMLParser.dll
        ${MINGW_ENV}/install/bin/libCEGUICoreWindowRendererSet.dll
        ${MINGW_ENV}/install/bin/Release/libfreetype.dll
        ${MINGW_ENV}/install/bin/Release/libjpeg.dll
        ${MINGW_ENV}/install/bin/Release/liblibpng.dll
        ${MINGW_ENV}/install/bin/Release/libSILLY.dll
        ${MINGW_ENV}/install/bin/Release/libzlib.dll
        ${MINGW_ENV}/install/bin/OpenAL32.dll
        DESTINATION bin
		CONFIGURATIONS Release
    )

elseif(UNIX)

    SeparateLibrariesByBuildType(
        "${OGRE_LIBRARIES}"
        OGRE_MAIN_DBG
        OGRE_MAIN_REL
    )
    InstallFollowingSymlink(
        ${OGRE_MAIN_REL}
        bin
        Release
        False
    )
    InstallFollowingSymlink(
        ${OGRE_MAIN_DBG}
        bin
        Debug
        False
    )

    foreach(OGRE_PLUGIN ${OGRE_PLUGINS})
        SeparateLibrariesByBuildType(
            "${OGRE_${OGRE_PLUGIN}_LIBRARIES}"
            OGRE_PLUGIN_LIB_DBG
            OGRE_PLUGIN_LIB_REL
        )
        # Release
        InstallFollowingSymlink(
            ${OGRE_PLUGIN_LIB_REL}
            bin
            Release
            True
        )
        # Debug
        InstallFollowingSymlink(
            ${OGRE_PLUGIN_LIB_DBG}
            bin
            Debug
            True
        )
    endforeach()

endif()

###############################################################################
# CPack
###############################################################################

configure_file(${CMAKE_SOURCE_DIR}/cpack/zip.cmake.in
  "${CMAKE_CURRENT_BINARY_DIR}/cpack/zip.cmake" @ONLY
)

add_custom_target(zip
    COMMAND cpack --config "${CMAKE_CURRENT_BINARY_DIR}/cpack/zip.cmake"
    DEPENDS Thrive
)

