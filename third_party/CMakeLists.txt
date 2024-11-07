# Native third party module references

# JoltPhysics

# TODO: option for turning JPH_TRACK_BROADPHASE_STATS on for debugging

# Might as well get used to double precision impact now as we'll want that eventually anyway
# In Jolt

# CPU core optimization
include(ProcessorCount)
ProcessorCount(CPU_CORES)
if(CPU_CORES EQUAL 0)
    set(CPU_CORES 1)
elseif(CPU_CORES GREATER 4)
    math(EXPR CPU_CORES "${CPU_CORES} - 1")
endif()

if(WIN32 AND MSVC)
    add_compile_options(/nologo /MP${CPU_CORES})
    string(APPEND CMAKE_CXX_FLAGS " /nologo")
    string(APPEND CMAKE_C_FLAGS " /nologo")
endif()

# Force dynamic runtime for all libraries
set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>DLL")

# JoltPhysics configuration
if(CMAKE_SIZEOF_VOID_P EQUAL 8)
    set(DOUBLE_PRECISION ON)
else()
    set(DOUBLE_PRECISION OFF)
    message(WARNING "32-bit build detected, using single precision")
endif()

set(USE_SSE4_1 ON)
set(USE_SSE4_2 ON)
set(USE_AVX ${THRIVE_AVX})
set(USE_AVX2 ${THRIVE_AVX})
set(USE_AVX512 OFF)
set(USE_FMADD OFF)

# Core settings
set(CMAKE_POSITION_INDEPENDENT_CODE ON)
set(USE_STATIC_MSVC_RUNTIME_LIBRARY OFF)
set(CPP_RTTI_ENABLED ON)

# Disable unused Jolt components
set(BUILD_SHARED_LIBS OFF)
set(TARGET_UNIT_TESTS OFF)
set(TARGET_VIEWER OFF)
set(TARGET_SAMPLES OFF)
set(TARGET_HELLO_WORLD OFF)
set(TARGET_PERFORMANCE_TEST OFF)

if(WIN32 AND MSVC)
    add_compile_definitions(
        JPH_PROFILE_ENABLED=0
        JPH_DEBUG_RENDERER=0
        JPH_FLOATING_POINT_EXCEPTIONS_ENABLED=0
        JPH_USE_SSE4_1=1
        JPH_USE_SSE4_2=1
        JPH_TRACK_BROADPHASE_STATS=0
        JPH_STATISTICS=0
        JPH_DISABLE_CUSTOM_ALLOCATOR
        _HAS_EXCEPTIONS=1
    )

    add_compile_options(/wd4574) # Disable C4574 warning
endif()

add_subdirectory(JoltPhysics/Build EXCLUDE_FROM_ALL)
add_subdirectory(boost EXCLUDE_FROM_ALL)

# Configure godot-cpp settings
set(GODOT_GDEXTENSION_DIR "${GODOT_GDEXTENSION_DIR}")
set(FLOAT_PRECISION "single")
set(GODOT_CPP_SYSTEM_HEADERS ON)
set(GODOT_CPP_WARNING_AS_ERROR OFF)

# Handle godot-cpp differently for Windows
if(WIN32 AND MSVC)
    # Create Windows build directory
    set(GODOT_CPP_BINARY_DIR "${CMAKE_BINARY_DIR}/godot-cpp-windows")
    file(MAKE_DIRECTORY ${GODOT_CPP_BINARY_DIR})

    # Copy godot-cpp files except CMakeLists.txt
    file(COPY ${CMAKE_CURRENT_SOURCE_DIR}/godot-cpp/ DESTINATION ${GODOT_CPP_BINARY_DIR}
        PATTERN "CMakeLists.txt" EXCLUDE
    )

    # Use Windows-specific CMakeLists.txt
    configure_file(
        "${CMAKE_CURRENT_SOURCE_DIR}/windows/godot-cpp-windows.cmake"
        "${GODOT_CPP_BINARY_DIR}/CMakeLists.txt"
        COPYONLY
    )

    add_subdirectory(${GODOT_CPP_BINARY_DIR} ${GODOT_CPP_BINARY_DIR}/build EXCLUDE_FROM_ALL)
else()
    add_subdirectory(godot-cpp EXCLUDE_FROM_ALL)
endif()

