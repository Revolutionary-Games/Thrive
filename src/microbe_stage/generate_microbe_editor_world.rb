#!/usr/bin/env ruby
# Generates Thrive world for the microbe editor. This is different from the actual stage as
# most of the systems and components aren't shared

require_relative '../../RubySetupSystem/RubyCommon.rb'
require_relative '../../ThirdParty/Leviathan/Helpers/FileGen.rb'

abort "no target files provided" if ARGV.count < 2

generator = Generator.new ARGV[0], separateFiles: true

generator.useNamespace "thrive"
# generator.useExportMacro "THRIVE_EXPORT"
generator.useExportMacro nil
generator.addInclude "Entities/GameWorld.h"
generator.addInclude "Generated/StandardWorld.h"
# Needs script include for basic world functionality
generator.addInclude "Script/ScriptTypeResolver.h"

generator.addInclude "thrive_world_factory.h"


editorWorld = GameWorldClass.new(
  "MicrobeEditorWorld", componentTypes: [  
  ],
  systems: [
  ],
  systemspreticksetup: (<<-END
  const auto timeAndTickTuple = GetTickAndTime();
  const auto calculatedTick = std::get<0>(timeAndTickTuple);
  const auto progressInTick = std::get<1>(timeAndTickTuple);
  const auto tick = GetTickNumber();
END
                       ),
  networking: false
)

editorWorld.WorldType = "static_cast<int32_t>(thrive::THRIVE_WORLD_TYPE::MICROBE_EDITOR)"
editorWorld.base "Leviathan::StandardWorld"

generator.add editorWorld



# Output the file
generator.run


bindGenerator = Generator.new ARGV[1], bareOutput: true


bindGenerator.add OutputText.new(editorWorld.genAngelScriptBindings)


bindGenerator.run
