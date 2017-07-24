# Based on a file from the Ogre Wiki Tutorial Framework
#      http://www.ogre3d.org/tikiwiki/
#
# Modified as part of the Thrive project
# As of the Leviathan engine update this has been mostly rewritten
#-----------------------------------------------------------------------------

cmake_minimum_required(VERSION 3.0)

project(Thrive)



###################
# Cache Variables #
###################

set(ASSET_DIRECTORY "${CMAKE_SOURCE_DIR}/assets"
  CACHE PATH "Path to assets"
  )

# This isn't really meant to be modified
set(LEVIATHAN_SRC "${PROJECT_SOURCE_DIR}/ThirdParty/leviathan" CACHE FILEPATH
  "Path to leviathan source folder")

if(NOT IS_DIRECTORY "${ASSET_DIRECTORY}/models")
    message(FATAL_ERROR 
"Could not find assets in ${ASSET_DIRECTORY}.  
Please run the SetupThrive.rb script to download them.
Use 'thrive' for the username and password
"
    )
endif()


###########
# Options #
###########

option(USE_BREAKPAD "For enabling breakpad crash handling, set off for local debugging" OFF)
option(COPY_BOOST_TO_PACKAGE "If on copies all boost libraries to package" ON)
option(STRIP_SYMBOLS_ON_INSTALL "For stripping debug symbols on install" ON)


###########################
# Leviathan Project Setup #
###########################

# Set up cmake modules
set(CMAKE_MODULE_PATH ${CMAKE_MODULE_PATH} "${PROJECT_SOURCE_DIR}/CMake"
  "${LEVIATHAN_SRC}/CMake"
  "${LEVIATHAN_SRC}/LeviathanCMake")

include(LeviathanCompileOptions)
include(LeviathanSetRPath)

# Find required libraries
set(USE_ANGELSCRIPT ON)
set(USE_BOOST ON)
set(USE_OGRE ON)
set(USE_NEWTON ON)
set(USE_SFML ON)
set(USE_SDL2 ON)

include(LeviathanFindLibraries)

include(LeviathanUtility)

# Leviathan Required settings
set(CMAKE_INSTALL_PREFIX "./Install" CACHE FILEPATH "Install path")

DefinePreprocessorMacro(USE_BREAKPAD LEVIATHAN_USING_BREAKPAD)

include(LeviathanDefaultFileCopy)
include(LeviathanSetRPath)

# -Wl,-rpath-link is used to suppress warnings from games linking gainst libEngine.so
if(UNIX)
  set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -Wl,-rpath-link,${CMAKE_BINARY_DIR}/bin/lib")
endif()

# Links to libs

# Copy engine lib
if(WIN32)
  file(COPY "${LEVIATHAN_SRC}/build/bin/libEngine.dll" DESTINATION "${CMAKE_BINARY_DIR}/bin")
else()
  file(COPY "${LEVIATHAN_SRC}/build/bin/libEngine.so" DESTINATION "${CMAKE_BINARY_DIR}/bin")
endif()

link_directories("${CMAKE_BINARY_DIR}/bin")

# Version
set(PROGRAM_VERSION_STABLE 0)
set(PROGRAM_VERSION_MAJOR 0)
set(PROGRAM_VERSION_MINOR 4)
set(PROGRAM_VERSION_PATCH 0)

set(PROGRAM_VERSION_STR ${PROGRAM_VERSION_STABLE}.${PROGRAM_VERSION_MAJOR}.${PROGRAM_VERSION_MINOR}.${PROGRAM_VERSION_PATCH})
set(PROGRAM_VERSION ${PROGRAM_VERSION_STABLE}.${PROGRAM_VERSION_MAJOR}${PROGRAM_VERSION_MINOR}${PROGRAM_VERSION_PATCH})

set(PROGRAM_VERSIONS "\"${PROGRAM_VERSION_STR}\"")
set(PROGRAM_VERSIONS_ANSI "\"${PROGRAM_VERSION_STR}\"")

set(ENGINECONFIGURATION				"./EngineConf.conf")
set(GAMENAMEIDENTIFICATION			Thrive)
set(GAMEVERSIONIDENTIFICATION		GAME_VERSIONS)
set(ProgramNamespace Thrive)
set(PROGRAMMASTERSERVERINFO			"MasterServerInformation(\"ThriveMasters.txt\", \"Thrive_\" GAME_VERSIONS, \"http://revolutionarygamesstudio.com/\", \"/Thrive/MastersList.png\", \"ThriveAccountCrecentials.txt\", false)")

# Include dirs
include_directories("${LEVIATHAN_SRC}/Engine")

set(DEPENDENT_LIBS ${LINK_LIBS_TOENGINE_RELEASE}
  ${LINK_LIBS_TOENGINE_DEBUG})


# Thrive compile setup
set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -Wall -Wno-unused-function -Wno-error=redundant-decls" )
include_directories(
    "${CMAKE_CURRENT_SOURCE_DIR}/src"
)

add_subdirectory(src)



set_target_properties(ThriveLib PROPERTIES
  OUTPUT_NAME Thrive
)

add_subdirectory(test)



# #######
# # Lua #
# #######

# option(LUA_CHECKS "when ON enables additional Lua safety
#        checks. Lowers performance so only use while debugging"
#        OFF)

# if(LUA_CHECKS)

#   add_definitions(-DSOL_CHECK_ARGUMENTS)
  
# endif()

# include_directories(
#   "${CMAKE_CURRENT_SOURCE_DIR}/contrib/lua"
#   )

# link_directories(
#   "${CMAKE_CURRENT_SOURCE_DIR}/contrib/lua/luajit/src"
#   )

# set(LUA_FOUND TRUE)

# if(WIN32)
#   set(LUA_LIBRARIES lua51.dll)
# else()
#   set(LUA_LIBRARIES luajit.a dl)
# endif()





#################
# Documentation #
#################

set(DOC_OUTPUT_DIR "${CMAKE_CURRENT_BINARY_DIR}/doc" CACHE PATH "Documentation output directory.")
set(DOC_INPUT "${CMAKE_CURRENT_SOURCE_DIR}/src ${CMAKE_CURRENT_SOURCE_DIR}/doc")

set( DOXYGEN_CONFIG_FILE
    "${CMAKE_CURRENT_SOURCE_DIR}/doc/doxygen.cfg.in"
)

find_package(Doxygen)

if(DOXYGEN_FOUND)
    configure_file("${DOXYGEN_CONFIG_FILE}" "${CMAKE_CURRENT_BINARY_DIR}/doxygen.cfg" @ONLY)
    add_custom_target(doc
        "${DOXYGEN_EXECUTABLE}" "${CMAKE_CURRENT_BINARY_DIR}/doxygen.cfg"
        WORKING_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/doc"
        COMMENT "Generating API documentation with Doxygen" VERBATIM
        SOURCES "${DOXYGEN_CONFIG_FILE}"
    )
endif()

###############################################################################
# Linux package
###############################################################################

if(UNIX)
  # TODO: fix this, also add windows support

  set(LINUX_LIBRARIES "${cAudio_LIBRARIES}" "${BULLET_LIBRARIES}"
    "${SWRESAMPLE_LIBRARIES}" "${OgreFFMPEG_LIBRARIES}" "${FFMPEG_LIBRARIES}"
    "${Boost_LIBRARIES}" "${OGRE_LIBRARIES}" "${OIS_LIBRARIES}" "${CMAKE_THREAD_LIBS_INIT}"
    "${BULLET_LIBRARIES}" "${CEGUI_LIBRARY}" "${TINYXML_LIBRARIES}" "${OpenAL_LIBRARIES}")

  # Linux setup
  configure_file("${CMAKE_SOURCE_DIR}/cpack/CMakeLibraryList.xml.in"
    "${CMAKE_CURRENT_BINARY_DIR}/CMakeLibraryList.xml" @ONLY
    )

  if(LUA_CHECKS)

    # creating debug releases is bad
    add_custom_target(linuxrelease
      # Please don't touch this really complex command output format
      COMMAND "ruby" "-e" \"puts \\\"\\033[31m\\\"\; puts 'ERROR: LUA_CHECKS is on. release build is disabled. Please run \\'cmake .. -DLUA_CHECKS=OFF\\' and try again'\; puts \\\"\\033[0m\\\"\; exit(3) \"
      )
    
  else()
    
    add_custom_target(linuxrelease
      COMMAND "${CMAKE_SOURCE_DIR}/cpack/PackageProject.rb" "${CMAKE_CURRENT_BINARY_DIR}"
      WORKING_DIRECTORY "${CMAKE_SOURCE_DIR}/cpack"
      DEPENDS ThriveLib doc
      )
    # The name of the Thrive target somehow breaks in the above command
    add_dependencies(linuxrelease Thrive)

  endif()
  
endif()
