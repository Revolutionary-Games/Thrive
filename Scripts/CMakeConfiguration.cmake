# Architecture detection
if(CMAKE_SIZEOF_VOID_P EQUAL 8)
    set(THRIVE_ARCH "x64")
    set(FLOAT_PRECISION "double")
    add_definitions(-DTHRIVE_64_BIT)
else()
    set(THRIVE_ARCH "x86")
    set(FLOAT_PRECISION "single")
    add_definitions(-DTHRIVE_32_BIT)
    message(WARNING "32-bit build detected. Some features may not work correctly due to struct size differences.")
endif()

# OS Detection
if(WIN32)
    set(THRIVE_OS "windows")
else()
    set(THRIVE_OS "linux")
endif()

# CPU core optimization
include(ProcessorCount)
ProcessorCount(CPU_CORES)
if(CPU_CORES EQUAL 0)
    set(CPU_CORES 1)
elseif(CPU_CORES GREATER 4)
    math(EXPR CPU_CORES "${CPU_CORES} - 1")
endif()

# Warning configuration
set(THRIVE_MSVC_WARNING_SUPPRESS
    /wd4005  # Macro redefinition
    /wd4273  # Inconsistent dll linkage
    /wd4141  # 'inline' used more than once
)

# Common compiler options
if(WIN32 AND MSVC)
    add_compile_options(
        /MP${CPU_CORES}
        /bigobj
        /GR 
        /EHsc
        /nologo
        ${THRIVE_MSVC_WARNING_SUPPRESS}
        $<$<CONFIG:Debug>:/MDd>
        $<$<NOT:$<CONFIG:Debug>>:/MD>
    )

    add_compile_definitions(
        WIN32_LEAN_AND_MEAN
        NOMINMAX
        _CRT_SECURE_NO_WARNINGS
        _HAS_EXCEPTIONS=1
    )

    # Force consistent runtime
    set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>DLL")
endif()

# Target configuration functions
function(configure_windows_target target)
    if(NOT WIN32 OR NOT MSVC)
        message(FATAL_ERROR "function called on non-Windows/MSVC platform for target ${target}")
        return()
    endif()

    target_compile_options(${target} PRIVATE
        ${THRIVE_MSVC_WARNING_SUPPRESS}
    )

    target_compile_definitions(${target} PRIVATE
        WIN32_LEAN_AND_MEAN
        NOMINMAX
        _CRT_SECURE_NO_WARNINGS
        _HAS_EXCEPTIONS=1
    )

    set_target_properties(${target} PROPERTIES
        MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>DLL"
    )

    configure_unity_build(${target})
endfunction()

function(configure_linux_target target)
    if(WIN32)
        return()
    endif()

    target_compile_options(${target} PRIVATE
        -Wall
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
        )
    else()
        configure_linux_target(${target})
    endif()
endfunction()

# Function to configure godot-cpp target without modifying its CMakeLists
function(configure_godot_cpp_target target)
    if(TARGET ${target} AND MSVC)
        target_compile_options(${target} PRIVATE
            ${THRIVE_MSVC_WARNING_SUPPRESS}
        )

        set_target_properties(${target} PROPERTIES
            MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>DLL"
            UNITY_BUILD ON
            UNITY_BUILD_MODE GROUP
            UNITY_BUILD_BATCH_SIZE 100
        )
    endif()
endfunction()