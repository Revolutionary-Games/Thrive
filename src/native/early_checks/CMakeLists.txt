# Early load library that allows checking things before loading the main library,
# which causes global constructors to run that may already do some complex stuff
add_library(early_checks SHARED
  EarlyInterop.h EarlyInterop.cpp
  ../helpers/CPUCheck.hpp
  ../helpers/CPUCheckResult.h)

# Make the library runnable on really old CPUs (and turn on extra warnings)
if("${CMAKE_CXX_COMPILER_ID}" STREQUAL "MSVC")
  # TODO: really old CPU support for MSVC

  target_compile_options(early_checks PRIVATE /W4 /wd4068)

  if(WARNINGS_AS_ERRORS)
    target_compile_options(early_checks PRIVATE /WX)
  endif()

  # Probably really don't need to support debug symbols in release for MSVC
else()
  # Sadly this needs to specify sandy bridge to get xgetbv working, but that
  # is only called if AVX support is already detected, so this should be fine
  # for older CPUs. Nehalem is the minimum Thrive can work with but when SSE
  # 4.2 support is not detected the slower library is used anyway and the CPU
  # check won't have detected AVX.
  target_compile_options(early_checks PRIVATE -march=sandybridge)

  target_compile_options(early_checks PRIVATE -Wall -Wextra -Wpedantic -Wno-unknown-pragmas)

  if(WARNINGS_AS_ERRORS)
    target_compile_options(early_checks PRIVATE -Werror)
  endif()

  # For now, no debug symbols as it should be impossible to crash in this
  # simple library
  # For some reason this needs to define NDEBUG explicitly to work (Thrive
  # library gets the NDEBUG automatically from somewhere)
  # target_compile_options(early_checks PRIVATE $<$<CONFIG:Release>:-g -O3>
  #   $<$<CONFIG:Distribution>:-g -O3>)
  target_compile_options(early_checks PRIVATE $<$<CONFIG:Release>:-DNDEBUG -O3>
      $<$<CONFIG:Distribution>:-DNDEBUG -O3>)
endif()

target_compile_definitions(early_checks PRIVATE EARLY_CHECK_BUILD)

set_target_properties(early_checks PROPERTIES
  CXX_STANDARD 20
  CXX_EXTENSIONS OFF)

install(TARGETS early_checks)
