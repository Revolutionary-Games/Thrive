# Native code side of Thrive
cmake_minimum_required(VERSION 3.13)

set(CMAKE_CONFIGURATION_TYPES "Debug;Release;RelWithDebInfo;Distribution" CACHE STRING "" FORCE)

# Force x64 for Windows before project() call
if(WIN32)
    set(CMAKE_GENERATOR_PLATFORM x64)
endif()

project(Thrive)

# If you want to get compile commands run cmake with
# "-DCMAKE_EXPORT_COMPILE_COMMANDS=ON"
# Options
# Building either the faster variant with AVX or without for older CPU support

# CPU core optimization 
include(ProcessorCount)
ProcessorCount(CPU_CORES)
if(CPU_CORES EQUAL 0)
    set(CPU_CORES 1)
elseif(CPU_CORES GREATER 4)
    math(EXPR CPU_CORES "${CPU_CORES} - 1")
endif()

# Function to configure unity build size based on CPU cores
function(configure_cpu_optimized_unity_build target)
    if(WIN32 AND MSVC)
        if(CPU_CORES LESS 4)
            set(UNITY_BATCH_SIZE 10)
        elseif(CPU_CORES LESS 8)
            set(UNITY_BATCH_SIZE 20)
        else()
            set(UNITY_BATCH_SIZE 30)
        endif()

        set_target_properties(${target} PROPERTIES
            UNITY_BUILD ON
            UNITY_BUILD_MODE GROUP
            UNITY_BUILD_BATCH_SIZE ${UNITY_BATCH_SIZE}
            VS_GLOBAL_EnableUnitySupport "true"
            VS_GLOBAL_UseUnitySupport "true"
            VS_GLOBAL_UnitySupport "true"
        )
    endif()
endfunction()

# Windows detection function
function(configure_windows_target target)
    if(NOT WIN32 OR NOT MSVC)
        return()
    endif()

    string(REGEX REPLACE "/W[0-4]" "" CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS}")

# These are the survivors that earned their right to silence
    target_compile_options(${target} PRIVATE
        /MP${CPU_CORES}
        /bigobj
        /GR 
        /EHsc
        /nologo
        /WX-
        /Zc:preprocessor
        /Zc:__cplusplus
        /Zc:externConstexpr
        /Zc:throwingNew
        /Zc:rvalueCast
        /FI"cstddef"
        /FI"cstdint"
        /FI"atomic"
        $<$<CONFIG:Debug>:/MDd /Od /Ob1 /RTC1>
        $<$<NOT:$<CONFIG:Debug>>:/MD /O2 /Ob2 /DNDEBUG>
        # Critical warning suppressions
        /wd4005  # Macro redefinition
        /wd4273  # Inconsistent dll linkage
        /wd4141  # 'inline' used more than once
        # Other warning disables
        /wd4068  # Unknown pragma
        /wd4100  # Unreferenced parameter
        /wd4127  # Conditional expression
        /wd4189  # Local variable not referenced
        /wd4201  # Nameless struct/union
        /wd4244  # Conversion possible loss of data
        /wd4245  # Signed/unsigned mismatch
        /wd4267  # Conversion from size_t
        /wd4305  # Truncation
        /wd4310  # Cast truncates constant
        /wd4311  # Pointer truncation
        /wd4324  # Structure padding
        /wd4458  # Declaration hides member
        /wd4464  # Relative include path
        /wd4505  # Unreferenced function removed
        /wd4514  # Unreferenced inline removed
        /wd4530  # Exception handler
        /wd4611  # setjmp interaction
        /wd4702  # Unreachable code
        /wd4710  # Function not inlined
        /wd4711  # Selected for inline
        /wd4820  # Padding added
        /wd4996  # Deprecated functions
        /wd5026  # Move ctor deleted
        /wd5027  # Move assign deleted
        /wd5045  # Spectre mitigation
    )

    target_compile_definitions(${target} PRIVATE
        WIN32_LEAN_AND_MEAN
        NOMINMAX
        _CRT_SECURE_NO_WARNINGS
        _CRT_NONSTDC_NO_WARNINGS
        _SCL_SECURE_NO_WARNINGS
        _HAS_EXCEPTIONS=1
    )

    set_target_properties(${target} PROPERTIES
        MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>DLL"
        VS_GLOBAL_CLToolChain "v143"
        VS_GLOBAL_PlatformToolset "v143"
        VS_GLOBAL_DisableSpecificWarnings "MSB8027;MSB8065;4244;4267;4996;4273;4141;4005;4141" 
        VS_GLOBAL_TreatWarningAsError "false"
        VS_GLOBAL_WarningLevel "0"
    )

    configure_cpu_optimized_unity_build(${target})
endfunction()

# Linux detection function
function(configure_linux_target target)
    if(WIN32)
        return()
    endif()

    target_compile_options(${target} PRIVATE
        -Wall
        -Wextra
        -Wpedantic
        -Wno-unknown-pragmas
        -fPIC
        $<$<OR:$<CONFIG:Release>,$<CONFIG:Distribution>>:-O3>
    )

    if(WARNINGS_AS_ERRORS)
        target_compile_options(${target} PRIVATE -Werror)
    endif()

    set_target_properties(${target} PROPERTIES
        POSITION_INDEPENDENT_CODE ON
        CXX_VISIBILITY_PRESET hidden
        VISIBILITY_INLINES_HIDDEN ON
    )
endfunction()

# Common target configuration function
function(configure_target_build target)
    if(WIN32 AND MSVC)
        configure_windows_target(${target})
        
        target_include_directories(${target} BEFORE PRIVATE 
            ${CMAKE_CXX_IMPLICIT_INCLUDE_DIRECTORIES}
            ${PROJECT_BINARY_DIR}
            ${PROJECT_SOURCE_DIR}/third_party/JoltPhysics
        )
    else()
        configure_linux_target(${target})
    endif()

    target_compile_definitions(${target} PRIVATE
        JPH_PROFILE_ENABLED=0
        JPH_DEBUG_RENDERER=0
        JPH_FLOATING_POINT_EXCEPTIONS_ENABLED=0
        JPH_USE_SSE4_1=1
        JPH_USE_SSE4_2=1
        JPH_TRACK_BROADPHASE_STATS=0
        JPH_STATISTICS=0
        JPH_DISABLE_CUSTOM_ALLOCATOR
    )
endfunction()

# Source files print function
function(print_source_files target)
    if(MSVC)
        get_target_property(target_sources ${target} SOURCES)
        if(target_sources)
            set(cpp_files "")
            foreach(source ${target_sources})
                if(source MATCHES ".*\\.(cpp|cxx|cc)$")
                    get_filename_component(filename ${source} NAME)
                    list(APPEND cpp_files ${filename})
                endif()
            endforeach()
            
            list(LENGTH cpp_files total_files)
            if(total_files GREATER 0)
                list(SORT cpp_files)
                message(STATUS "Files for ${target}:")
                foreach(filename ${cpp_files})
                    message(STATUS "  ${filename}")
                endforeach()
            endif()
        endif()
    endif()
endfunction()

# OS detection
if(WIN32)
    set(THRIVE_OS "windows")
    set(THRIVE_ARCH "x64")
else()
    set(THRIVE_OS "linux")
    execute_process(COMMAND uname -m OUTPUT_VARIABLE THRIVE_ARCH OUTPUT_STRIP_TRAILING_WHITESPACE)
endif()

message(STATUS "Building for ${THRIVE_OS} on ${THRIVE_ARCH}")

# Detect library version
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

# Set default build type if not specified
if(NOT CMAKE_BUILD_TYPE)
    set(CMAKE_BUILD_TYPE Debug)
endif()

# Handle RelWithDebInfo as Release for installation purposes
if(CMAKE_BUILD_TYPE STREQUAL "RelWithDebInfo")
    set(INSTALL_BUILD_TYPE "release")
else()
    string(TOLOWER "${CMAKE_BUILD_TYPE}" INSTALL_BUILD_TYPE)
endif()

# Set installation prefix
set(CMAKE_INSTALL_PREFIX "${CMAKE_BINARY_DIR}/install/${THRIVE_OS}/${INSTALL_BUILD_TYPE}" 
    CACHE PATH "Installation directory" FORCE)

# Configure output directories
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin/${INSTALL_BUILD_TYPE}")
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/lib/${INSTALL_BUILD_TYPE}")
set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/lib/${INSTALL_BUILD_TYPE}")

foreach(CONFIG ${CMAKE_CONFIGURATION_TYPES})
    if(${CONFIG} STREQUAL "RelWithDebInfo")
        set(CONFIG_LOWER "release")
    else()
        string(TOLOWER "${CONFIG}" CONFIG_LOWER)
    endif()
    
    set(CMAKE_RUNTIME_OUTPUT_DIRECTORY_${CONFIG} "${CMAKE_BINARY_DIR}/bin/${CONFIG_LOWER}")
    set(CMAKE_LIBRARY_OUTPUT_DIRECTORY_${CONFIG} "${CMAKE_BINARY_DIR}/lib/${CONFIG_LOWER}")
    set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY_${CONFIG} "${CMAKE_BINARY_DIR}/lib/${CONFIG_LOWER}")
endforeach()

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

option(WARNINGS_AS_ERRORS "Treat compiler warnings as errors" OFF)

option(NULL_HAS_UNUSUAL_REPRESENTATION
  "When on it is not assumed that null equals numeric 0" OFF)

# Extra CMake module load path if we ever need those for Thrive
# set(CMAKE_MODULE_PATH ${CMAKE_MODULE_PATH} "${PROJECT_SOURCE_DIR}/CMake")

option(THRIVE_GODOT_API_FILE "Set to override folder Godot API file is looked for in"
  "")

message(STATUS "Configured native library version ${NATIVE_LIBRARY_VERSION}")

# Configure include file
configure_file(
    "${CMAKE_CURRENT_SOURCE_DIR}/src/native/Include.h.in"
    "${CMAKE_BINARY_DIR}/Include.h"
    @ONLY
)

include_directories(${CMAKE_BINARY_DIR})

# Configure gdextension stuff
if(THRIVE_GODOT_API_FILE)
    set(GODOT_GDEXTENSION_DIR "${THRIVE_GODOT_API_FILE}")
    include_directories("${THRIVE_GODOT_API_FILE}")
else()
    set(GODOT_GDEXTENSION_DIR "${CMAKE_BINARY_DIR}/api")
    include_directories("${CMAKE_BINARY_DIR}/api")
endif()

# Force consistent runtime library
set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>DLL")

# Float precision configuration for Godot
set(FLOAT_PRECISION "single")

# GODOT_CPP_SYSTEM_HEADERS
# GODOT_CPP_WARNING_AS_ERROR

# Add the subfolders that define the actual things to build
add_subdirectory(third_party)

# Thrive AVX check location has been called only here 
if(NOT THRIVE_DISTRIBUTION OR (THRIVE_AVX AND THRIVE_DISTRIBUTION))
    # Early checks is not compiled in non-avx mode
    add_subdirectory(src/native/early_checks)
endif()

add_subdirectory(src/native)
add_subdirectory(src/extension)

message(STATUS "Building Thrive version ${NATIVE_LIBRARY_VERSION}")
message(STATUS "Platform: ${CMAKE_GENERATOR_PLATFORM}")
message(STATUS "Build type: ${CMAKE_BUILD_TYPE}")
message(STATUS "Install prefix: ${CMAKE_INSTALL_PREFIX}")
