# Native third party module references

# JoltPhysics

# TODO: option for turning JPH_TRACK_BROADPHASE_STATS on for debugging

# Might as well get used to double precision impact now as we'll want that eventually anyway
# In Jolt
set(DOUBLE_PRECISION ON)

if(THRIVE_AVX)
  set(USE_SSE4_1 ON)
  set(USE_SSE4_2 ON)
  set(USE_AVX ON)
  set(USE_AVX2 ON)

  # Not directly AVX instructions, but are about as new so are only on when AVX is on
  set(USE_F16C ON)
  set(USE_LZCNT ON)
  set(USE_TZCNT ON)

  # this is off anyway as this causes indeterminism in the simulation
  set(USE_FMADD OFF)
else()
  set(USE_SSE4_1 OFF)
  set(USE_SSE4_2 OFF)
  set(USE_AVX OFF)
  set(USE_AVX2 OFF)
  set(USE_F16C OFF)
  set(USE_FMADD OFF)
  set(USE_LZCNT OFF)
  set(USE_TZCNT OFF)
endif()


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

add_subdirectory(JoltPhysics/Build EXCLUDE_FROM_ALL)

# Force Jolt library to use the right MSVC runtime (this is for some reason required as 
# the above library set doesn't work fully to override things)
# Could be just a case of CMake policy CMP0091 not being set to new
if(MSVC)
  target_compile_options(Jolt PRIVATE -MD$<$<CONFIG:Debug>:d>)

  # Need to force off this Jolt error which is triggered when compiling
  # thrive_native otherwise
  target_compile_options(Jolt PUBLIC /wd4714)
endif()

# Boost libraries
add_subdirectory(boost EXCLUDE_FROM_ALL)

# gdextension base module
add_subdirectory(godot-cpp EXCLUDE_FROM_ALL)
