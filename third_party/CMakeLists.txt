# Native third party module references

# JoltPhysics

# TODO: option for turning JPH_TRACK_BROADPHASE_STATS on for debugging

# Might as well get used to double precision impact now as we'll want that eventually anyway
# In Jolt

# Suppress MSBuild output for all third party libraries
if(WIN32 AND MSVC)
    add_compile_options(/nologo)
    string(APPEND CMAKE_CXX_FLAGS " /nologo")
    string(APPEND CMAKE_C_FLAGS " /nologo")
endif()

# Force dynamic runtime for all libraries
set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>DLL")

# JoltPhysics configuration
set(DOUBLE_PRECISION ON)

if(THRIVE_AVX)
    set(USE_AVX ON)
    set(USE_AVX2 ON)

    # Not directly AVX instructions, but are about as new so are only on when AVX is on
    set(USE_F16C ON)
    set(USE_LZCNT ON)
    set(USE_TZCNT ON)

    # this is off anyway as this causes indeterminism in the simulation
    set(USE_FMADD OFF)
else()
    set(USE_AVX OFF)
    set(USE_AVX2 OFF)
    set(USE_F16C OFF)
    set(USE_FMADD OFF)
    set(USE_LZCNT OFF)
    set(USE_TZCNT OFF)
endif()

# Should be fine to require on CPUs, as these are OLD
set(USE_SSE4_1 ON)
set(USE_SSE4_2 ON)

# This is way too unsupported to enable in a general build
set(USE_AVX512 OFF)

# Need to force Jolt to be position independent to allow linking into shared
# libraries
set(CMAKE_POSITION_INDEPENDENT_CODE ON)

# Try to tell Jolt to use the DLL friendly runtime library already set up above
set(USE_STATIC_MSVC_RUNTIME_LIBRARY OFF)

# We need RTTI to be able to link without missing symbols when Jolt is built
set(CPP_RTTI_ENABLED ON)

# Jolt build configuration
set(BUILD_SHARED_LIBS OFF)
set(TARGET_UNIT_TESTS OFF)
set(TARGET_VIEWER OFF)
set(TARGET_SAMPLES OFF)  
set(TARGET_HELLO_WORLD OFF)
set(TARGET_PERFORMANCE_TEST OFF)

# Force MD/MDd runtime for Jolt
if(MSVC)
    string(REGEX REPLACE "/W[0-4]" "" CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS}")
    # Make sure Jolt uses dynamic runtime
    string(REPLACE "/MT" "/MD" CMAKE_CXX_FLAGS_DEBUG "${CMAKE_CXX_FLAGS_DEBUG}")
    string(REPLACE "/MT" "/MD" CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE}")
    string(REPLACE "/MT" "/MD" CMAKE_CXX_FLAGS_RELWITHDEBINFO "${CMAKE_CXX_FLAGS_RELWITHDEBINFO}")
    string(REPLACE "/MT" "/MD" CMAKE_CXX_FLAGS_MINSIZEREL "${CMAKE_CXX_FLAGS_MINSIZEREL}")

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
        NOMINMAX
        WIN32_LEAN_AND_MEAN
    )

    add_compile_options(
        /MP
        /bigobj
        /GR 
        /EHsc
        /nologo
        /wd4068 # Unknown pragma
        /wd4324 # Structure was padded
        /wd4530 # C++ exception handler used
        /wd4996 # Deprecated functions
        /wd4141 # 'inline' used more than once
        /wd4273 # 'function': inconsistent dll linkage
        /wd4005 # Macro redefinition
        /wd4574 # Macro is defined to be 0
        /FI"cstddef"
        /FI"cstdint"
        /FI"atomic"
        $<$<CONFIG:Debug>:/MDd>
        $<$<NOT:$<CONFIG:Debug>>:/MD>
    )
endif()

add_subdirectory(JoltPhysics/Build EXCLUDE_FROM_ALL)

if(TARGET Jolt)
    if(MSVC)
    # These are the survivors that earned their right to silence
        target_compile_options(Jolt PRIVATE
            $<$<CONFIG:Debug>:/MDd>
            $<$<NOT:$<CONFIG:Debug>>:/MD>
            /MP
            /GR 
            /EHsc
            /bigobj
            /nologo
            /wd4068 # Unknown pragma
            /wd4324 # Structure was padded
            /wd4530 # C++ exception handler used
            /wd4996 # Deprecated functions
            /wd4141 # 'inline' used more than once
            /wd4273 # 'function': inconsistent dll linkage
            /wd4005 # Macro redefinition
            /wd4574 # Macro is defined to be 0
            /FI"cstddef"
            /FI"cstdint"
            /FI"atomic"
        )

        set_target_properties(Jolt PROPERTIES
            MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>DLL"
            RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin/${INSTALL_BUILD_TYPE}"
            LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/lib/${INSTALL_BUILD_TYPE}"
            ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/lib/${INSTALL_BUILD_TYPE}"
            VS_GLOBAL_DisableSpecificWarnings "MSB8027;MSB8065;4244;4267;4996;4273;4141;9025;9035;9036;4005;4574"
        )
    endif()
endif()

add_subdirectory(boost EXCLUDE_FROM_ALL)

# Configure godot-cpp settings
set(GODOT_GDEXTENSION_DIR "${GODOT_GDEXTENSION_DIR}")
set(GODOT_CUSTOM_API_FILE "${GODOT_GDEXTENSION_API_FILE}")
set(FLOAT_PRECISION "single")
set(GODOT_CPP_SYSTEM_HEADERS ON)
set(GODOT_CPP_WARNING_AS_ERROR OFF)

# Force Jolt library to use the right MSVC runtime (this is for some reason required as 
# the above library set doesn't work fully to override things)
# Could be just a case of CMake policy CMP0091 not being set to new
if(WIN32 AND MSVC)
    # Create a custom directory for the Windows build
    set(GODOT_CPP_BINARY_DIR "${CMAKE_BINARY_DIR}/godot-cpp-windows")
    file(MAKE_DIRECTORY ${GODOT_CPP_BINARY_DIR})

    # Copy all godot-cpp files except CMakeLists.txt
    file(COPY ${CMAKE_CURRENT_SOURCE_DIR}/godot-cpp/ DESTINATION ${GODOT_CPP_BINARY_DIR}
        PATTERN "CMakeLists.txt" EXCLUDE
    )

    # Use our Windows-specific CMakeLists.txt
    configure_file(
        "${CMAKE_CURRENT_SOURCE_DIR}/windows/godot-cpp-windows.cmake"
        "${GODOT_CPP_BINARY_DIR}/CMakeLists.txt"
        COPYONLY
    )

    # Add the Windows version
    add_subdirectory(${GODOT_CPP_BINARY_DIR} ${GODOT_CPP_BINARY_DIR}/build EXCLUDE_FROM_ALL)
else()
    # On Linux, use the original directory with the original CMakeLists.txt
    add_subdirectory(godot-cpp EXCLUDE_FROM_ALL)
endif()

# Configure godot-cpp target if it exists
if(TARGET godot-cpp)
    if(WIN32 AND MSVC)
        # Additional Windows-specific configurations if needed
        target_compile_definitions(godot-cpp PRIVATE
            WIN32_LEAN_AND_MEAN
            NOMINMAX
            _CRT_SECURE_NO_WARNINGS
            _CRT_NONSTDC_NO_WARNINGS
            _SCL_SECURE_NO_WARNINGS
            _HAS_EXCEPTIONS=1
            GODOT_API_COMPATIBLE
        )

        set_target_properties(godot-cpp PROPERTIES
            MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>DLL"
            RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin/${INSTALL_BUILD_TYPE}"
            LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/lib/${INSTALL_BUILD_TYPE}"
            ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/lib/${INSTALL_BUILD_TYPE}"
            VS_GLOBAL_DisableSpecificWarnings "MSB8027;MSB8065;4244;4267;4996;4273;4141;9025;9035;9036;4005;4574"
        )
    endif()
endif()
