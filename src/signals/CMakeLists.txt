
add_sources(
    ${CMAKE_CURRENT_SOURCE_DIR}/connection.h 
    ${CMAKE_CURRENT_SOURCE_DIR}/connection.cpp 
    ${CMAKE_CURRENT_SOURCE_DIR}/guardian.h 
    ${CMAKE_CURRENT_SOURCE_DIR}/guardian.cpp 
    ${CMAKE_CURRENT_SOURCE_DIR}/signal.h 
)

add_test_sources(
    ${CMAKE_CURRENT_SOURCE_DIR}/tests/signal.cpp 
)
