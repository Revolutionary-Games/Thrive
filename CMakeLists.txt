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

find_package(OGRE REQUIRED QUIET)

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
    date_time   # Required by OGRE
    thread      # Required by OGRE
    system      # Required by OGRE
)

find_package(Boost 1.51 COMPONENTS ${BOOST_COMPONENTS} REQUIRED QUIET)

######################
# Configure Compiler # 
######################

include_directories( 
    ${Boost_INCLUDE_DIRS}
    ${OIS_INCLUDE_DIRS}
	${OGRE_INCLUDE_DIRS}
)

# Compile using c++11
set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -std=gnu++0x")

# add subdirectories
# Collect sources from sub directories
add_subdirectory(src)
get_property(SOURCE_FILES GLOBAL PROPERTY SOURCE_FILES)


add_executable(Thrive WIN32 ${PREP_SRCS}) #${HDRS} ${SRCS})

target_link_libraries(Thrive 
    ${Boost_LIBRARIES}
    ${OGRE_LIBRARIES} 
    ${OIS_LIBRARIES}
)


###########
# Install #
###########

# Executable
install(TARGETS Thrive
    RUNTIME DESTINATION bin
)

# OGRE config

install(FILES 
    ${CMAKE_SOURCE_DIR}/res/dist/bin/resources.cfg
    DESTINATION bin
)

install(FILES 
    ${CMAKE_SOURCE_DIR}/res/dist/bin/plugins.cfg
    DESTINATION bin
    CONFIGURATIONS Release 
)

install(FILES 
    ${CMAKE_SOURCE_DIR}/res/dist/bin/plugins_d.cfg
    DESTINATION bin
    CONFIGURATIONS Debug
    RENAME plugins.cfg
)

install(DIRECTORY ${CMAKE_SOURCE_DIR}/res/dist/media
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
		${OGRE_PLUGIN_DIR_REL}/cg.dll
		DESTINATION bin
		CONFIGURATIONS Release
	)

	install(FILES 
        ${OGRE_PLUGIN_DIR_DBG}/OgreMain_d.dll
		${OGRE_PLUGIN_DIR_DBG}/RenderSystem_GL_d.dll
		${OGRE_PLUGIN_DIR_DBG}/OIS_d.dll
		${OGRE_PLUGIN_DIR_DBG}/cg.dll
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

