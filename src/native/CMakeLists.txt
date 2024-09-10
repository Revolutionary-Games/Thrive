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

target_compile_definitions(thrive_native PRIVATE THRIVE_NATIVE_BUILD)

if(MSVC)
  target_compile_options(thrive_native PRIVATE 
    /W4 /wd4068 /wd4710 /wd4711 
  )
  if(WARNINGS_AS_ERRORS)
    target_compile_options(thrive_native PRIVATE /WX)
  endif()
else()
  target_compile_options(thrive_native PRIVATE 
    -Wall -Wextra -Wpedantic -Wno-unknown-pragmas
  )
  if(WARNINGS_AS_ERRORS)
    target_compile_options(thrive_native PRIVATE -Werror)
  endif()
endif()

target_compile_options(thrive_native PRIVATE 
  $<$<OR:$<CONFIG:Release>,$<CONFIG:Distribution>>:-DNDEBUG -O3>
)

target_link_libraries(thrive_native 
  PRIVATE Jolt
  PUBLIC Boost::intrusive Boost::circular_buffer Boost::pool
)

target_include_directories(thrive_native 
  PUBLIC 
    ${CMAKE_CURRENT_SOURCE_DIR}
    ${CMAKE_CURRENT_SOURCE_DIR}/../../third_party/concurrentqueue
)

target_precompile_headers(thrive_native 
  PUBLIC 
    "${PROJECT_BINARY_DIR}/Include.h"
    "core/ForwardDefinitions.hpp"
    "core/RefCounted.hpp"
  PRIVATE 
    "../../third_party/JoltPhysics/Jolt/Jolt.h"
)

set_target_properties(thrive_native PROPERTIES
  CXX_STANDARD 20
  CXX_STANDARD_REQUIRED ON
  CXX_EXTENSIONS OFF
)

if(THRIVE_LTO)
  set_target_properties(thrive_native PROPERTIES INTERPROCEDURAL_OPTIMIZATION ON)
endif()

install(TARGETS thrive_native)
