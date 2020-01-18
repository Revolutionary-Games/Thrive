

# Add source files to the list
set(GROUP_ENGINE
  "engine/component_types.h"
  "engine/typedefs.h"
  "engine/player_data.cpp"
  "engine/player_data.h"
  # "engine/rolling_grid.cpp"
  # "engine/rolling_grid.h"
  )

set(GROUP_GENERAL
  "general/timed_life_system.cpp"
  "general/timed_life_system.h"
  "general/properties_component.cpp"
  "general/properties_component.h"
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
  "general/global_keypresses.h"
  "general/global_keypresses.cpp"
  "general/timed_world_operations.cpp"
  "general/timed_world_operations.cpp"
  )

set(GROUP_MICROBE_STAGE
  "microbe_stage/agent_cloud_system.cpp"
  "microbe_stage/agent_cloud_system.h"
  "microbe_stage/backgrounds.cpp"
  "microbe_stage/backgrounds.h"
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
  "microbe_stage/generate_microbe_editor_world.rb"
  "microbe_stage/fluid_system.cpp"
  "microbe_stage/fluid_system.h"
  "microbe_stage/membrane_system.cpp"
  "microbe_stage/membrane_system.h"
  "microbe_stage/microbe_camera_system.cpp"
  "microbe_stage/microbe_camera_system.h"
  "microbe_stage/process_system.cpp"
  "microbe_stage/process_system.h"
  "microbe_stage/compound_venter_system.cpp"
  "microbe_stage/compound_venter_system.h"
  "microbe_stage/simulation_parameters.cpp"
  "microbe_stage/simulation_parameters.h"
  "microbe_stage/spawn_system.cpp"
  "microbe_stage/spawn_system.h"
  "microbe_stage/species.cpp"
  "microbe_stage/species.h"
  "microbe_stage/patch.cpp"
  "microbe_stage/patch.h"
  "microbe_stage/patch_manager.cpp"
  "microbe_stage/patch_manager.h"
  "microbe_stage/species_name_controller.cpp"
  "microbe_stage/species_name_controller.h"
  "microbe_stage/player_microbe_control.h"
  "microbe_stage/player_microbe_control.cpp"
  "microbe_stage/microbe_editor_key_handler.h"
  "microbe_stage/microbe_editor_key_handler.cpp"
  "microbe_stage/player_hover_info.h"
  "microbe_stage/player_hover_info.cpp"
  "microbe_stage/organelle_types.h"
  "microbe_stage/organelle_types.cpp"
  "microbe_stage/organelle_template.h"
  "microbe_stage/organelle_template.cpp"
  "microbe_stage/organelle_table.h"
  "microbe_stage/organelle_table.cpp"
  "microbe_stage/membrane_types.h"
  "microbe_stage/membrane_types.cpp"
  )

set(GROUP_AUTO-EVO
  "auto-evo/auto-evo.cpp"
  "auto-evo/auto-evo.h"
  "auto-evo/run_parameters.cpp"
  "auto-evo/run_parameters.h"
  "auto-evo/run_step.h"
  "auto-evo/run_results.cpp"
  "auto-evo/run_results.h"
  "auto-evo/common_steps.h"
  "auto-evo/common_steps.cpp"
  "auto-evo/auto-evo_script_helpers.cpp"
  "auto-evo/auto-evo_script_helpers.h"
  )

set_source_files_properties("microbe_stage/generate_cell_stage_world.rb"
  PROPERTIES HEADER_FILE_ONLY TRUE)

set_source_files_properties("microbe_stage/generate_microbe_editor_world.rb"
  PROPERTIES HEADER_FILE_ONLY TRUE)

set(GROUP_SCRIPTING
  "scripting/script_initializer.cpp"
  "scripting/script_initializer.h"
  "scripting/register_organelle.cpp"
  "scripting/register_systems.cpp"
  "scripting/register_worlds.cpp"
  "scripting/register_patches.cpp"
  "scripting/register_registries.cpp"
  )

set(GROUP_UTIL
  "util/pair_hash.h"
  )

set(GROUP_SIMULATION
  "simulation/star_and_planet_generator.h"
  "simulation/star_and_planet_generator.cpp"
  )

file(GLOB MICROBE_GENERATOR_FILES "${PROJECT_SOURCE_DIR}/src/microbe_stage/*.rb")


AddRubyGeneratedFile(OUTPUT "${PROJECT_SOURCE_DIR}/src/generated/cell_stage_world.h"
  "${PROJECT_SOURCE_DIR}/src/generated/cell_stage_world.cpp"
  "${PROJECT_SOURCE_DIR}/src/generated/cell_stage_bindings.h"
  PARAMS "${PROJECT_SOURCE_DIR}/src/microbe_stage/generate_cell_stage_world.rb"
  "${PROJECT_SOURCE_DIR}/src/generated/cell_stage_world"
  "${PROJECT_SOURCE_DIR}/src/generated/cell_stage_bindings.h"
  DEPENDS ${MICROBE_GENERATOR_FILES}
  )

AddRubyGeneratedFile(OUTPUT "${PROJECT_SOURCE_DIR}/src/generated/microbe_editor_world.h"
  "${PROJECT_SOURCE_DIR}/src/generated/microbe_editor_world.cpp"
  "${PROJECT_SOURCE_DIR}/src/generated/microbe_editor_bindings.h"
  PARAMS "${PROJECT_SOURCE_DIR}/src/microbe_stage/generate_microbe_editor_world.rb"
  "${PROJECT_SOURCE_DIR}/src/generated/microbe_editor_world"
  "${PROJECT_SOURCE_DIR}/src/generated/microbe_editor_bindings.h"
  DEPENDS ${MICROBE_GENERATOR_FILES}
  )

set(GROUP_MICROBE
  "generated/cell_stage_world.h" "generated/cell_stage_world.cpp"
  "generated/cell_stage_bindings.h"
  "generated/microbe_editor_world.h" "generated/microbe_editor_world.cpp"
  "generated/microbe_editor_bindings.h"
  )

add_custom_command(OUTPUT "${PROJECT_SOURCE_DIR}/src/generated/thrive_v8_extension.h"
  COMMAND "${LEVIATHAN_SRC}/build/bin/FileGenerator" "V8EXT"
  "\"${PROJECT_SOURCE_DIR}/src/thrive_v8_extension.js\""
  "\"${PROJECT_SOURCE_DIR}/src/generated/thrive_v8_extension.h\""
  "thrive"
  DEPENDS "${PROJECT_SOURCE_DIR}/src/thrive_v8_extension.js"
  )

# Menu things
set(GROUP_MENU
  "main_menu_keypresses.cpp"
  "main_menu_keypresses.h"
  )

# For testing purposes even these quite heavily to the final
# executable connected files are included in the lib
set(GROUP_CORE_FILES
  # TODO: to move these out of here the script building would need
  # IF-DEFS everywhere for switching when ThriveGame isn't available
  "ThriveGame.h" "ThriveGame.cpp"
  "thrive_net_handler.h" "thrive_net_handler.cpp"
  "thrive_common.h" "thrive_common.cpp"
  # These might not be needed here
  "server/ThriveServer.h" "server/ThriveServer.cpp"
  "server/thrive_server_net_handler.h" "server/thrive_server_net_handler.cpp"
  )


# Groups for better visual studio experience
source_group("bullet" FILES ${GROUP_BULLET})
source_group("engine" FILES ${GROUP_ENGINE})
source_group("general" FILES ${GROUP_GENERAL})
source_group("gui" FILES ${GROUP_GUI})
source_group("microbe_stage" FILES ${GROUP_MICROBE_STAGE})
source_group("scripting" FILES ${GROUP_SCRIPTING})
source_group("sound" FILES ${GROUP_SOUND})
source_group("util" FILES ${GROUP_UTIL})
source_group("simulation" FILES ${GROUP_SIMULATION})
source_group("auto-evo" FILES ${GROUP_AUTO-EVO})

source_group("menu_code" FILES ${GROUP_MENU})

source_group("microbe" FILES ${GROUP_MICROBE})
source_group("main" FILES ${GROUP_CORE_FILES})


set(THRIVE_FILES ${GROUP_CORE_FILES} ${GROUP_BULLET} ${GROUP_ENGINE}
  ${GROUP_GENERAL} ${GROUP_GUI} ${GROUP_MICROBE_STAGE}
  ${GROUP_SCRIPTING} ${GROUP_SOUND}
  ${GROUP_UTIL} ${GROUP_SIMULATION} ${GROUP_MICROBE} ${GROUP_AUTO-EVO}
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

# Thrive specific JS native functions are registered by these options
set(PROGRAMUSE_CUSTOMJS 1)
set(PROGRAMCUSTOMJS_INCLUDE "generated/thrive_v8_extension.h")
set(PROGRAMCUSTOMJS_HANDLER_INCLUDE "thrive_js_interface.h")
set(PROGRAMCUSTOMJS_CLASS "ThriveJSInterface")
set(PROGRAMCUSTOMJS_EXTFILE "thrive_v8_extensionStr")
set(PROGRAMCUSTOMJS_HANDLER "&makeThriveJSHandler")
set(PROGRAMCUSTOMJS_MESSAGE_HANDLER "std::make_shared<ThriveJSMessageHandler>()")


# Configure main and thrive_version.h files
StandardConfigureExecutableMainAndInclude("${BaseIncludeFileName}" "main.cpp"
  "${BaseSubFolder}" "${PROJECT_SOURCE_DIR}/${BaseSubFolder}")


include_directories(${CMAKE_CURRENT_LIST_DIR})

# Application Resources
set(GROUP_RESOURCES
  "Main_Res.rc"
  )

source_group("Resource Files" FILES ${GROUP_RESOURCES})

set(CurrentProjectName Thrive)
set(AllProjectFiles
  ${BaseIncludeFileName}
  "main.cpp"
  "thrive_world_factory.h" "thrive_world_factory.cpp"
  "generated/thrive_v8_extension.h"
  "thrive_js_interface.h" "thrive_js_interface.cpp"
  ${GROUP_RESOURCES}
  )

# Compile a common library
add_library(ThriveLib STATIC ${THRIVE_FILES})

# All of a sudden this started causing problems on windows
# So ThriveLib now needs to have a different name than the main executable
# set_target_properties(ThriveLib PROPERTIES
#   OUTPUT_NAME Thrive
#   )

set_property(TARGET ThriveLib PROPERTY CXX_STANDARD 17)
set_property(TARGET ThriveLib PROPERTY CXX_EXTENSIONS OFF)

target_compile_definitions(ThriveLib PRIVATE "THRIVELIB_BUILD=1")

target_link_libraries(ThriveLib ${DEPENDENT_LIBS})

# Include the common file
set(CREATE_CONSOLE_APP OFF)
include(LeviathanUsingProject)

# The project is now defined
target_link_libraries(Thrive ThriveLib)

# Server target. This also defines a Leviathan program
add_subdirectory(server)
