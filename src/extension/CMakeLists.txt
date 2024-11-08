# GDExtension for Thrive using C++.
# The non-Godot using C++ is in the "native" sibling folder.

# Note when the source code is packaged, godot-cpp (and other native)
# libraries are not included and need to be separately downloaded
# (for example from our official Thrive git repository)

add_library(thrive_extension SHARED
  "${PROJECT_BINARY_DIR}/Include.h"
  core/RegisterThriveTypes.cpp 
  core/RegisterThriveTypes.hpp
  core/ThriveConfig.cpp 
  core/ThriveConfig.hpp
  interop/ExtensionInterop.cpp 
  interop/ExtensionInterop.h
  nodes/DebugDrawer.cpp 
  nodes/DebugDrawer.hpp
  core/GodotJoltConversions.hpp
)

if(PRINT_SOURCE_FILES)
  print_source_files(thrive_extension)
endif()

configure_target(thrive_extension)

# Common compile definitions
target_compile_definitions(thrive_extension PRIVATE 
  THRIVE_NATIVE_BUILD
  _HAS_EXCEPTIONS=1
  JPH_DISABLE_CUSTOM_ALLOCATOR
  JPH_USE_SSE4_1=1
  JPH_USE_SSE4_2=1
  JPH_DOUBLE_PRECISION=1
  JPH_USE_DOUBLE=1
  $<$<CONFIG:Debug>:JPH_PROFILE_ENABLED=1>
  $<$<NOT:$<CONFIG:Debug>>:JPH_PROFILE_ENABLED=0>
  JPH_DEBUG_RENDERER=0
  JPH_FLOATING_POINT_EXCEPTIONS_ENABLED=0
  JPH_TRACK_BROADPHASE_STATS=0
  TYPED_METHOD_BIND
  _ALLOW_KEYWORD_MACROS
  GDCLASS_NO_RTTI
)

target_link_libraries(thrive_extension 
  PRIVATE 
    godot-cpp 
    Jolt
    thrive_native
)

target_include_directories(thrive_extension PUBLIC 
  ${CMAKE_CURRENT_LIST_DIR}
  ${CMAKE_CURRENT_LIST_DIR}/../native
  ${GODOT_GDEXTENSION_DIR}
  ../../third_party/godot-cpp/include
  ../../third_party/godot-cpp/gen/include
)

target_precompile_headers(thrive_extension 
  PUBLIC 
    "${PROJECT_BINARY_DIR}/Include.h"
    <cstddef>
    <cstdint>
    <atomic>
    <memory>
    <utility>
    <functional>
    <type_traits>
  PRIVATE 
    "../../third_party/godot-cpp/include/godot_cpp/classes/ref.hpp"
    "${GODOT_GDEXTENSION_DIR}/gdextension_interface.h"
)

set_target_properties(thrive_extension PROPERTIES
  CXX_STANDARD 17
  CXX_STANDARD_REQUIRED ON
  CXX_EXTENSIONS OFF
  OUTPUT_NAME "libthrive_extension"
  ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/lib/${CMAKE_BUILD_TYPE}"
  LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin/${CMAKE_BUILD_TYPE}"
  RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin/${CMAKE_BUILD_TYPE}"
  PDB_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin/${CMAKE_BUILD_TYPE}"
)

if(THRIVE_LTO)
  set_target_properties(thrive_extension PROPERTIES 
    INTERPROCEDURAL_OPTIMIZATION ON
  )
endif()

# Installation configuration
install(TARGETS thrive_extension
  RUNTIME DESTINATION "${INSTALL_BUILD_TYPE}/bin"
  LIBRARY DESTINATION "${INSTALL_BUILD_TYPE}/lib"
  ARCHIVE DESTINATION "${INSTALL_BUILD_TYPE}/lib"
)

if(MSVC AND CMAKE_BUILD_TYPE STREQUAL "Debug")
  install(FILES "$<TARGET_PDB_FILE:thrive_extension>"
    DESTINATION "${INSTALL_BUILD_TYPE}/bin"
    OPTIONAL
  )
endif()

# Create post-build directory structure
add_custom_command(TARGET thrive_extension POST_BUILD
  COMMAND ${CMAKE_COMMAND} -E make_directory 
    "${CMAKE_BINARY_DIR}/install/${THRIVE_OS}/${INSTALL_BUILD_TYPE}/bin"
  COMMAND ${CMAKE_COMMAND} -E make_directory 
    "${CMAKE_BINARY_DIR}/install/${THRIVE_OS}/${INSTALL_BUILD_TYPE}/lib"
)

# Copy files to expected location
add_custom_command(TARGET thrive_extension POST_BUILD
  COMMAND ${CMAKE_COMMAND} -E copy_if_different
    "$<TARGET_FILE:thrive_extension>"
    "${CMAKE_BINARY_DIR}/install/${THRIVE_OS}/${INSTALL_BUILD_TYPE}/bin/"
  COMMAND ${CMAKE_COMMAND} -E copy_if_different
    "$<TARGET_LINKER_FILE:thrive_extension>"
    "${CMAKE_BINARY_DIR}/install/${THRIVE_OS}/${INSTALL_BUILD_TYPE}/lib/"
)

if(MSVC AND CMAKE_BUILD_TYPE STREQUAL "Debug")
  add_custom_command(TARGET thrive_extension POST_BUILD
    COMMAND ${CMAKE_COMMAND} -E copy_if_different
      "$<TARGET_PDB_FILE:thrive_extension>"
      "${CMAKE_BINARY_DIR}/install/${THRIVE_OS}/${INSTALL_BUILD_TYPE}/bin/"
  )
endif()
