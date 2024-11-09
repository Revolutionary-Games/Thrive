# Architecture detection
if(CMAKE_SIZEOF_VOID_P EQUAL 8)
  set(THRIVE_ARCH "x64")
  set(FLOAT_PRECISION "double")
  add_definitions(-DTHRIVE_64_BIT)
else()
  set(THRIVE_ARCH "x86")
  set(FLOAT_PRECISION "single")
  add_definitions(-DTHRIVE_32_BIT)
  message(WARNING "32-bit build detected. Some features may not work correctly.")
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

# Common compiler options
if(WIN32 AND MSVC)
  add_compile_options(
    /MP${CPU_CORES}
    /bigobj
    /GR 
    /EHsc
    /nologo
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

  target_compile_definitions(${target} PRIVATE
    NOMINMAX
    _CRT_SECURE_NO_WARNINGS
    _HAS_EXCEPTIONS=1
  )

  # Create a Windows header that defines WIN32_LEAN_AND_MEAN
  set(WIN32_HEADER "${CMAKE_BINARY_DIR}/windows_lean_mean.h")
  file(WRITE ${WIN32_HEADER} "#ifndef WIN32_LEAN_AND_MEAN\n#define WIN32_LEAN_AND_MEAN\n#endif\n")
  
  # Force include the header before any other includes
  target_compile_options(${target} PRIVATE /FI"${WIN32_HEADER}")

  set_target_properties(${target} PROPERTIES
    MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>DLL"
  )

  configure_unity_build(${target})
endfunction()

function(configure_linux_target target)
  if(WIN32)
    message(FATAL_ERROR "configure_linux_target called on Windows platform")
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
