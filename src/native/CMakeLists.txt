# Main library of the native side of things (non-Godot using)
add_library(thrive_native SHARED
  interop/CInterop.h interop/CInterop.cpp
  interop/CStructures.h
  physics/PhysicalWorld.hpp physics/PhysicalWorld.cpp
  "${PROJECT_BINARY_DIR}/Include.h"
  )

target_compile_definitions(thrive_native PRIVATE THRIVE_NATIVE_BUILD)

target_link_libraries(thrive_native PRIVATE Jolt)
# target_link_libraries(thrive_native PRIVATE stuff)

target_include_directories(thrive_native PUBLIC ${CMAKE_CURRENT_LIST_DIR})

# target_include_directories(thrive_native PUBLIC "../../third_party/JoltPhysics/")

target_precompile_headers(thrive_native PUBLIC "${PROJECT_BINARY_DIR}/Include.h"
  PRIVATE "../../third_party/JoltPhysics/Jolt/Jolt.h")

set_target_properties(thrive_native PROPERTIES
  CXX_STANDARD 17
  CXX_EXTENSIONS OFF
  INTERPROCEDURAL_OPTIMIZATION ON
  )

message(STATUS "TODO: static standard lib for easier distributing")
# # Static standard lib
# target_link_libraries(thrive_native PRIVATE -static-libgcc -static-libstdc++)

# # Fully static executable
# if(CMAKE_SYSTEM_NAME STREQUAL "Windows")
#   target_link_libraries(thrive_native PRIVATE -static)
# endif()

if(NOT WIN32)
  set_target_properties(thrive_native PROPERTIES LINK_FLAGS_RELEASE "-fuse-ld=gold")
else()
  set_target_properties(thrive_native PROPERTIES LINK_FLAGS_RELEASE "")
endif()

install(TARGETS thrive_native)
