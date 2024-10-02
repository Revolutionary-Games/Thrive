# GDExtension for Thrive using C++.
# The non-Godot using C++ is in the "native" sibling folder.

# Note when the source code is packaged, godot-cpp (and other native)
# libraries are not included and need to be separately downloaded
# (for example from our official Thrive git repository)

add_library(thrive_extension SHARED
  "${PROJECT_BINARY_DIR}/Include.h"
  core/RegisterThriveTypes.cpp core/RegisterThriveTypes.hpp
  core/xatlas.cpp core/xatlas.h
  core/atlas_unwrap.cpp core/atlas_unwrap.hpp
  core/ThriveConfig.cpp core/ThriveConfig.hpp
  interop/ExtensionInterop.cpp interop/ExtensionInterop.h
  nodes/DebugDrawer.cpp nodes/DebugDrawer.hpp
  core/GodotJoltConversions.hpp
)

if(MSVC)
  target_compile_options(thrive_extension PRIVATE /W4 /wd4068)

  if(WARNINGS_AS_ERRORS)
    target_compile_options(thrive_extension PRIVATE /WX)
  endif()

  # TODO debug symbols in release for MSVC (and Distribution mode)

else()
  target_compile_options(thrive_extension PRIVATE -Wall -Wextra -Wpedantic
    -Wno-unknown-pragmas)

  if(WARNINGS_AS_ERRORS)
    target_compile_options(thrive_extension PRIVATE -Werror)
  endif()

  target_compile_options(thrive_extension PRIVATE
    $<$<OR:$<CONFIG:Release>,$<CONFIG:Distribution>>:-DNDEBUG -O3>)
endif()

target_link_libraries(thrive_extension PRIVATE godot-cpp Jolt)

target_include_directories(thrive_extension PUBLIC ${CMAKE_CURRENT_LIST_DIR}
  ${CMAKE_CURRENT_LIST_DIR}/../native)

target_precompile_headers(thrive_extension PUBLIC "${PROJECT_BINARY_DIR}/Include.h"
  PRIVATE "../../third_party/godot-cpp/include/godot_cpp/classes/ref.hpp"
  "${GODOT_GDEXTENSION_DIR}/gdextension_interface.h"
)

set_target_properties(thrive_extension PROPERTIES
  CXX_STANDARD 20
  CXX_STANDARD_REQUIRED ON
  CXX_EXTENSIONS OFF)

if(THRIVE_LTO)
  message(STATUS "Enabling LTO for thrive_extension")
  set_target_properties(thrive_extension PROPERTIES INTERPROCEDURAL_OPTIMIZATION ON)
endif()

install(TARGETS thrive_extension)
