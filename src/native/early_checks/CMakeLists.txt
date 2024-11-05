# Early load library that allows checking things before loading the main library,
# which causes global constructors to run that may already do some complex stuff
add_library(early_checks SHARED)

# Collect all source files recursively
file(GLOB_RECURSE EARLY_CHECK_SOURCES 
    "*.cpp"
    "*.hpp"
    "*.h"
)

target_sources(early_checks PRIVATE ${EARLY_CHECK_SOURCES})

# Print files and configure build
print_source_files(early_checks)
configure_target_build(early_checks)

# Common compile definitions for both OS
target_compile_definitions(early_checks PRIVATE 
    EARLY_CHECK_BUILD
    _HAS_EXCEPTIONS=1
)

# OS-specific configurations
if(WIN32 AND MSVC)
    # Clear any existing warning levels
    string(REGEX REPLACE "/W[0-4]" "" CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS}")
    
    # Windows-specific compile definitions
    target_compile_definitions(early_checks PRIVATE 
        WIN32_LEAN_AND_MEAN
        NOMINMAX
        _CRT_SECURE_NO_WARNINGS
        _CRT_NONSTDC_NO_WARNINGS
        _SCL_SECURE_NO_WARNINGS
    )

    # These are the survivors that earned their right to silence
    target_compile_options(early_checks PRIVATE 
        /MP
        /bigobj
        /GR 
        /EHsc
        /WX-    # Don't treat warnings as errors
        /FI"cstddef"
        /FI"cstdint"
        /FI"atomic"
        # Debug/Release specific options
        $<$<CONFIG:Debug>:/MDd /Od /Ob1 /RTC1>
        $<$<NOT:$<CONFIG:Debug>>:/MD /O2 /Ob2 /DNDEBUG>
        # Warning disables
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
    )

    # Windows-specific target properties
    set_target_properties(early_checks PROPERTIES
        MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>DLL"
        UNITY_BUILD ON
        UNITY_BUILD_MODE GROUP
        UNITY_BUILD_BATCH_SIZE 20
        VS_GLOBAL_EnableUnitySupport "true"
        VS_GLOBAL_UseUnitySupport "true"
        VS_GLOBAL_UnitySupport "true"
        VS_GLOBAL_DisableSpecificWarnings "MSB8027;MSB8065;4244;4267;4996;4273;4141;9025;9035;9036;4005;4574"
    )
else()
    # Linux-specific compiler options
    target_compile_options(early_checks PRIVATE
        -march=sandybridge  # Minimum CPU support for required instructions
        -Wall 
        -Wextra 
        -Wpedantic 
        -Wno-unknown-pragmas
        # Release mode optimizations
        $<$<CONFIG:Release>:-DNDEBUG -O3>
        $<$<CONFIG:Distribution>:-DNDEBUG -O3>
    )

    if(WARNINGS_AS_ERRORS)
        target_compile_options(early_checks PRIVATE -Werror)
    endif()

    # Linux-specific target properties
    set_target_properties(early_checks PROPERTIES
        POSITION_INDEPENDENT_CODE ON
        CXX_VISIBILITY_PRESET hidden
        VISIBILITY_INLINES_HIDDEN ON
    )
endif()

# Common properties for both OS
set_target_properties(early_checks PROPERTIES
    CXX_STANDARD 20
    CXX_STANDARD_REQUIRED ON
    CXX_EXTENSIONS OFF
    OUTPUT_NAME "libearly_checks"
    ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/lib/${INSTALL_BUILD_TYPE}"
    LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/lib/${INSTALL_BUILD_TYPE}"
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin/${INSTALL_BUILD_TYPE}"
    MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>DLL"
)

# LTO configuration if enabled
if(THRIVE_LTO)
    set_target_properties(early_checks PROPERTIES 
        INTERPROCEDURAL_OPTIMIZATION ON
    )
endif()

# Install configurations based on build type and OS
if(CMAKE_BUILD_TYPE STREQUAL "Debug")
    # Debug installation
    install(TARGETS early_checks
        CONFIGURATIONS Debug
        RUNTIME DESTINATION bin
        LIBRARY DESTINATION lib
        ARCHIVE DESTINATION lib
    )

    # Install PDB for Debug (Windows only)
    if(WIN32 AND MSVC)
        install(FILES $<TARGET_PDB_FILE:early_checks>
            DESTINATION bin
            OPTIONAL
        )
    endif()
else()
    # Release/RelWithDebInfo installation
    install(TARGETS early_checks
        CONFIGURATIONS Release RelWithDebInfo
        RUNTIME DESTINATION bin
        LIBRARY DESTINATION lib
        ARCHIVE DESTINATION lib
    )
endif()
