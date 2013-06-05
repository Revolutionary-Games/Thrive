add_sources(
    ${CMAKE_CURRENT_SOURCE_DIR}/game.cpp
    ${CMAKE_CURRENT_SOURCE_DIR}/game.h
)

add_subdirectory(bullet)
add_subdirectory(engine)
add_subdirectory(microbe_stage)
add_subdirectory(ogre)
add_subdirectory(scripting)
add_subdirectory(sound)
add_subdirectory(util)
