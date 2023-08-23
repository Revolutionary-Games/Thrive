# Native code side of Thrive
cmake_minimum_required(VERSION 3.10)

project(Thrive)

# If you want to get compile commands run cmake with
# "-DCMAKE_EXPORT_COMPILE_COMMANDS=ON"

# Options
option(USE_OBJECT_POOLS
  "Use object pools instead of direct memory allocation (can be turned off for memory debugging)"
  ON)

# Note that PhysicsBody::RecordCollision is not fully certain that it is actually thead safe in this mode
option(LOCK_FREE_COLLISION_RECORDING
  "If on uses lock free collision data recording which is hopefully faster than with locks" ON)

option(USE_SMALL_VECTOR_POOLS
  "If on uses also pools for small list allocations in physics" OFF)

option(WARNINGS_AS_ERRORS "When on treat compiler warnings as errors" ON)

option(NULL_HAS_UNUSUAL_REPRESENTATION
  "When on it is not assumed that null equals numeric 0" OFF)

# set(CMAKE_MODULE_PATH ${CMAKE_MODULE_PATH} "${PROJECT_SOURCE_DIR}/CMake")

# static building
set(CMAKE_FIND_LIBRARY_SUFFIXES ".a")
set(BUILD_SHARED_LIBS OFF)

# Common options
if(CMAKE_BUILD_TYPE STREQUAL "")
  set(CMAKE_BUILD_TYPE Release CACHE STRING
    "Set the build type, usually Debug or Distribution" FORCE)
endif()

# A bit unneeded Jolt hack
# # This and the following hack is required due to Jolt being very debug-y in
# # release mode
# elseif(CMAKE_BUILD_TYPE STREQUAL "Release")
# set(CMAKE_BUILD_TYPE Distribution)
# elseif(CMAKE_BUILD_TYPE STREQUAL "RelWithDebInfo")
# set(CMAKE_BUILD_TYPE Distribution)

if(CMAKE_BUILD_TYPE STREQUAL "Debug")
  set(CMAKE_INSTALL_PREFIX "${CMAKE_INSTALL_PREFIX}/debug")
else()
  set(CMAKE_INSTALL_PREFIX "${CMAKE_INSTALL_PREFIX}/release")
endif()

# Detect library version
file(READ "src/native/NativeConstants.cs" versionFile)

string(REGEX MATCH "Version = ([0-9]+);" _ "${versionFile}")
set(NATIVE_LIBRARY_VERSION ${CMAKE_MATCH_1})

if(NOT NATIVE_LIBRARY_VERSION)
  message(FATAL_ERROR "Failed to parse native library version")
endif()

message(STATUS "Configured native library version ${NATIVE_LIBRARY_VERSION}")

# Configure include file
configure_file("src/native/Include.h.in" "${PROJECT_BINARY_DIR}/Include.h")
include_directories(${PROJECT_BINARY_DIR})

# Add the subfolders that define the actual things to build
add_subdirectory(third_party)

add_subdirectory(src/native)
