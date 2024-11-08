# Native code side of Thrive
cmake_minimum_required(VERSION 3.13)

# Must be defined before project() call
set(CMAKE_CONFIGURATION_TYPES "Debug;Release;RelWithDebInfo;Distribution" CACHE STRING "" FORCE)

project(Thrive)

# Options
# Building either the faster variant with AVX or without for older CPU support
option(PRINT_SOURCE_FILES "Print source files for targets during configuration"
  OFF)

option(THRIVE_AVX "Create faster code that needs AVX2" ON)

option(THRIVE_LTO "Use LTO when linking Thrive libraries" ON)

option(USE_OBJECT_POOLS
  "Use object pools instead of direct memory allocation (can be turned off for memory debugging)"
  ON)

option(LOCK_FREE_COLLISION_RECORDING
  "If on uses lock free collision data recording which is hopefully faster than with locks"
  ON)

option(USE_SMALL_VECTOR_POOLS
  "If on uses also pools for small list allocations in physics" OFF)

option(USE_LOCK_FREE_QUEUE
  "If on uses lock-free data structures" ON)

# TODO: implement this if it might improve task performance
# option(TASK_QUEUE_USES_POINTERS
#   "If on uses pointers in the task queue instead of the task objects themselves" ON)

option(THRIVE_DISTRIBUTION
  "Set on when building native libs for Thrive distribution" OFF)

# This is disabled for now as this is not available when cross compiling to
# Windows
option(USE_ATOMIC_COLLISION_WRITE
  "If on uses atomic write to collision data that multiple threads might want to read/write"
  OFF)

option(WARNINGS_AS_ERRORS "Treat compiler warnings as errors" OFF)

option(NULL_HAS_UNUSUAL_REPRESENTATION
  "When on it is not assumed that null equals numeric 0" OFF)

option(THRIVE_GODOT_API_FILE "Override folder Godot API file is looked for in" "")

# Add our custom CMake scripts directory
list(APPEND CMAKE_MODULE_PATH "${CMAKE_CURRENT_SOURCE_DIR}/Scripts")
include(CMakeConfiguration)
include(CMakeHelperFunctions)

# Set default build type if not specified
if(NOT CMAKE_BUILD_TYPE)
  set(CMAKE_BUILD_TYPE Debug)
endif()

if(CMAKE_BUILD_TYPE STREQUAL "RelWithDebInfo")
  set(INSTALL_BUILD_TYPE "release")
else()
  string(TOLOWER "${CMAKE_BUILD_TYPE}" INSTALL_BUILD_TYPE)
endif()

# Set installation prefix
set(CMAKE_INSTALL_PREFIX "${CMAKE_BINARY_DIR}/install/${THRIVE_OS}/${INSTALL_BUILD_TYPE}" 
  CACHE PATH "Installation directory" FORCE)

# Configure output directories
foreach(CONFIG ${CMAKE_CONFIGURATION_TYPES})
  string(TOUPPER ${CONFIG} CONFIG_UPPER)
  set(CMAKE_RUNTIME_OUTPUT_DIRECTORY_${CONFIG_UPPER} "${CMAKE_BINARY_DIR}/bin/${CONFIG}")
  set(CMAKE_LIBRARY_OUTPUT_DIRECTORY_${CONFIG_UPPER} "${CMAKE_BINARY_DIR}/lib/${CONFIG}")
  set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY_${CONFIG_UPPER} "${CMAKE_BINARY_DIR}/lib/${CONFIG}")
  set(CMAKE_PDB_OUTPUT_DIRECTORY_${CONFIG_UPPER} "${CMAKE_BINARY_DIR}/bin/${CONFIG}")
endforeach()

file(READ "${CMAKE_CURRENT_SOURCE_DIR}/src/native/NativeConstants.cs" versionFile)
string(REGEX MATCH "Version = ([0-9]+);" _ "${versionFile}")
set(NATIVE_LIBRARY_VERSION ${CMAKE_MATCH_1})

if(NOT NATIVE_LIBRARY_VERSION)
  message(FATAL_ERROR "Failed to parse native library version")
endif()

string(REGEX MATCH "EarlyCheck = ([0-9]+);" _ "${versionFile}")
set(EARLY_CHECK_LIBRARY_VERSION ${CMAKE_MATCH_1})

if(NOT EARLY_CHECK_LIBRARY_VERSION)
  message(FATAL_ERROR "Failed to parse native (early check) library version")
endif()

string(REGEX MATCH "ExtensionVersion = ([0-9]+);" _ "${versionFile}")
set(THRIVE_EXTENSION_VERSION ${CMAKE_MATCH_1})

if(NOT THRIVE_EXTENSION_VERSION)
  message(FATAL_ERROR "Failed to parse Thrive GDExtensions library version")
endif()

# Configure gdextension stuff
if(THRIVE_GODOT_API_FILE)
  set(GODOT_GDEXTENSION_DIR "${THRIVE_GODOT_API_FILE}")
else()
  set(GODOT_GDEXTENSION_DIR "${CMAKE_BINARY_DIR}/api")
endif()

include_directories(${GODOT_GDEXTENSION_DIR})

# Configure include file
configure_file(
  "${CMAKE_CURRENT_SOURCE_DIR}/src/native/Include.h.in"
  "${CMAKE_BINARY_DIR}/Include.h"
  @ONLY
)

include_directories(${CMAKE_BINARY_DIR})

# GODOT_CPP_SYSTEM_HEADERS
# GODOT_CPP_WARNING_AS_ERROR

# Add the subfolders that define the actual things to build
add_subdirectory(third_party)

if(NOT THRIVE_DISTRIBUTION OR (THRIVE_AVX AND THRIVE_DISTRIBUTION))
  add_subdirectory(src/native/early_checks)
endif()

add_subdirectory(src/native)
add_subdirectory(src/extension)

message(STATUS "Building Thrive version ${NATIVE_LIBRARY_VERSION}")
message(STATUS "Build type: ${CMAKE_BUILD_TYPE}")
message(STATUS "Install prefix: ${CMAKE_INSTALL_PREFIX}")
