# Native third party module references

# JoltPhysics

# Might as well get used to double precision impact now as we'll want that eventually anyway
set(DOUBLE_PRECISION ON)

# TODO: do we need to turn this off (requiring too new AMD CPUs)?
set(USE_AVX2 ON)

# This is way too unsupported to enable in a general build
set(USE_AVX512 OFF)

add_subdirectory(JoltPhysics/Build)
