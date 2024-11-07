# Main library of the native side of things (non-Godot using)
# The Godot using GDExtension code is in the "extension" sibling folder to this
# Due to linking path etc. requirements, that library cannot easily link to
# this which is why it is in a separate folder to clearly tell what it can
# easily call

# Note when the source code is packaged, boost (and other native)
# libraries are not included and need to be separately downloaded
# (for example from our official Thrive git repository)

add_library(thrive_native SHARED
  "${PROJECT_BINARY_DIR}/Include.h"
  core/ForwardDefinitions.hpp
  interop/CInterop.cpp interop/CInterop.h
  interop/CStructures.h interop/JoltTypeConversions.hpp
  core/Logger.cpp core/Logger.hpp
  core/Math.hpp
  core/Mutex.hpp
  core/NonCopyable.hpp core/Reference.hpp
  core/RefCounted.hpp
  core/Spinlock.hpp
  core/TaskSystem.cpp core/TaskSystem.hpp
  core/Time.hpp
  helpers/CPUCheck.hpp
  physics/BodyActivationListener.cpp physics/BodyActivationListener.hpp
  physics/BodyControlState.hpp
  physics/ContactListener.cpp physics/ContactListener.hpp
  physics/CustomConstraintTypes.hpp
  physics/Layers.hpp
  physics/PhysicalWorld.cpp physics/PhysicalWorld.hpp
  physics/PhysicsBody.cpp physics/PhysicsBody.hpp
  physics/ShapeCreator.cpp physics/ShapeCreator.hpp
  physics/ShapeWrapper.cpp physics/ShapeWrapper.hpp
  physics/SimpleShapes.cpp physics/SimpleShapes.hpp
  physics/TrackedConstraint.cpp physics/TrackedConstraint.hpp
  physics/StepListener.cpp physics/StepListener.hpp
  physics/DebugDrawForwarder.cpp physics/DebugDrawForwarder.hpp
  physics/PhysicsCollision.hpp
  physics/PhysicsRayWithUserData.hpp
  physics/ArrayRayCollector.hpp
  core/NativeLibIntercommunication.hpp
  shared/IntercommunicationManager.cpp core/IntercommunicationManager.hpp)

target_sources(thrive_native PRIVATE ${NATIVE_SOURCES})

# Print files and configure build
print_source_files(thrive_native)
configure_target_build(thrive_native)

# Common compile definitions for both OS
target_compile_definitions(thrive_native PRIVATE 
    THRIVE_NATIVE_BUILD
    _HAS_EXCEPTIONS=1
    JPH_PROFILE_ENABLED=0
    JPH_DEBUG_RENDERER=0
    JPH_FLOATING_POINT_EXCEPTIONS_ENABLED=0
    JPH_USE_SSE4_1=1
    JPH_USE_SSE4_2=1
    JPH_TRACK_BROADPHASE_STATS=0
    JPH_DISABLE_CUSTOM_ALLOCATOR
)

# OS-specific configurations
if(WIN32 AND MSVC)
    string(REGEX REPLACE "/W[0-4]" "" CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS}")
    
    target_compile_definitions(thrive_native PRIVATE
        WIN32_LEAN_AND_MEAN
        NOMINMAX
        _CRT_SECURE_NO_WARNINGS
        _CRT_NONSTDC_NO_WARNINGS
        _SCL_SECURE_NO_WARNINGS
    )

    # These are the survivors that earned their right to silence
    target_compile_options(thrive_native PRIVATE 
        /MP
        /bigobj
        /GR 
        /EHsc
        /permissive-
        /Zc:preprocessor
        /Zc:__cplusplus
        /Zc:externConstexpr
        /Zc:throwingNew
        /Zc:rvalueCast
        /WX-   # Don't treat warnings as errors
        /FI"cstddef"
        /FI"cstdint"
        /FI"atomic"
        $<$<CONFIG:Debug>:/MDd /Od /Ob1 /RTC1>
        $<$<NOT:$<CONFIG:Debug>>:/MD /O2 /Ob2 /DNDEBUG>
        /wd4068  # Unknown pragma
        /wd4100  # Unreferenced parameter
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

    set_target_properties(thrive_native PROPERTIES
        MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>DLL"
        UNITY_BUILD ON
        UNITY_BUILD_MODE GROUP
        UNITY_BUILD_BATCH_SIZE 20
        VS_GLOBAL_EnableUnitySupport "true"
        VS_GLOBAL_UseUnitySupport "true"
        VS_GLOBAL_UnitySupport "true"
        VS_GLOBAL_CLToolChain "v143"
        VS_GLOBAL_PlatformToolset "v143"
        VS_GLOBAL_DisableSpecificWarnings "MSB8027;MSB8065;4244;4267;4996;4273"
        VS_GLOBAL_TreatWarningAsError "false"
        VS_GLOBAL_WarningLevel "0"
    )
else()
    # Linux-specific compiler options
    target_compile_options(thrive_native PRIVATE
        -Wall 
        -Wextra 
        -Wpedantic
        -Wno-unknown-pragmas
        $<$<OR:$<CONFIG:Release>,$<CONFIG:Distribution>>:-O3>
    )

    if(WARNINGS_AS_ERRORS)
        target_compile_options(thrive_native PRIVATE -Werror)
    endif()
endif()

target_link_libraries(thrive_native 
    PRIVATE Jolt 
    PUBLIC Boost::intrusive
    Boost::circular_buffer 
    Boost::pool
)

target_include_directories(thrive_native PUBLIC 
    ${CMAKE_CURRENT_LIST_DIR}
    ../../third_party/concurrentqueue
    ../../third_party/JoltPhysics
)

# TODO: the one private precompiled header will probably cause issues with
# other libraries linking to this
target_precompile_headers(thrive_native 
    PUBLIC 
        "${PROJECT_BINARY_DIR}/Include.h"
        "core/ForwardDefinitions.hpp" 
        "core/RefCounted.hpp"
        <cstddef>
        <cstdint>
        <atomic>
        <memory>
        <utility>
        <type_traits>
    PRIVATE 
        "../../third_party/JoltPhysics/Jolt/Jolt.h"
)

set_target_properties(thrive_native PROPERTIES
    CXX_STANDARD 20
    CXX_STANDARD_REQUIRED ON
    CXX_EXTENSIONS OFF
    OUTPUT_NAME "libthrive_native"
    ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/lib/${INSTALL_BUILD_TYPE}"
    LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/lib/${INSTALL_BUILD_TYPE}"
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin/${INSTALL_BUILD_TYPE}"
)

if(THRIVE_LTO)
    # TODO: figure out why msys2 fails with this or disable this by default there
    set_target_properties(thrive_native PROPERTIES 
        INTERPROCEDURAL_OPTIMIZATION ON
    )
    message(STATUS "LTO is on for ThriveNative")
endif()

if(CMAKE_BUILD_TYPE STREQUAL "Debug")
    # Debug installation
    install(TARGETS thrive_native
        CONFIGURATIONS Debug
        RUNTIME DESTINATION bin
        LIBRARY DESTINATION lib
        ARCHIVE DESTINATION lib
    )

    # Install PDB files in Debug builds (Windows only)
    if(WIN32 AND MSVC)
        install(FILES $<TARGET_PDB_FILE:thrive_native>
            DESTINATION bin
            OPTIONAL
        )
    endif()
else()
    # Release/RelWithDebInfo installation
    install(TARGETS thrive_native
        CONFIGURATIONS Release RelWithDebInfo
        RUNTIME DESTINATION bin
        LIBRARY DESTINATION lib
        ARCHIVE DESTINATION lib
    )
endif()
