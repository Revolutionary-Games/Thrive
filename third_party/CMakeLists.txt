# Native third party module references

# JoltPhysics

# TODO: option for turning JPH_TRACK_BROADPHASE_STATS on for debugging

# Might as well get used to double precision impact now as we'll want that eventually anyway
# In Jolt

# Need to force Jolt to be position independent to allow linking into shared
# libraries
set(CMAKE_POSITION_INDEPENDENT_CODE ON)

# Make sure this other option doesn't accidentally trigger in Jolt
set(USE_STATIC_MSVC_RUNTIME_LIBRARY OFF)

# We need RTTI to be able to link without missing symbols when Jolt is built
set(CPP_RTTI_ENABLED ON)

if(MSVC)
  set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>DLL")
  
  foreach(flag_var
    CMAKE_C_FLAGS CMAKE_C_FLAGS_DEBUG CMAKE_C_FLAGS_RELEASE
    CMAKE_CXX_FLAGS CMAKE_CXX_FLAGS_DEBUG CMAKE_CXX_FLAGS_RELEASE)
    if(${flag_var} MATCHES "/MT")
      string(REGEX REPLACE "/MT" "/MD" ${flag_var} "${${flag_var}}")
    endif()
  endforeach()
endif()

# JoltPhysics configuration
if(CMAKE_SIZEOF_VOID_P EQUAL 8)
  # Force double precision and RuntimeFloat = double
  set(DOUBLE_PRECISION ON)
  add_definitions(-DJPH_DOUBLE_PRECISION=1)
  add_definitions(-DJPH_USE_DOUBLE=1)
endif()

# Should be fine to require on CPUs, as these are OLD
set(USE_SSE4_1 ON)
set(USE_SSE4_2 ON)

set(USE_AVX ${THRIVE_AVX})
set(USE_AVX2 ${THRIVE_AVX})

# This is way too unsupported to enable in a general build
set(USE_AVX512 OFF)

# this is off anyway as this causes indeterminism in the simulation
set(USE_FMADD OFF)

# Not directly AVX instructions, but are about as new so are only on when AVX is on
set(USE_LZCNT OFF)
set(USE_TZCNT OFF)

# Disable unused Jolt components
set(BUILD_SHARED_LIBS OFF)
set(TARGET_UNIT_TESTS OFF)
set(TARGET_VIEWER OFF)
set(TARGET_SAMPLES OFF)
set(TARGET_HELLO_WORLD OFF)
set(TARGET_PERFORMANCE_TEST OFF)

if(MSVC)
  add_compile_definitions(
    NOMINMAX
    WIN32_LEAN_AND_MEAN
    _CRT_SECURE_NO_WARNINGS
    JPH_DISABLE_CUSTOM_ALLOCATOR
    JPH_DEBUG_RENDERER=0
    JPH_USE_SSE4_1=1
    JPH_USE_SSE4_2=1
    JPH_TRACK_BROADPHASE_STATS=0
    JPH_PROFILE_ENABLED=$<IF:$<CONFIG:Debug>,1,0>
    _HAS_EXCEPTIONS=1
  )
endif()

# Add JoltPhysics
add_subdirectory(JoltPhysics/Build EXCLUDE_FROM_ALL)

if(MSVC AND TARGET Jolt)
  target_compile_options(Jolt PRIVATE 
    /EHsc 
    /WX- 
    /wd4574
    $<$<CONFIG:Debug>:/MDd>
    $<$<NOT:$<CONFIG:Debug>>:/MD>
  )

  set_target_properties(Jolt PROPERTIES
    MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>DLL"
    INTERFACE_COMPILE_DEFINITIONS 
      "JPH_DISABLE_CUSTOM_ALLOCATOR;JPH_DOUBLE_PRECISION=1;JPH_USE_DOUBLE=1"
  )
endif()

add_subdirectory(boost EXCLUDE_FROM_ALL)

# Configure godot-cpp
set(GODOT_GDEXTENSION_DIR "${GODOT_GDEXTENSION_DIR}")
set(FLOAT_PRECISION "single")
set(GODOT_CPP_SYSTEM_HEADERS ON)
set(GODOT_CPP_WARNING_AS_ERROR OFF)

if(MSVC)
  set(GODOT_CPP_BINARY_DIR "${CMAKE_BINARY_DIR}/godot-cpp-windows")
  file(MAKE_DIRECTORY ${GODOT_CPP_BINARY_DIR})

  file(COPY ${CMAKE_CURRENT_SOURCE_DIR}/godot-cpp/ 
    DESTINATION ${GODOT_CPP_BINARY_DIR}
    PATTERN "CMakeLists.txt" EXCLUDE
  )

  configure_file(
    "${CMAKE_CURRENT_SOURCE_DIR}/windows/godot-cpp-windows.cmake"
    "${GODOT_CPP_BINARY_DIR}/CMakeLists.txt"
    COPYONLY
  )

  add_subdirectory(${GODOT_CPP_BINARY_DIR} ${GODOT_CPP_BINARY_DIR}/build EXCLUDE_FROM_ALL)
  
  if(TARGET godot-cpp)
    set_target_properties(godot-cpp PROPERTIES
      MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>DLL"
    )
  endif()
else()
  add_subdirectory(godot-cpp EXCLUDE_FROM_ALL)
endif()

foreach(TARGET Jolt godot-cpp)
  if(TARGET ${TARGET})
    set_target_properties(${TARGET} PROPERTIES
      ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/lib/${CMAKE_BUILD_TYPE}"
      LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/lib/${CMAKE_BUILD_TYPE}"
      RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin/${CMAKE_BUILD_TYPE}"
    )
  endif()
endforeach()
