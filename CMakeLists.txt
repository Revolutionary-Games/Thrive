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
      "RelWithDebInfo" 
      CACHE STRING "Choose the type of build, options are: None (CMAKE_CXX_FLAGS or CMAKE_C_FLAGS used) Debug Release RelWithDebInfo MinSizeRel." 
      FORCE
    )
endif ()

set(CMAKE_INSTALL_PREFIX "${CMAKE_CURRENT_BINARY_DIR}/dist" CACHE STRING "Install path" FORCE)

set(RUNTIME_LIBRARIES ${SYSTEM_DLLS})


#############
# Find OGRE #
#############

find_package(OGRE REQUIRED)

if(CMAKE_BUILD_TYPE STREQUAL "Debug")
    set(OGRE_LIB_SUFFIX DBG)
else()
    set(OGRE_LIB_SUFFIX REL)
endif()

list(APPEND RUNTIME_LIBRARIES ${OGRE_BINARY_${OGRE_LIB_SUFFIX}})

# Gather OGRE plugin files
set(OGRE_COMPONENTS
    Plugin_BSPSceneManager
    Plugin_CgProgramManager
    Plugin_OctreeZone
    Plugin_OctreeSceneManager
    Plugin_ParticleFX
    Plugin_PCZSceneManager
    RenderSystem_GL
)

set(OGRE_PLUGIN_LIBS)
foreach(OGRE_COMPONENT ${OGRE_COMPONENTS})
    set(LIB_VAR OGRE_${OGRE_COMPONENT}_BINARY_${OGRE_LIB_SUFFIX})
    list(APPEND OGRE_PLUGIN_LIBS ${${LIB_VAR}})
endforeach()


#######
# OIS #
#######

find_package(OIS REQUIRED)

GetLibForCurrentBuildType("${OIS_LIBRARIES}" OIS_RUNTIME_LIB)
list(APPEND RUNTIME_LIBRARIES ${OIS_RUNTIME_LIB})

##############
# Find Boost #
##############

set(BOOST_COMPONENTS
    date_time   # Required by OGRE
    thread      # Required by OGRE
    system      # Required by OGRE
)

find_package(Boost 1.51 COMPONENTS ${BOOST_COMPONENTS} REQUIRED)

message("Boost: ${Boost_LIBRARIES}")

list(APPEND RUNTIME_LIBRARIES ${Boost_LIBRARIES})

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
    ${CMAKE_SOURCE_DIR}/res/dist/bin/plugins.cfg
    ${CMAKE_SOURCE_DIR}/res/dist/bin/resources.cfg
    DESTINATION bin
    CONFIGURATIONS Release RelWithDebInfo
)

install(FILES 
    ${CMAKE_SOURCE_DIR}/res/dist/bin/plugins_d.cfg
    DESTINATION bin
    CONFIGURATIONS Debug
    RENAME plugins.cfg
)

install(FILES 
    ${CMAKE_SOURCE_DIR}/res/dist/bin/resources_d.cfg
    DESTINATION bin
    CONFIGURATIONS Debug
    resources.cfg
)

install(DIRECTORY ${CMAKE_SOURCE_DIR}/res/dist/media
    DESTINATION ./
    CONFIGURATIONS Release RelWithDebInfo Debug
)


InstallRuntimeFiles("${RUNTIME_LIBRARIES}" bin FALSE)
InstallRuntimeFiles("${OGRE_PLUGIN_LIBS}" bin TRUE)





if(WIN32)
    # NOTE: for the 1.7.1 sdk the OIS dll is called OIS.dll instead of libOIS.dll
    # so you'll have to change that to make it work with 1.7.1
    install(FILES ${OGRE_PLUGIN_LIBS}
		${OGRE_PLUGIN_DIR_REL}/RenderSystem_GL.dll
		${OGRE_PLUGIN_DIR_REL}/OIS.dll
		${OGRE_PLUGIN_DIR_REL}/cg.dll
		DESTINATION bin
		CONFIGURATIONS Release RelWithDebInfo
	)

	install(FILES ${OGRE_PLUGIN_DIR_DBG}/OgreMain_d.dll
		${OGRE_PLUGIN_DIR_DBG}/RenderSystem_GL_d.dll
		${OGRE_PLUGIN_DIR_DBG}/OIS_d.dll
		${OGRE_PLUGIN_DIR_DBG}/cg.dll
		DESTINATION bin
		CONFIGURATIONS Debug
	)

    # Install plugins
    install(FILES ${OGRE_PLUGIN_DIR_REL}/Plugin_BSPSceneManager.dll
        ${OGRE_PLUGIN_DIR_REL}/Plugin_CgProgramManager.dll
        ${OGRE_PLUGIN_DIR_REL}/Plugin_OctreeSceneManager.dll
        ${OGRE_PLUGIN_DIR_REL}/Plugin_OctreeZone.dll
        ${OGRE_PLUGIN_DIR_REL}/Plugin_ParticleFX.dll
        ${OGRE_PLUGIN_DIR_REL}/Plugin_PCZSceneManager.dll
        ${OGRE_PLUGIN_DIR_REL}/RenderSystem_GL.dll
        DESTINATION bin/plugins
        CONFIGURATIONS Release RelWithDebInfo
    )

    install(FILES ${OGRE_PLUGIN_DIR_DBG}/Plugin_BSPSceneManager_d.dll
        ${OGRE_PLUGIN_DIR_DBG}/Plugin_CgProgramManager_d.dll
        ${OGRE_PLUGIN_DIR_DBG}/Plugin_OctreeSceneManager_d.dll
        ${OGRE_PLUGIN_DIR_DBG}/Plugin_OctreeZone_d.dll
        ${OGRE_PLUGIN_DIR_DBG}/Plugin_ParticleFX_d.dll
        ${OGRE_PLUGIN_DIR_DBG}/Plugin_PCZSceneManager_d.dll
        ${OGRE_PLUGIN_DIR_DBG}/RenderSystem_GL_d.dll
        DESTINATION bin/plugins
        CONFIGURATIONS Debug
    )

endif(WIN32)

if(UNIX)

	install(FILES ${CMAKE_SOURCE_DIR}/res/dist/bin/plugins.cfg
		${CMAKE_SOURCE_DIR}/res/dist/bin/resources.cfg
		DESTINATION bin
		CONFIGURATIONS Release RelWithDebInfo Debug
	)

endif(UNIX)

message("Runtime libs: ${RUNTIME_LIBRARIES}")
