# GDExtension for Thrive using C++.
# The non-Godot using C++ is in the "native" sibling folder.

# Note when the source code is packaged, godot-cpp (and other native)
# libraries are not included and need to be separately downloaded
# (for example from our official Thrive git repository)

add_library(thrive_extension SHARED
  "${PROJECT_BINARY_DIR}/Include.h"
  core/RegisterThriveTypes.cpp core/RegisterThriveTypes.hpp
  core/ThriveConfig.cpp core/ThriveConfig.hpp
  interop/ExtensionInterop.cpp interop/ExtensionInterop.h
  nodes/DebugDrawer.cpp nodes/DebugDrawer.hpp
  core/GodotJoltConversions.hpp
)

target_sources(thrive_extension PRIVATE ${EXTENSION_SOURCES})

# Print files and configure build
print_source_files(thrive_extension)

# Common compile definitions for both OS
target_compile_definitions(thrive_extension PRIVATE 
    THRIVE_NATIVE_BUILD
    _HAS_EXCEPTIONS=1
    JPH_PROFILE_ENABLED=0
    JPH_DEBUG_RENDERER=0
    JPH_FLOATING_POINT_EXCEPTIONS_ENABLED=0
    JPH_USE_SSE4_1=1
    JPH_USE_SSE4_2=1
    JPH_TRACK_BROADPHASE_STATS=0
    JPH_DISABLE_CUSTOM_ALLOCATOR
    TYPED_METHOD_BIND
    _ALLOW_KEYWORD_MACROS
    GDCLASS_NO_RTTI
)

# OS-specific configurations
if(WIN32 AND MSVC)
    # Clear any existing warning levels
    string(REGEX REPLACE "/W[0-4]" "" CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS}")

    # Windows-specific compile definitions
    target_compile_definitions(thrive_extension PRIVATE 
        WIN32_LEAN_AND_MEAN
        NOMINMAX
        _CRT_SECURE_NO_WARNINGS
        _CRT_NONSTDC_NO_WARNINGS
        _SCL_SECURE_NO_WARNINGS
    )

    # These are the survivors that earned their right to silence
    target_compile_options(thrive_extension PRIVATE 
        /MP
        /bigobj
        /vmg    # Use full generality for member pointers
        /vmm    # Multi-inheritance model
        /GR     # Enable RTTI
        /EHsc   # Exception handling model
        /WX-    # Don't treat warnings as errors
        /FI"cstddef"
        /FI"cstdint"
        /FI"atomic"
        $<$<CONFIG:Debug>:/MDd /Od /Ob1 /RTC1>
        $<$<NOT:$<CONFIG:Debug>>:/MD /O2 /Ob2 /DNDEBUG>
        # Disable specific warnings
        /wd4068  # Unknown pragma
        /wd4100  # Unreferenced formal parameter
        /wd4127  # Conditional expression is constant
        /wd4189  # Local variable initialized but not referenced
        /wd4201  # Nonstandard extension: nameless struct/union
        /wd4244  # Conversion: possible loss of data
        /wd4245  # Conversion: signed/unsigned mismatch
        /wd4267  # Conversion from size_t
        /wd4305  # Truncation
        /wd4310  # Cast truncates constant value
        /wd4311  # Pointer truncation
        /wd4324  # Structure was padded
        /wd4141 # Inline used more than once (added at the top of warning list)
        /wd4458  # Declaration hides class member
        /wd4464  # Relative include path contains '..'
        /wd4505  # Unreferenced local function removed
        /wd4514  # Unreferenced inline function removed
        /wd4530  # C++ exception handler used
        /wd4611  # Interaction between '_setjmp' and C++ object destruction
        /wd4702  # Unreachable code
        /wd4710  # Function not inlined
        /wd4711  # Function selected for inline expansion
        /wd4820  # Padding added
        /wd4996  # Deprecated functions
        /wd5026  # Move constructor was implicitly defined as deleted
        /wd5027  # Move assignment operator was implicitly defined as deleted
        /wd5045  # Compiler will insert Spectre mitigation
        /Zc:preprocessor 
        /Zc:__cplusplus
        /Zc:externConstexpr
        /Zc:throwingNew
        /Zc:rvalueCast
        /GF     # Enable string pooling
        /Zc:forScope
        /Zc:inline
        /Zc:wchar_t
    )

    # Windows-specific target properties
    set_target_properties(thrive_extension PROPERTIES
        MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>DLL"
        UNITY_BUILD ON
        UNITY_BUILD_MODE GROUP
        UNITY_BUILD_BATCH_SIZE 20
        VS_GLOBAL_EnableUnitySupport "true"
        VS_GLOBAL_UseUnitySupport "true"
        VS_GLOBAL_UnitySupport "true"
        VS_GLOBAL_CLToolChain "v143"
        VS_GLOBAL_PlatformToolset "v143"
        VS_GLOBAL_DisableSpecificWarnings "MSB8027;MSB8065;4244;4267;4996;4273;4141;4005;4141;4141"
        VS_GLOBAL_TreatWarningAsError "false"
        VS_GLOBAL_WarningLevel "0"
    )
else()
    # Linux-specific compiler options
    target_compile_options(thrive_extension PRIVATE
        -Wall 
        -Wextra 
        -Wpedantic
        -Wno-unknown-pragmas
        $<$<OR:$<CONFIG:Release>,$<CONFIG:Distribution>>:-O3>
    )

    if(WARNINGS_AS_ERRORS)
        target_compile_options(thrive_extension PRIVATE -Werror)
    endif()
endif()

# Common link libraries for both OS
target_link_libraries(thrive_extension 
    PRIVATE godot-cpp Jolt thrive_native
)

# Common include directories for both OS
target_include_directories(thrive_extension PUBLIC 
    ${CMAKE_CURRENT_LIST_DIR}
    ${CMAKE_CURRENT_LIST_DIR}/../native
    ${GODOT_GDEXTENSION_DIR}
    ../../third_party/godot-cpp/include
    ../../third_party/godot-cpp/gen/include
)

# Common precompiled headers for both OS
target_precompile_headers(thrive_extension 
    PUBLIC 
        "${PROJECT_BINARY_DIR}/Include.h"
        <cstddef>
        <cstdint>
        <atomic>
        <memory>
        <utility>
        <functional>
        <type_traits>
    PRIVATE 
        "../../third_party/godot-cpp/include/godot_cpp/classes/ref.hpp"
        "${GODOT_GDEXTENSION_DIR}/gdextension_interface.h"
)

# Common properties for both OS
set_target_properties(thrive_extension PROPERTIES
    CXX_STANDARD 17
    CXX_STANDARD_REQUIRED ON
    CXX_EXTENSIONS OFF
    OUTPUT_NAME "libthrive_extension"
    ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/lib/${INSTALL_BUILD_TYPE}"
    LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/lib/${INSTALL_BUILD_TYPE}"
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin/${INSTALL_BUILD_TYPE}"
)

# LTO configuration if enabled
if(THRIVE_LTO)
    set_target_properties(thrive_extension PROPERTIES 
        INTERPROCEDURAL_OPTIMIZATION ON
    )
endif()

# Install configurations based on build type and OS
if(CMAKE_BUILD_TYPE STREQUAL "Debug")
    install(TARGETS thrive_extension
        CONFIGURATIONS Debug
        RUNTIME DESTINATION bin
        LIBRARY DESTINATION lib
        ARCHIVE DESTINATION lib
    )

    if(WIN32 AND MSVC)
        install(FILES $<TARGET_PDB_FILE:thrive_extension>
            DESTINATION bin
            OPTIONAL
        )
    endif()
else()
    install(TARGETS thrive_extension
        CONFIGURATIONS Release RelWithDebInfo
        RUNTIME DESTINATION bin
        LIBRARY DESTINATION lib
        ARCHIVE DESTINATION lib
    )
endif()
