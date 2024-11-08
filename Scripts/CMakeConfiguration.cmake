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

# Platform detection (platform-agnostic)
if(WIN32)
  set(THRIVE_OS "windows")
elseif(APPLE)
  set(THRIVE_OS "macos")
else()
  set(THRIVE_OS "linux")
endif()

# Base configuration function
function(configure_target target)
  if(WIN32 AND MSVC)
    include(MSVCConfiguration)
    configure_msvc_target(${target})
  else()
    target_compile_options(${target} PRIVATE
      -Wall
      -Wextra
      -Wpedantic
      -Wno-unknown-pragmas
      $<$<OR:$<CONFIG:Release>,$<CONFIG:Distribution>>:-O3>
    )

    if(WARNINGS_AS_ERRORS)
      target_compile_options(${target} PRIVATE -Werror)
    endif()

    set_target_properties(${target} PROPERTIES
      POSITION_INDEPENDENT_CODE ON
      CXX_VISIBILITY_PRESET hidden
    )
  endif()

  # Common properties for all platforms
  set_target_properties(${target} PROPERTIES
    CXX_STANDARD 20
    CXX_STANDARD_REQUIRED ON
    CXX_EXTENSIONS OFF
  )
endfunction()
