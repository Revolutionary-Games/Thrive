# Native third party module references

# JoltPhysics

# TODO: option for turning JPH_TRACK_BROADPHASE_STATS on for debugging

include(ProcessorCount)
ProcessorCount(CPU_CORES)
if(CPU_CORES EQUAL 0)
  set(CPU_CORES 1)
elseif(CPU_CORES GREATER 4)
  math(EXPR CPU_CORES "${CPU_CORES} - 1")
endif()

if(CMAKE_SIZEOF_VOID_P EQUAL 8)
# Might as well get used to double precision impact now as we'll want that eventually anyway
# In Jolt
  set(DOUBLE_PRECISION ON)
else()
  set(DOUBLE_PRECISION ON)
endif()

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

# Try to tell Jolt to use the DLL friendly runtime library
set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>DLL")

# Make sure this other option doesn't accidentally trigger in Jolt
set(USE_STATIC_MSVC_RUNTIME_LIBRARY OFF)

# We need RTTI to be able to link without missing symbols when Jolt is built
set(CPP_RTTI_ENABLED ON)

# Core settings
set(CMAKE_POSITION_INDEPENDENT_CODE ON)
set(USE_STATIC_MSVC_RUNTIME_LIBRARY OFF)
set(CPP_RTTI_ENABLED ON)

set(BUILD_SHARED_LIBS OFF)
set(TARGET_UNIT_TESTS OFF)
set(TARGET_VIEWER OFF)
set(TARGET_SAMPLES OFF)
set(TARGET_HELLO_WORLD OFF)
set(TARGET_PERFORMANCE_TEST OFF)

if(WIN32 AND MSVC)
  # Compiler options and flags
  add_compile_options(/nologo /MP${CPU_CORES})
  string(APPEND CMAKE_CXX_FLAGS " /nologo")
  string(APPEND CMAKE_C_FLAGS " /nologo")

  # Force MDd/MD runtime for Jolt
  string(REPLACE "/MT" "/MD" CMAKE_CXX_FLAGS_DEBUG "${CMAKE_CXX_FLAGS_DEBUG}")
  string(REPLACE "/MT" "/MD" CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE}")
  string(REPLACE "/MT" "/MD" CMAKE_C_FLAGS_DEBUG "${CMAKE_C_FLAGS_DEBUG}")
  string(REPLACE "/MT" "/MD" CMAKE_C_FLAGS_RELEASE "${CMAKE_C_FLAGS_RELEASE}")

  add_compile_definitions(
    JPH_PROFILE_ENABLED=0
    JPH_DEBUG_RENDERER=0
    JPH_FLOATING_POINT_EXCEPTIONS_ENABLED=0
    JPH_USE_SSE4_1=1
    JPH_USE_SSE4_2=1
    JPH_TRACK_BROADPHASE_STATS=0
    JPH_STATISTICS=0
    JPH_DISABLE_CUSTOM_ALLOCATOR
  )

  add_compile_options(
    /EHsc  # Enable C++ exception handling
    /wd4574 # Disable C4574 warning
    /WX-    # Don't treat warnings as errors
    /MD$<$<CONFIG:Debug>:d>  # Force MD/MDd runtime
  )
endif()

add_subdirectory(JoltPhysics/Build EXCLUDE_FROM_ALL)

if(TARGET Jolt AND MSVC)
  # Force MDd/MD runtime for Jolt target specifically
  target_compile_options(Jolt PRIVATE 
    /MD$<$<CONFIG:Debug>:d>
    $<$<CONFIG:Debug>:/MDd>
    $<$<NOT:$<CONFIG:Debug>>:/MD>
  )
endif()

# Boost libraries
add_subdirectory(boost EXCLUDE_FROM_ALL)

# Configure godot-cpp settings
set(GODOT_GDEXTENSION_DIR "${GODOT_GDEXTENSION_DIR}")
set(FLOAT_PRECISION "single")
set(GODOT_CPP_SYSTEM_HEADERS ON)
set(GODOT_CPP_WARNING_AS_ERROR OFF)

if(WIN32 AND MSVC)
  set(GODOT_CPP_BINARY_DIR "${CMAKE_BINARY_DIR}/godot-cpp-windows")
  file(MAKE_DIRECTORY ${GODOT_CPP_BINARY_DIR})
  file(COPY ${CMAKE_CURRENT_SOURCE_DIR}/godot-cpp/ DESTINATION ${GODOT_CPP_BINARY_DIR}
    PATTERN "CMakeLists.txt" EXCLUDE
  )

  configure_file(
    "${CMAKE_CURRENT_SOURCE_DIR}/windows/godot-cpp-windows.cmake"
    "${GODOT_CPP_BINARY_DIR}/CMakeLists.txt"
    COPYONLY
  )

  add_subdirectory(${GODOT_CPP_BINARY_DIR} ${GODOT_CPP_BINARY_DIR}/build EXCLUDE_FROM_ALL)
else()
  # gdextension base module
  add_subdirectory(godot-cpp EXCLUDE_FROM_ALL)
endif()
