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
    message(WARNING 
        "Could not find assets in ${ASSET_DIRECTORY}. 
        Please use Subversion to download the assets:
        svn co 91.250.119.121/scm/svn/thrive_assets/trunk ${ASSET_DIRECTORY}
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

############
# irrKlang #
############

#No longer used

#find_package(irrKlang REQUIRED QUIET)

#include_directories(SYSTEM ${IRRKLANG_INCLUDE_DIRS})

##########
# OpenAL #
##########

find_package(OpenAL REQUIRED QUIET)

include_directories(SYSTEM ${IRRKLANG_INCLUDE_DIR})


#######
# Lua #
#######

include_directories(
    SYSTEM
    ${CMAKE_CURRENT_SOURCE_DIR}/contrib/lua/lua/src
    ${CMAKE_CURRENT_SOURCE_DIR}/contrib/luabind/
)

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
    ${IRRKLANG_LIBRARIES}
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
    ${CMAKE_SOURCE_DIR}/res/ogre_cfg/resources.cfg
    DESTINATION bin
)

install(FILES
    ${CMAKE_SOURCE_DIR}/res/ogre_cfg/plugins.cfg
    DESTINATION bin
    CONFIGURATIONS Release
)

install(FILES
    ${CMAKE_SOURCE_DIR}/res/ogre_cfg/plugins_d.cfg
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
)

install(DIRECTORY
    ${CMAKE_SOURCE_DIR}/scripts
    DESTINATION ./
    CONFIGURATIONS Release  Debug
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
            DESTINATION bin/plugins
            CONFIGURATIONS Release
        )
        # Debug
        install(FILES
            ${OGRE_PLUGIN_DIR_DBG}/${OGRE_PLUGIN}_d.dll
            DESTINATION bin/plugins
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
            bin/plugins
            Release
            True
        )
        # Debug
        InstallFollowingSymlink(
            ${OGRE_PLUGIN_LIB_DBG}
            bin/plugins
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

