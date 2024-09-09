# Native code side of Thrive
cmake_minimum_required(VERSION 3.10)

project(Thrive)

# If you want to get compile commands run cmake with
# "-DCMAKE_EXPORT_COMPILE_COMMANDS=ON"

# Options
# Building either the faster variant with AVX or without for older CPU support

option(THRIVE_AVX "Create faster code that needs AVX2" ON)

option(THRIVE_LTO "Use LTO when linking Thrive libraries" ON)

option(USE_OBJECT_POOLS "Use object pools instead of direct memory allocation" ON)

option(LOCK_FREE_COLLISION_RECORDING "Use lock-free collision data recording" ON)

option(USE_SMALL_VECTOR_POOLS "Use pools for small list allocations in physics" OFF)

option(USE_LOCK_FREE_QUEUE "Use lock-free data structures" ON)

option(THRIVE_DISTRIBUTION "Build native libs for Thrive distribution" OFF)

# This is disabled for now as this is not available when cross compiling to
# Windows
option(USE_ATOMIC_COLLISION_WRITE "Use atomic write to collision data" OFF)

option(WARNINGS_AS_ERRORS "Treat compiler warnings as errors" ON)

option(NULL_HAS_UNUSUAL_REPRESENTATION "Assume null doesn't equal numeric 0" OFF)

set(CMAKE_CXX_STANDARD 20)

set(CMAKE_CXX_STANDARD_REQUIRED ON)

set(CMAKE_CXX_EXTENSIONS OFF)

# Use static runtime library for all configurations
set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")

# Also use a lib prefix on windows for consistency
if(WIN32)
  set(CMAKE_SHARED_LIBRARY_PREFIX_CXX "lib")
endif()

# Set output directories
set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib)

set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib)

set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/bin)

# AVX support
if(NOT THRIVE_AVX)
  message(STATUS "Building without AVX support")
  set(CMAKE_SHARED_LIBRARY_SUFFIX "_without_avx${CMAKE_SHARED_LIBRARY_SUFFIX}")
  set(CMAKE_STATIC_LIBRARY_SUFFIX "_without_avx${CMAKE_STATIC_LIBRARY_SUFFIX}")
endif()

# Set default build type
if(NOT CMAKE_BUILD_TYPE)
  set(CMAKE_BUILD_TYPE Release CACHE STRING "Choose the type of build." FORCE)
endif()

# Configuration types
set(CMAKE_CONFIGURATION_TYPES "Debug;Release;RelWithDebInfo;Distribution")

# Set flags for Distribution configuration
set(CMAKE_C_FLAGS_DISTRIBUTION "${CMAKE_C_FLAGS_RELEASE}" CACHE STRING "Flags used by the C compiler during Distribution builds." FORCE)
set(CMAKE_CXX_FLAGS_DISTRIBUTION "${CMAKE_CXX_FLAGS_RELEASE}" CACHE STRING "Flags used by the CXX compiler during Distribution builds." FORCE)
set(CMAKE_EXE_LINKER_FLAGS_DISTRIBUTION "${CMAKE_EXE_LINKER_FLAGS_RELEASE}" CACHE STRING "Flags used for linking binaries during Distribution builds." FORCE)
set(CMAKE_SHARED_LINKER_FLAGS_DISTRIBUTION "${CMAKE_SHARED_LINKER_FLAGS_RELEASE}" CACHE STRING "Flags used by the shared libraries linker during Distribution builds." FORCE)
set(CMAKE_STATIC_LINKER_FLAGS_DISTRIBUTION "${CMAKE_STATIC_LINKER_FLAGS_RELEASE}" CACHE STRING "Flags used by the static libraries linker during Distribution builds." FORCE)

mark_as_advanced(
  CMAKE_CXX_FLAGS_DISTRIBUTION
  CMAKE_C_FLAGS_DISTRIBUTION
  CMAKE_EXE_LINKER_FLAGS_DISTRIBUTION
  CMAKE_SHARED_LINKER_FLAGS_DISTRIBUTION
  CMAKE_STATIC_LINKER_FLAGS_DISTRIBUTION
)

# LTO support
if(THRIVE_LTO)
  include(CheckIPOSupported)
  check_ipo_supported(RESULT supported OUTPUT error)
  if(supported)
    set(CMAKE_INTERPROCEDURAL_OPTIMIZATION TRUE)
  else()
    message(WARNING "LTO is not supported: ${error}")
  endif()
endif()

# Detect library version
file(READ "src/native/NativeConstants.cs" versionFile)
string(REGEX MATCH "Version = ([0-9]+);" _ "${versionFile}")
set(NATIVE_LIBRARY_VERSION ${CMAKE_MATCH_1})

# Set THRIVE_EXTENSION_VERSION
set(THRIVE_EXTENSION_VERSION ${NATIVE_LIBRARY_VERSION})

# Configure include file
configure_file("src/native/Include.h.in" "${PROJECT_BINARY_DIR}/Include.h" @ONLY)
include_directories(${PROJECT_BINARY_DIR})

# Force include the generated Include.h file
if(MSVC)
  add_compile_options(/FI"${PROJECT_BINARY_DIR}/Include.h")
else()
  add_compile_options(-include "${PROJECT_BINARY_DIR}/Include.h")
endif()

# Set gdextension stuff
set(GODOT_GDEXTENSION_DIR "${CMAKE_CURRENT_SOURCE_DIR}/third_party/godot-cpp/gdextension")
include_directories(${GODOT_GDEXTENSION_DIR})

# Add the subfolders that define the actual things to build
add_subdirectory(third_party)
add_subdirectory(src/native)
add_subdirectory(src/extension)

# Jolt Physics specific settings
if(TARGET Jolt)
  set_target_properties(Jolt PROPERTIES 
    MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>"
    COMPILE_WARNING_AS_ERROR OFF
  )
  if(MSVC)
    target_compile_options(Jolt PRIVATE /wd4710 /wd4711)
  endif()
endif()

message(STATUS "LTO is ${THRIVE_LTO} for Thrive")
