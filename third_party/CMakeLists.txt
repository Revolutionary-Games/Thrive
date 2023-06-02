# Native third party module references

# JoltPhysics

# Might as well get used to double precision impact now as we'll want that eventually anyway
set(DOUBLE_PRECISION ON)

# TODO: do we need to turn this off (requiring too new AMD CPUs)?
set(USE_AVX2 ON)

# This is way too unsupported to enable in a general build
set(USE_AVX512 OFF)

# Need to force Jolt to be position independent to allow linking into shared
# libraries
set(CMAKE_POSITION_INDEPENDENT_CODE ON)

add_subdirectory(JoltPhysics/Build EXCLUDE_FROM_ALL)

# Boost libraries
add_subdirectory(boost EXCLUDE_FROM_ALL)
