message(STATUS "todo: remake the tests")

include_directories("catch")
include_directories("../src")

include_directories("${LEVIATHAN_SRC}")

set(CurrentProjectName ThriveTest)
set(AllProjectFiles
  "test_main.cpp"
  "test_script_compile.cpp"
  "test_simulation_parameters.cpp"
  "test_clouds.cpp"

  # LeviathanTest support files
  "${LEVIATHAN_SRC}/LeviathanTest/PartialEngine.h"
  "${LEVIATHAN_SRC}/LeviathanTest/PartialEngine.cpp"
  
  "${LEVIATHAN_SRC}/LeviathanTest/DummyLog.cpp"
  "${LEVIATHAN_SRC}/LeviathanTest/DummyLog.cpp"
  )

set(CREATE_CONSOLE_APP ON)
include(LeviathanUsingProject)

# The project is now defined
target_link_libraries(ThriveTest ThriveLib)



if(WIN32)
  # TODO: debug target (can't use the linux approach as visual studio
  # can switch between the build types without invoking cmake)
  add_custom_target(check COMMAND "${PROJECT_BINARY_DIR}/bin/ThriveTest.exe"
    DEPENDS ThriveTest
    WORKING_DIRECTORY "${PROJECT_BINARY_DIR}/bin")
else()
  if(CMAKE_BUILD_TYPE STREQUAL "Debug")
    add_custom_target(check COMMAND "${PROJECT_BINARY_DIR}/bin/ThriveTestD"
      DEPENDS ThriveTest
      WORKING_DIRECTORY "${PROJECT_BINARY_DIR}/bin")
  else()
    add_custom_target(check COMMAND "${PROJECT_BINARY_DIR}/bin/ThriveTest"
      DEPENDS ThriveTest
      WORKING_DIRECTORY "${PROJECT_BINARY_DIR}/bin")
  endif()
endif()
