# Main library of the native side of things (non-Godot using)
# The Godot using GDExtension code is in the "extension" sibling folder to this
# Due to linking path etc. requirements, that library cannot easily link to
# this which is why it is in a separate folder to clearly tell what it can
# easily call

# Note when the source code is packaged, boost (and other native)
# libraries are not included and need to be separately downloaded
# (for example from our official Thrive git repository)

add_library(thrive_native SHARED
  "${PROJECT_BINARY_DIR}/Include.h"
  core/ForwardDefinitions.hpp
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
  helpers/CPUCheck.hpp
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
  physics/ArrayRayCollector.hpp
  core/NativeLibIntercommunication.hpp
  shared/IntercommunicationManager.cpp core/IntercommunicationManager.hpp)

if(PRINT_SOURCE_FILES)
  print_source_files(thrive_native)
endif()

configure_target(thrive_native)

# Platform-independent compile definitions
target_compile_definitions(thrive_native PRIVATE 
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
)

target_link_libraries(thrive_native 
  PRIVATE Jolt 
  PUBLIC 
    Boost::intrusive
    Boost::circular_buffer 
    Boost::pool
)

target_include_directories(thrive_native PUBLIC 
  ${CMAKE_CURRENT_LIST_DIR}
  ../../third_party/concurrentqueue
  ../../third_party/JoltPhysics
)

# TODO: the one private precompiled header will probably cause issues with
# other libraries linking to this
target_precompile_headers(thrive_native 
  PUBLIC 
    "${PROJECT_BINARY_DIR}/Include.h"
    "core/ForwardDefinitions.hpp" 
    "core/RefCounted.hpp"
    <cstddef>
    <cstdint>
    <atomic>
    <memory>
    <utility>
    <type_traits>
  PRIVATE 
    "../../third_party/JoltPhysics/Jolt/Jolt.h"
)

set_target_properties(thrive_native PROPERTIES
  CXX_STANDARD 20
  CXX_STANDARD_REQUIRED ON
  CXX_EXTENSIONS OFF
  OUTPUT_NAME "libthrive_native"
  ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/lib/${CMAKE_BUILD_TYPE}"
  LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin/${CMAKE_BUILD_TYPE}"
  RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin/${CMAKE_BUILD_TYPE}"
  PDB_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin/${CMAKE_BUILD_TYPE}"
)

if(THRIVE_LTO)
  # TODO: figure out why msys2 fails with this or disable this by default there
  set_target_properties(thrive_native PROPERTIES 
    INTERPROCEDURAL_OPTIMIZATION ON
  )
  message(STATUS "LTO is on for ThriveNative")
endif()

# Release/RelWithDebInfo installation
install(TARGETS thrive_native
  RUNTIME DESTINATION "${INSTALL_BUILD_TYPE}/bin"
  LIBRARY DESTINATION "${INSTALL_BUILD_TYPE}/lib"
  ARCHIVE DESTINATION "${INSTALL_BUILD_TYPE}/lib"
)

if(MSVC AND CMAKE_BUILD_TYPE STREQUAL "Debug")
  install(FILES "$<TARGET_PDB_FILE:thrive_native>"
    DESTINATION "${INSTALL_BUILD_TYPE}/bin"
    OPTIONAL
  )
endif()

# Create post-build directory structure
add_custom_command(TARGET thrive_native POST_BUILD
  COMMAND ${CMAKE_COMMAND} -E make_directory 
    "${CMAKE_BINARY_DIR}/install/${THRIVE_OS}/${INSTALL_BUILD_TYPE}/bin"
  COMMAND ${CMAKE_COMMAND} -E make_directory 
    "${CMAKE_BINARY_DIR}/install/${THRIVE_OS}/${INSTALL_BUILD_TYPE}/lib"
)

# Copy files to expected location
add_custom_command(TARGET thrive_native POST_BUILD
  COMMAND ${CMAKE_COMMAND} -E copy_if_different
    "$<TARGET_FILE:thrive_native>"
    "${CMAKE_BINARY_DIR}/install/${THRIVE_OS}/${INSTALL_BUILD_TYPE}/bin/"
  COMMAND ${CMAKE_COMMAND} -E copy_if_different
    "$<TARGET_LINKER_FILE:thrive_native>"
    "${CMAKE_BINARY_DIR}/install/${THRIVE_OS}/${INSTALL_BUILD_TYPE}/lib/"
)

if(MSVC AND CMAKE_BUILD_TYPE STREQUAL "Debug")
  add_custom_command(TARGET thrive_native POST_BUILD
    COMMAND ${CMAKE_COMMAND} -E copy_if_different
      "$<TARGET_PDB_FILE:thrive_native>"
      "${CMAKE_BINARY_DIR}/install/${THRIVE_OS}/${INSTALL_BUILD_TYPE}/bin/"
  )
endif()
