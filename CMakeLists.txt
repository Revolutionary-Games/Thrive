# Native code side of Thrive
cmake_minimum_required(VERSION 3.10)

project(Thrive)

# If you want to get compile commands run cmake with
# "-DCMAKE_EXPORT_COMPILE_COMMANDS=ON"

# Options
# Building either the faster variant with AVX or without for older CPU support
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

option(WARNINGS_AS_ERRORS "When on treat compiler warnings as errors" ON)

option(NULL_HAS_UNUSUAL_REPRESENTATION
  "When on it is not assumed that null equals numeric 0" OFF)

# Extra CMake module load path if we ever need those for Thrive
# set(CMAKE_MODULE_PATH ${CMAKE_MODULE_PATH} "${PROJECT_SOURCE_DIR}/CMake")

option(THRIVE_GODOT_API_FILE "Set to override folder Godot API file is looked for in"
  "")

# Also use a lib prefix on windows for consistency
if(WIN32)
  set(CMAKE_SHARED_LIBRARY_PREFIX_CXX "lib")
endif()

if(NOT THRIVE_AVX)
  message(STATUS "Building without AVX support")
  set(CMAKE_SHARED_LIBRARY_SUFFIX "_without_avx${CMAKE_SHARED_LIBRARY_SUFFIX}")
  set(CMAKE_STATIC_LIBRARY_SUFFIX "_without_avx${CMAKE_STATIC_LIBRARY_SUFFIX}")
endif()

# static building (on Linux)
set(CMAKE_FIND_LIBRARY_SUFFIXES ".a")
set(BUILD_SHARED_LIBS OFF)

# Configuration types. Jolt requires a "Distribution" build type
set(CMAKE_CONFIGURATION_TYPES "Debug;Release;RelWithDebInfo;Distribution")

# Set flags for Distribution configuration
# These are based on relwithdebinfo as we want symbols for eventual crash reporting
set(CMAKE_C_FLAGS_DISTRIBUTION "${CMAKE_C_FLAGS_RELWITHDEBINFO}" CACHE
  STRING "Flags used by the C compiler during Distribution builds." FORCE)
set(CMAKE_CXX_FLAGS_DISTRIBUTION "${CMAKE_CXX_FLAGS_RELWITHDEBINFO}" CACHE
  STRING "Flags used by the CXX compiler during Distribution builds." FORCE)
set(CMAKE_EXE_LINKER_FLAGS_DISTRIBUTION "${CMAKE_EXE_LINKER_FLAGS_RELWITHDEBINFO}" CACHE
  STRING "Flags used for linking binaries during Distribution builds." FORCE)
set(CMAKE_SHARED_LINKER_FLAGS_DISTRIBUTION "${CMAKE_SHARED_LINKER_FLAGS_RELWITHDEBINFO}" CACHE
  STRING "Flags used by the shared libraries linker during Distribution builds." FORCE)
set(CMAKE_STATIC_LINKER_FLAGS_DISTRIBUTION "${CMAKE_STATIC_LINKER_FLAGS_RELWITHDEBINFO}" CACHE
  STRING "Flags used by the static libraries linker during Distribution builds." FORCE)

mark_as_advanced(
  CMAKE_CXX_FLAGS_DISTRIBUTION
  CMAKE_C_FLAGS_DISTRIBUTION
  CMAKE_EXE_LINKER_FLAGS_DISTRIBUTION
  CMAKE_SHARED_LINKER_FLAGS_DISTRIBUTION
  CMAKE_STATIC_LINKER_FLAGS_DISTRIBUTION
)

# If wanted, and extra safety check before LTO enable
# if(THRIVE_LTO)
#   include(CheckIPOSupported)
#   check_ipo_supported(RESULT supported OUTPUT error)
#   if(supported)
#     set(CMAKE_INTERPROCEDURAL_OPTIMIZATION TRUE)
#   else()
#     message(WARNING "LTO is not supported: ${error}")
#   endif()
# endif()

# Common options
if(NOT CMAKE_BUILD_TYPE)
  set(CMAKE_BUILD_TYPE Release CACHE STRING
    "Set the build type, usually Debug or Distribution" FORCE)
endif()

if(CMAKE_BUILD_TYPE STREQUAL "Debug")
  set(CMAKE_INSTALL_PREFIX "${CMAKE_INSTALL_PREFIX}/debug")
else()
  set(CMAKE_INSTALL_PREFIX "${CMAKE_INSTALL_PREFIX}/release")
endif()

# Standard library and other linking flags
if(CMAKE_CXX_COMPILER_ID STREQUAL "Clang")

  set(CLANG_DEFAULT_CXX_STDLIB libc++)
  set(CLANG_DEFAULT_RTLIB compiler-rt)

  # set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -stdlib=libc++")
  # set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -stdlib=libc++")
  ### apparently lld can't be specified here as llvm-ar will complain
  # set(CMAKE_STATIC_LINKER_FLAGS
  #   "${CMAKE_STATIC_LINKER_FLAGS}  -lc++abi -static-libstdc++ -fuse-ld=lld")
  #set(CMAKE_STATIC_LINKER_FLAGS
  #  "${CMAKE_STATIC_LINKER_FLAGS} -static -lc++abi -pthread -fuse-ld=lld")
  #set(CMAKE_SHARED_LINKER_FLAGS
  #  "${CMAKE_SHARED_LINKER_FLAGS} -static -lc++abi -pthread -fuse-ld=lld")

  # set(CMAKE_EXE_LINKER_FLAGS )

  # static can't specify a linker, seems to use llvm-ar thankfully
  # set(CMAKE_STATIC_LINKER_FLAGS "${CMAKE_STATIC_LINKER_FLAGS} -fuse-ld=lld")
  set(CMAKE_SHARED_LINKER_FLAGS
    "${CMAKE_SHARED_LINKER_FLAGS} -fuse-ld=lld")

  # fully static standard lib, seems to fail as libc is not relocatable code
  # set(CMAKE_SHARED_LINKER_FLAGS
  #   "${CMAKE_SHARED_LINKER_FLAGS} -static")
  # set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -static")
  # set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -static")

elseif(CMAKE_CXX_COMPILER_ID STREQUAL "GNU")
  set(CMAKE_STATIC_LINKER_FLAGS "${CMAKE_STATIC_LINKER_FLAGS} -fuse-ld=gold")
  set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} -fuse-ld=gold")
else()
endif()

# Make better loading Linux binaries
set(CMAKE_BUILD_WITH_INSTALL_RPATH ON)
set(CMAKE_INSTALL_RPATH "$ORIGIN")

# Avoid lib64 suffix on some Linux distros which will break the script
set(CMAKE_INSTALL_LIBDIR "lib")

# Detect library version
file(READ "src/native/NativeConstants.cs" versionFile)

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

message(STATUS "Configured native library version ${NATIVE_LIBRARY_VERSION}")

# Configure include file
configure_file("src/native/Include.h.in" "${PROJECT_BINARY_DIR}/Include.h")
include_directories(${PROJECT_BINARY_DIR})

# Configure gdextension stuff
if(THRIVE_GODOT_API_FILE)
  set(GODOT_GDEXTENSION_DIR "${THRIVE_GODOT_API_FILE}")
  include_directories("${THRIVE_GODOT_API_FILE}")
  message(STATUS "Using custom location of Godot API file")
else()
  set(GODOT_GDEXTENSION_DIR "${CMAKE_BINARY_DIR}/api")
  include_directories("${CMAKE_BINARY_DIR}/api")
endif()

# Float precision configuration for Godot
set(FLOAT_PRECISION "single")

# GODOT_CPP_SYSTEM_HEADERS
# GODOT_CPP_WARNING_AS_ERROR

# Add the subfolders that define the actual things to build
add_subdirectory(third_party)

if(NOT THRIVE_DISTRIBUTION OR (THRIVE_AVX AND THRIVE_DISTRIBUTION))
  # Early checks is not compiled in non-avx mode
  add_subdirectory(src/native/early_checks)
endif()

add_subdirectory(src/native)
add_subdirectory(src/extension)

