

# Add source files to the list
set(GROUP_BULLET 
  # "bullet/bullet_ogre_conversion.h"
  # "bullet/bullet_to_ogre_system.cpp"
  # "bullet/bullet_to_ogre_system.h"
  # "bullet/collision_shape.cpp"
  # "bullet/collision_shape.h"
  # "bullet/debug_drawing.cpp"
  # "bullet/debug_drawing.h"
  # "bullet/rigid_body_system.cpp"
  # "bullet/rigid_body_system.h"
  # "bullet/update_physics_system.cpp"
  # "bullet/update_physics_system.h"
  # "bullet/collision_system.cpp"
  # "bullet/collision_system.h"
  # "bullet/collision_filter.cpp"
  # "bullet/collision_filter.h"
  # "bullet/physical_world.cpp"
  # "bullet/physical_world.h"
  )

set(GROUP_ENGINE
  # "engine/component.cpp"
  # "engine/component.h"
  # "engine/component_collection.cpp"
  # "engine/component_collection.h"
  # "engine/component_factory.cpp"
  # "engine/component_factory.h"
  # "engine/engine.cpp"
  # "engine/engine.h"
  # "engine/entity.cpp"
  "engine/typedefs.h"
  # "engine/entity.h"
  # "engine/entity_filter.h"
  # "engine/entity_manager.cpp"
  # "engine/entity_manager.h"
  # "engine/game_state.cpp"
  # "engine/game_state.h"
  "engine/player_data.cpp"
  "engine/player_data.h"
  # "engine/rng.cpp"
  # "engine/rng.h"
  # "engine/rolling_grid.cpp"
  # "engine/rolling_grid.h"
  "engine/serialization.cpp"
  "engine/serialization.h"
  # "engine/system.cpp"
  # "engine/system.h"
  # "engine/touchable.cpp"
  # "engine/touchable.h"


  # "engine/component.cpp"
  # "engine/component.h"
  # "engine/component_collection.cpp"
  # "engine/component_collection.h"
  # "engine/component_factory.cpp"
  # "engine/component_factory.h"
  # "engine/engine.cpp"
  # "engine/engine.h"
  # "engine/entity.cpp"
  # "engine/entity.h"
  # "engine/entity_filter.h"
  # "engine/entity_manager.cpp"
  # "engine/entity_manager.h"
  # "engine/game_state.cpp"
  # "engine/game_state.h"
  # "engine/player_data.cpp"
  # "engine/player_data.h"
  # "engine/rng.cpp"
  # "engine/rng.h"
  # "engine/rolling_grid.cpp"
  # "engine/rolling_grid.h"
  # "engine/serialization.cpp"
  # "engine/serialization.h"
  # "engine/system.cpp"
  # "engine/system.h"
  # "engine/touchable.cpp"
  # "engine/touchable.h"
  )

set(GROUP_GENERAL
  "general/timed_life_system.cpp"
  "general/timed_life_system.h"
  "general/locked_map.cpp"
  "general/locked_map.h"
  "general/hex.cpp"
  "general/hex.h"
  # "general/powerup_system.cpp"
  # "general/powerup_system.h"
  "general/perlin_noise.cpp"
  "general/perlin_noise.h"
  "general/thrive_math.cpp"
  "general/thrive_math.h"
  )

set(GROUP_MICROBE_STAGE
  "microbe_stage/agent_cloud_system.cpp"
  "microbe_stage/agent_cloud_system.h"
  "microbe_stage/bacteria_types.cpp"
  "microbe_stage/bacteria_types.h"
  "microbe_stage/biome_controller.cpp"
  "microbe_stage/biome_controller.h"
  "microbe_stage/biomes.cpp"
  "microbe_stage/biomes.h"
  "microbe_stage/bioprocesses.cpp"
  "microbe_stage/bioprocesses.h"
  "microbe_stage/compound_absorber_system.cpp"
  "microbe_stage/compound_absorber_system.h"
  "microbe_stage/compound_cloud_system.cpp"
  "microbe_stage/compound_cloud_system.h"
  "microbe_stage/compounds.cpp"
  "microbe_stage/compounds.h"
  "microbe_stage/generate_cell_stage_world.rb"
  "microbe_stage/membrane_system.cpp"
  "microbe_stage/membrane_system.h"
  "microbe_stage/microbe_camera_system.cpp"
  "microbe_stage/microbe_camera_system.h"
  "microbe_stage/organelle_types.cpp"
  "microbe_stage/organelle_types.h"
  "microbe_stage/process_system.cpp"
  "microbe_stage/process_system.h"
  "microbe_stage/simulation_parameters.cpp"
  "microbe_stage/simulation_parameters.h"
  "microbe_stage/spawn_system.cpp"
  "microbe_stage/spawn_system.h"
  "microbe_stage/species.cpp"
  "microbe_stage/species.h"
  "microbe_stage/species_component.cpp"
  "microbe_stage/species_component.h"
  "microbe_stage/species_name_controller.cpp"
  "microbe_stage/species_name_controller.h"
  "microbe_stage/player_microbe_control.h"
  "microbe_stage/player_microbe_control.cpp"
  )

set_source_files_properties("microbe_stage/generate_cell_stage_world.rb"
  PROPERTIES HEADER_FILE_ONLY TRUE)


set(GROUP_OGRE
  # "ogre/camera_system.cpp"
  # "ogre/camera_system.h"
  # "ogre/colour_material.cpp"
  # "ogre/colour_material.h"
  # "ogre/keyboard.cpp"
  # "ogre/keyboard.h"
  # "ogre/light_system.cpp"
  # "ogre/light_system.h"
  # "ogre/mouse.cpp"
  # "ogre/mouse.h"
  # "ogre/render_system.cpp"
  # "ogre/render_system.h"
  # "ogre/scene_node_system.cpp"
  # "ogre/scene_node_system.h"
  # "ogre/sky_system.cpp"
  # "ogre/sky_system.h"
  # "ogre/workspace_system.h"
  # "ogre/workspace_system.cpp"
  )

set(GROUP_SCRIPTING
  # "scripting/luajit.h"
  # "scripting/script_entity_filter.cpp"
  # "scripting/script_entity_filter.h"
  # "scripting/script_initializer.cpp"
  # "scripting/script_initializer.h"
  # "scripting/script_wrapper.h"
  # "scripting/wrapper_classes.cpp"
  # "scripting/wrapper_classes.h"
  # "scripting/script_helpers.h"
  )

set(GROUP_SOUND
  # "sound/sound_source_system.cpp"
  # "sound/sound_source_system.h"
  # "sound/caudio_include.h"
  # "sound/node_attachable.h"
  # "sound/node_attachable.cpp"
  # "sound/sound_emitter.h"
  # "sound/sound_emitter.cpp"
  # "sound/sound_listener.cpp"
  # "sound/sound_listener.h"
  # "sound/sound_manager.cpp"
  # "sound/sound_manager.h"
  # "sound/sound_memory_stream.cpp"
  # "sound/sound_memory_stream.h"
  )
  
set(GROUP_UTIL
  "util/pair_hash.h"
  )


add_custom_command(OUTPUT "${PROJECT_SOURCE_DIR}/src/generated/cell_stage_world.h"
  "${PROJECT_SOURCE_DIR}/src/generated/cell_stage_world.cpp"
  "${PROJECT_SOURCE_DIR}/src/generated/cell_stage_bindings.h"
  COMMAND "ruby" "${PROJECT_SOURCE_DIR}/src/microbe_stage/generate_cell_stage_world.rb"
  "${PROJECT_SOURCE_DIR}/src/generated/cell_stage_world"
  "${PROJECT_SOURCE_DIR}/src/generated/cell_stage_bindings.h"
  DEPENDS "${PROJECT_SOURCE_DIR}/src/microbe_stage/generate_cell_stage_world.rb"
  "${PROJECT_SOURCE_DIR}/ThirdParty/leviathan/Helpers/FileGen.rb"
  )

set(GROUP_MICROBE
  "generated/cell_stage_world.h" "generated/cell_stage_world.cpp"
  "generated/cell_stage_bindings.h"
  )

# Menu things
set(GROUP_MENU
  "main_menu_keypresses.cpp"
  "main_menu_keypresses.h"
  )


# Groups for better visual studio experience
source_group("bullet" FILES ${GROUP_BULLET})
source_group("engine" FILES ${GROUP_ENGINE})
source_group("general" FILES ${GROUP_GENERAL})
source_group("gui" FILES ${GROUP_GUI})
source_group("microbe_stage" FILES ${GROUP_MICROBE_STAGE})
source_group("ogre" FILES ${GROUP_OGRE})
source_group("scripting" FILES ${GROUP_SCRIPTING})
source_group("sound" FILES ${GROUP_SOUND})
source_group("util" FILES ${GROUP_UTIL})

source_group("menu_code" FILES ${GROUP_MENU})

source_group("microbe" FILES ${GROUP_MICROBE})



set(THRIVE_FILES ${GROUP_BULLET} ${GROUP_ENGINE}
  ${GROUP_GENERAL} ${GROUP_GUI} ${GROUP_MICROBE_STAGE}
  ${GROUP_OGRE} ${GROUP_SCRIPTING} ${GROUP_SOUND}
  ${GROUP_UTIL} ${GROUP_MICROBE}
  ${GROUP_MENU}
  )

# Leviathan program setup

set(BaseProgramName "Thrive")
set(BaseIncludeFileName "thrive_version.h")
set(BaseSubFolder "src")

# Set all the settings
set(ProgramIncludesHeader "${BaseIncludeFileName}")
set(ProgramAppHeader "ThriveGame.h")

set(WorldFactoryClass "ThriveWorldFactory")
set(WorldFactoryInclude "thrive_world_factory.h")


# ------------------ ProgramConfiguration ------------------ #
set(PROGRAMCLASSNAME				ThriveGame)
set(PROGRAMLOG						Thrive)
set(PROGRAMCONFIGURATION			"./ThriveGame.conf")
set(PROGRAMKEYCONFIGURATION			"./ThriveKeybindings.conf")
set(PROGRAMCHECKCONFIGFUNCNAME		"ThriveGame::CheckGameConfigurationVariables")
set(PROGRAMCHECKKEYCONFIGFUNCNAME	"ThriveGame::CheckGameKeyConfigVariables")
set(WINDOWTITLEGENFUNCTION			"ThriveGame::GenerateWindowTitle()")
set(USERREADABLEIDENTIFICATION		"\"Thrive game version \" GAME_VERSIONS")

# Configure main and thrive_version.h files
StandardConfigureExecutableMainAndInclude("${BaseIncludeFileName}" "main.cpp"
  "${BaseSubFolder}" "${PROJECT_SOURCE_DIR}/${BaseSubFolder}")


include_directories(${CMAKE_CURRENT_LIST_DIR})

set(CurrentProjectName Thrive)
set(AllProjectFiles
  ${BaseIncludeFileName}
  "main.cpp"
  "ThriveGame.h" "ThriveGame.cpp"
  "thrive_world_factory.h" "thrive_world_factory.cpp"
  "thrive_net_handler.h" "thrive_net_handler.cpp"
  )

# Compile a common library
add_library(ThriveLib STATIC ${THRIVE_FILES})

set_target_properties(ThriveLib PROPERTIES
  OUTPUT_NAME Thrive
  )

set_property(TARGET ThriveLib PROPERTY CXX_STANDARD 17)
set_property(TARGET ThriveLib PROPERTY CXX_EXTENSIONS OFF)

target_compile_definitions(ThriveLib PRIVATE "THRIVELIB_BUILD=1")

target_link_libraries(ThriveLib ${DEPENDENT_LIBS})

# Include the common file
set(CREATE_CONSOLE_APP OFF)
include(LeviathanUsingProject)

# The project is now defined
target_link_libraries(Thrive ThriveLib)


