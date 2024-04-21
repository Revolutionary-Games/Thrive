# Native third party module references

# JoltPhysics

# TODO: option for turning JPH_TRACK_BROADPHASE_STATS on for debugging

# Might as well get used to double precision impact now as we'll want that eventually anyway
set(DOUBLE_PRECISION ON)

if(THRIVE_AVX)
  set(USE_AVX ON)
  set(USE_AVX2 ON)
else()
  set(USE_AVX OFF)
  set(USE_AVX2 OFF)
endif()

set(USE_SSE4_1 ON)
set(USE_SSE4_2 ON)

# This is way too unsupported to enable in a general build
set(USE_AVX512 OFF)

# Need to force Jolt to be position independent to allow linking into shared
# libraries
set(CMAKE_POSITION_INDEPENDENT_CODE ON)

add_subdirectory(JoltPhysics/Build EXCLUDE_FROM_ALL)

# Boost libraries
add_subdirectory(boost EXCLUDE_FROM_ALL)
