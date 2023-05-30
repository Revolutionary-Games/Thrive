# Native third party module references
message(STATUS "TODO: do these native third party modules")

# TODO: do we need to turn this off (requiring too new AMD CPUs)?
set(USE_AVX2 ON)

add_subdirectory(JoltPhysics/Build)
