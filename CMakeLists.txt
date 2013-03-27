# Based on a file from the Ogre Wiki Tutorial Framework
#      http://www.ogre3d.org/tikiwiki/
#
# Modified as part of the Thrive project
#-----------------------------------------------------------------------------

cmake_minimum_required(VERSION 2.6)

project(Thrive)

####################
# Define functions #
####################

# function to collect all the sources from sub-directories into a single list
function(add_sources)
  get_property(is_defined GLOBAL PROPERTY SRCS_LIST DEFINED)
  if(NOT is_defined)
    define_property(GLOBAL PROPERTY SRCS_LIST
      BRIEF_DOCS "List of source files"
      FULL_DOCS "List of source files to be compiled in one library")
  endif()
  # make absolute paths
  set(SRCS)
  foreach(s IN LISTS ARGN)
    if(NOT IS_ABSOLUTE "${s}")
      get_filename_component(s "${s}" ABSOLUTE)
    endif()
    list(APPEND SRCS "${s}")
  endforeach()
  # append to global list
  set_property(GLOBAL APPEND PROPERTY SRCS_LIST "${SRCS}")
endfunction(add_sources)

###############
# CMake Setup #
###############

list(APPEND CMAKE_MODULE_PATH
    ${CMAKE_SOURCE_DIR}/cmake_modules
)

include(GetPrerequisites)
include(utils)
set(CMAKE_INSTALL_RPATH ".")
# Assure a proper build type
if (NOT CMAKE_BUILD_TYPE)
  set(CMAKE_BUILD_TYPE
      "Debug" 
      CACHE STRING "Choose the type of build, options are: None (CMAKE_CXX_FLAGS or CMAKE_C_FLAGS used) Debug Release." 
      FORCE
    )
endif ()

if (CMAKE_BUILD_TYPE STREQUAL "Debug")
    set(IS_DEBUG_BUILD True)
else()
    set(IS_DEBUG_BUILD False)
endif()

set(CMAKE_INSTALL_PREFIX "${CMAKE_CURRENT_BINARY_DIR}/dist" CACHE STRING "Install path" FORCE)

set(RUNTIME_LIBRARIES ${SYSTEM_DLLS})


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
add_subdirectory(src)

# Collect sources from sub directories
set(PREP_SRCS)
get_property(SRCS GLOBAL PROPERTY SRCS_LIST)
foreach(s IN LISTS SRCS)
  file(RELATIVE_PATH rs "${CMAKE_CURRENT_SOURCE_DIR}" "${s}")
  string(REGEX REPLACE "r$" "" o "${CMAKE_CURRENT_BINARY_DIR}/${rs}")
  add_custom_command(
    OUTPUT "${o}"
    COMMAND ${CMAKE_COMMAND} -E copy "${s}" "${o}"
    DEPENDS "${s}"
    COMMENT "Creating ${o}"
    VERBATIM
    )
  list(APPEND PREP_SRCS "${o}")
endforeach()

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

