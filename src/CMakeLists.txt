add_sources(
    ${CMAKE_CURRENT_SOURCE_DIR}/game.cpp
    ${CMAKE_CURRENT_SOURCE_DIR}/game.h
)

add_subdirectory(common)
add_subdirectory(engine)
add_subdirectory(ogre)
add_subdirectory(scripting)
add_subdirectory(util)
add_subdirectory(bullet)
