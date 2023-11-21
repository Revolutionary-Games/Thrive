# Main library of the native side of things (non-Godot using)
add_library(thrive_native SHARED
  "${PROJECT_BINARY_DIR}/Include.h" core/ForwardDefinitions.hpp
  interop/CInterop.cpp interop/CInterop.h
  interop/CStructures.h interop/JoltTypeConversions.hpp
  core/Logger.cpp core/Logger.hpp
  core/Math.hpp
  core/Mutex.hpp
  core/NonCopyable.hpp core/Reference.hpp
  core/RefCounted.hpp
  core/Spinlock.hpp
  core/TaskSystem.cpp core/TaskSystem.hpp
  core/Time.hpp
  physics/BodyActivationListener.cpp physics/BodyActivationListener.hpp
  physics/BodyControlState.hpp
  physics/ContactListener.cpp physics/ContactListener.hpp
  physics/CustomConstraintTypes.hpp
  physics/Layers.hpp
  physics/PhysicalWorld.cpp physics/PhysicalWorld.hpp
  physics/PhysicsBody.cpp physics/PhysicsBody.hpp
  physics/ShapeCreator.cpp physics/ShapeCreator.hpp
  physics/ShapeWrapper.cpp physics/ShapeWrapper.hpp
  physics/SimpleShapes.cpp physics/SimpleShapes.hpp
  physics/TrackedConstraint.cpp physics/TrackedConstraint.hpp
  physics/StepListener.cpp physics/StepListener.hpp
  physics/DebugDrawForwarder.cpp physics/DebugDrawForwarder.hpp
  physics/PhysicsCollision.hpp
  physics/PhysicsRayWithUserData.hpp
  physics/ArrayRayCollector.hpp)

if("${CMAKE_CXX_COMPILER_ID}" STREQUAL "MSVC")
  target_compile_options(thrive_native PRIVATE /W4 /wd4068)

  if(WARNINGS_AS_ERRORS)
    target_compile_options(thrive_native PRIVATE /WX)
  endif()

  # TODO debug symbols in release for MSVC (and Distribution mode)

else()
  target_compile_options(thrive_native PRIVATE -Wall -Wextra -Wpedantic -Werror
    -Wno-unknown-pragmas)

  if(WARNINGS_AS_ERRORS)
    target_compile_options(thrive_native PRIVATE -Werror)
  endif()

  target_compile_options(thrive_native PRIVATE $<$<CONFIG:Release>:-g -O3>
    $<$<CONFIG:Distribution>:-g -O3>)
endif()

target_compile_definitions(thrive_native PRIVATE THRIVE_NATIVE_BUILD)

target_link_libraries(thrive_native PRIVATE Jolt)
target_link_libraries(thrive_native PUBLIC Boost::intrusive
  Boost::circular_buffer Boost::pool)

target_include_directories(thrive_native PUBLIC ${CMAKE_CURRENT_LIST_DIR})

# TODO: the one private precompiled header will probably cause issues with
# other libraries linking to this
target_precompile_headers(thrive_native PUBLIC "${PROJECT_BINARY_DIR}/Include.h"
  "core/ForwardDefinitions.hpp" "core/RefCounted.hpp"
  PRIVATE "../../third_party/JoltPhysics/Jolt/Jolt.h")

set_target_properties(thrive_native PROPERTIES
  CXX_STANDARD 20
  CXX_EXTENSIONS OFF)

if(THRIVE_LTO)
  # TODO: figure out why msys2 fails with this or disable this by default there
  set_target_properties(thrive_native PROPERTIES INTERPROCEDURAL_OPTIMIZATION ON)
  message(STATUS "LTO is on for Thrive")
endif()

install(TARGETS thrive_native)
