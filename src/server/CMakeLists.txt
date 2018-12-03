# Thrive server program



# Groups for better visual studio experience
source_group("main" FILES ${GROUP_CORE_FILES})

# Leviathan program setup

set(BaseProgramName "ThriveServer")
set(BaseIncludeFileName "thrive_version.h")
set(BaseSubFolder "src/server")

# Set all the settings
set(ProgramIncludesHeader "${BaseIncludeFileName}")
set(ProgramAppHeader "ThriveServer.h")

set(WorldFactoryClass "ThriveWorldFactory")
set(WorldFactoryInclude "thrive_world_factory.h")


# ------------------ ProgramConfiguration ------------------ #
set(PROGRAMCLASSNAME				ThriveServer)
set(PROGRAMLOG						ThriveServer)
set(PROGRAMCONFIGURATION			"./ThriveServer.conf")
set(PROGRAMKEYCONFIGURATION			"./ThriveKeybindings.conf")
set(PROGRAMCHECKCONFIGFUNCNAME		"ThriveServer::CheckGameConfigurationVariables")
set(PROGRAMCHECKKEYCONFIGFUNCNAME	"ThriveServer::CheckGameKeyConfigVariables")
set(WINDOWTITLEGENFUNCTION			"ThriveServer::GenerateWindowTitle()")
set(USERREADABLEIDENTIFICATION		"\"Thrive server version \" GAME_VERSIONS")

# Server doesn't have GUI
set(PROGRAMUSE_CUSTOMJS 0)

# Configure main and thrive_version.h files
StandardConfigureExecutableMain("main.cpp" "${BaseSubFolder}"
  "${PROJECT_SOURCE_DIR}/${BaseSubFolder}")


set(CurrentProjectName ThriveServer)
set(AllProjectFiles
  "../${BaseIncludeFileName}"
  "main.cpp"
  "../thrive_world_factory.h" "../thrive_world_factory.cpp"
  )

# Include the common file
set(CREATE_CONSOLE_APP ON)
include(LeviathanUsingProject)

# The project is now defined
target_link_libraries(ThriveServer ThriveLib)
