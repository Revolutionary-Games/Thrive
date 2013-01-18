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


if(WIN32)
	set(CMAKE_MODULE_PATH "$ENV{OGRE_HOME}/CMake/;${CMAKE_MODULE_PATH}")
	set(OGRE_SAMPLES_INCLUDEPATH
		$ENV{OGRE_HOME}/Samples/include
	)
endif(WIN32)

if(UNIX)
	if(EXISTS "/usr/local/lib/OGRE/cmake")

	  set(CMAKE_MODULE_PATH "/usr/local/lib/OGRE/cmake/;${CMAKE_MODULE_PATH}")
	  set(OGRE_SAMPLES_INCLUDEPATH "/usr/local/share/OGRE/samples/Common/include/") # We could just *assume* that developers uses this basepath : /usr/local

	elseif(EXISTS "/usr/lib/OGRE/cmake")

	  set(CMAKE_MODULE_PATH "/usr/lib/OGRE/cmake/;${CMAKE_MODULE_PATH}")
	  set(OGRE_SAMPLES_INCLUDEPATH "/usr/share/OGRE/samples/Common/include/") # Otherwise, this one

	else ()
	  message(SEND_ERROR "Failed to find module path.")
	endif(EXISTS "/usr/local/lib/OGRE")
endif(UNIX)

if (CMAKE_BUILD_TYPE STREQUAL "")
  # CMake defaults to leaving CMAKE_BUILD_TYPE empty. This screws up
  # differentiation between debug and release builds.
  set(CMAKE_BUILD_TYPE "RelWithDebInfo" CACHE STRING "Choose the type of build, options are: None (CMAKE_CXX_FLAGS or CMAKE_C_FLAGS used) Debug Release RelWithDebInfo MinSizeRel." FORCE)
endif ()

set(CMAKE_DEBUG_POSTFIX "_d")

set(CMAKE_INSTALL_PREFIX "${CMAKE_CURRENT_BINARY_DIR}/dist")

find_package(OGRE REQUIRED)

find_package(OIS REQUIRED)

if(NOT OIS_FOUND)
	message(SEND_ERROR "Failed to find OIS.")
endif()

##############
# Find Boost #
##############
set(Boost_Dir $ENV{BOOST_ROOT})
if (NOT OGRE_BUILD_PLATFORM_IPHONE)
	if (WIN32 OR APPLE)
		set(Boost_USE_STATIC_LIBS TRUE)
	else ()
		# Statically linking boost to a dynamic Ogre build doesn't work on Linux 64bit
		set(Boost_USE_STATIC_LIBS ${OGRE_STATIC})
	endif ()
	if (MINGW)
		# this is probably a bug in CMake: the boost find module tries to look for
		# boost libraries with name libboost_*, but CMake already prefixes library
		# search names with "lib". This is the workaround.
		set(CMAKE_FIND_LIBRARY_PREFIXES ${CMAKE_FIND_LIBRARY_PREFIXES} "")
	endif ()

	set(Boost_ADDITIONAL_VERSIONS "1.44" "1.44.0" "1.42" "1.42.0" "1.41.0" "1.41" "1.40.0" "1.40" "1.39.0" "1.39" "1.38.0" "1.38" "1.37.0" "1.37" )
	# Components that need linking (NB does not include header-only components like bind)
	set(OGRE_BOOST_COMPONENTS thread date_time system)
	find_package(Boost 1.51 COMPONENTS ${OGRE_BOOST_COMPONENTS} REQUIRED)

	if (NOT Boost_FOUND)
		# Try again with the other type of libs
		set(Boost_USE_STATIC_LIBS NOT ${Boost_USE_STATIC_LIBS})
		find_package(Boost COMPONENTS ${OGRE_BOOST_COMPONENTS} REQUIRED)
			message("${Boost_FOUND}, ${Boost_DIR}")

	endif()
	find_package(Boost REQUIRED)

	# Set up referencing of Boost
	include_directories(${Boost_INCLUDE_DIR})
	add_definitions(-DBOOST_ALL_NO_LIB)
	set(OGRE_LIBRARIES ${OGRE_LIBRARIES} ${Boost_LIBRARIES})
endif()

################################
# Load additional dependencies #
################################

set(CMAKE_MODULE_PATH "${CMAKE_SOURCE_DIR}/cmake_modules")

### CEGUI
#set(CEGUI_COMPONENTS Base Ogre)
#message("${CEGUIDIR}")
#Find_Package(CEGUI REQUIRED ${CEGUI_COMPONENTS} OGRE)
#set(OGRE_LIRARIES ${OGRE_LIBRARIES} ${CEGUI_LIBRARY})

### MyGUI
## Removed for now - double commented lines were already commented
#set(MYGUI_COMPONENTS Common MyGUIEngine MyGUI.OgrePlatform)
## add_definitions(-DMYGUI_STATIC)
## set(MYGUI_STATIC TRUE)
#set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -DMYGUI_STATIC -DMYGUI_OGRE_PLATFORM")
## set(MYGUI_USE_STATIC_LIBS TRUE)
#find_package(MYGUI REQUIRED ${MYGUI_COMPONENTS})
#find_library(MYGUI_LIBRARIES_FREETYPE
#	NAMES freetype
#	HINTS "${OGRE_MEDIA_DIR}/../Dependencies/lib"
#	PATH_SUFFIXES "" release debug)
#find_package(Freetype REQUIRED)
#add_dependencies(${PROJECTNAME} MyGUIEngine Common MyGUI.OgrePlatform)
## target_link_libraries(${PROJECTNAME} MyGUIEngine Common MyGUI.OgrePlatform)
#include_directories(${MYGUI_INCLUDE_DIRS} ${MYGUI_PLATFORM_INCLUDE_DIRS})
#set(OGRE_LIBRARIES ${OGRE_LIBRARIES} ${MYGUI_LIBRARIES_FREETYPE} ${MYGUI_LIBRARIES} ${MYGUI_PLATFORM_LIBRARIES} )
	
################
# Finish setup # 
################

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

include_directories( ${OIS_INCLUDE_DIRS}
	${OGRE_INCLUDE_DIRS}
	${OGRE_SAMPLES_INCLUDEPATH}
	)

add_executable(Thrive WIN32 ${PREP_SRCS}) #${HDRS} ${SRCS})

set_target_properties(Thrive PROPERTIES DEBUG_POSTFIX _d)

target_link_libraries(Thrive ${OGRE_LIBRARIES} ${OIS_LIBRARIES})

file(MAKE_DIRECTORY ${CMAKE_CURRENT_BINARY_DIR}/dist/bin)
file(MAKE_DIRECTORY ${CMAKE_CURRENT_BINARY_DIR}/dist/media)

# post-build copy for win32
if(WIN32 AND NOT MINGW)
	add_custom_command( TARGET Thrive PRE_BUILD
		COMMAND if not exist .\\dist\\bin mkdir .\\dist\\bin )
	add_custom_command( TARGET Thrive POST_BUILD
		COMMAND copy \"$(TargetPath)\" .\\dist\\bin )
endif(WIN32 AND NOT MINGW)

if(MINGW OR UNIX)
	set(EXECUTABLE_OUTPUT_PATH ${PROJECT_BINARY_DIR}/dist/bin)
endif(MINGW OR UNIX)

if(WIN32)

	install(TARGETS Thrive
		RUNTIME DESTINATION bin
		CONFIGURATIONS All)

	install(DIRECTORY ${CMAKE_SOURCE_DIR}/res/dist/Media
		DESTINATION ./
		CONFIGURATIONS Release RelWithDebInfo Debug
	)

	install(FILES ${CMAKE_SOURCE_DIR}/res/dist/bin/plugins.cfg
		${CMAKE_SOURCE_DIR}/res/dist/bin/resources.cfg
		DESTINATION bin
		CONFIGURATIONS Release RelWithDebInfo
	)

	install(FILES ${CMAKE_SOURCE_DIR}/res/dist/bin/plugins_d.cfg
		${CMAKE_SOURCE_DIR}/res/dist/bin/resources_d.cfg
		DESTINATION bin
		CONFIGURATIONS Debug
	)

        # NOTE: for the 1.7.1 sdk the OIS dll is called OIS.dll instead of libOIS.dll
        # so you'll have to change that to make it work with 1.7.1
	install(FILES ${OGRE_PLUGIN_DIR_REL}/OgreMain.dll
		# ${OGRE_PLUGIN_DIR_REL}/RenderSystem_Direct3D9.dll
		${OGRE_PLUGIN_DIR_REL}/RenderSystem_GL.dll
		${OGRE_PLUGIN_DIR_REL}/OIS.dll
		DESTINATION bin
		CONFIGURATIONS Release RelWithDebInfo
	)

	install(FILES ${OGRE_PLUGIN_DIR_DBG}/OgreMain_d.dll
		# ${OGRE_PLUGIN_DIR_DBG}/RenderSystem_Direct3D9_d.dll
		${OGRE_PLUGIN_DIR_DBG}/RenderSystem_GL_d.dll
		${OGRE_PLUGIN_DIR_DBG}/OIS_d.dll
		DESTINATION bin
		CONFIGURATIONS Debug
	)

   # as of sdk 1.7.2 we need to copy the boost dll's as well
   # because they're not linked statically (it worked with 1.7.1 though)
   # install(FILES ${Boost_DATE_TIME_LIBRARY_RELEASE}
      # ${Boost_THREAD_LIBRARY_RELEASE}
      # DESTINATION bin
      # CONFIGURATIONS Release RelWithDebInfo
   # )

   # install(FILES ${Boost_DATE_TIME_LIBRARY_DEBUG}
      # ${Boost_THREAD_LIBRARY_DEBUG}
      # DESTINATION bin
      # CONFIGURATIONS Debug
   # )
endif(WIN32)

if(UNIX)

	install(TARGETS Thrive
		RUNTIME DESTINATION bin
		CONFIGURATIONS All)

	install(DIRECTORY ${CMAKE_SOURCE_DIR}/res/dist/media
		DESTINATION ./
		CONFIGURATIONS Release RelWithDebInfo Debug
	)

	install(FILES ${CMAKE_SOURCE_DIR}/res/dist/bin/plugins.cfg
		${CMAKE_SOURCE_DIR}/res/dist/bin/resources.cfg
		DESTINATION bin
		CONFIGURATIONS Release RelWithDebInfo Debug
	)

endif(UNIX)

