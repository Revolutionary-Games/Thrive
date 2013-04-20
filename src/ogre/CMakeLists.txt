add_subdirectory(CellStage)
add_subdirectory(entityframework)
add_subdirectory(Agents)

add_sources(
	Thrive.h 
    Thrive.cpp
)

add_sources(
    ${CMAKE_CURRENT_SOURCE_DIR}/ogre_engine.cpp
    ${CMAKE_CURRENT_SOURCE_DIR}/ogre_engine.h
)
