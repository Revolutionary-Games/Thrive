
add_sources(
    ${CMAKE_CURRENT_SOURCE_DIR}/component.cpp
    ${CMAKE_CURRENT_SOURCE_DIR}/component.h 
    ${CMAKE_CURRENT_SOURCE_DIR}/component_collection.cpp 
    ${CMAKE_CURRENT_SOURCE_DIR}/component_collection.h 
    ${CMAKE_CURRENT_SOURCE_DIR}/engine.cpp
    ${CMAKE_CURRENT_SOURCE_DIR}/engine.h
    ${CMAKE_CURRENT_SOURCE_DIR}/entity.cpp
    ${CMAKE_CURRENT_SOURCE_DIR}/entity.h
    ${CMAKE_CURRENT_SOURCE_DIR}/property.cpp 
    ${CMAKE_CURRENT_SOURCE_DIR}/property.h 
    ${CMAKE_CURRENT_SOURCE_DIR}/scripting.cpp 
    ${CMAKE_CURRENT_SOURCE_DIR}/scripting.h 
    ${CMAKE_CURRENT_SOURCE_DIR}/system.cpp
    ${CMAKE_CURRENT_SOURCE_DIR}/system.h
)

add_test_sources(
    ${CMAKE_CURRENT_SOURCE_DIR}/tests/component.cpp 
    ${CMAKE_CURRENT_SOURCE_DIR}/tests/engine.cpp 
    ${CMAKE_CURRENT_SOURCE_DIR}/tests/test_component.h
)
