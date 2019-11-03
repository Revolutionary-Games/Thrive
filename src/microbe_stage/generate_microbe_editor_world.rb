#!/usr/bin/env ruby
# Generates Thrive world for the microbe editor. This is different from the actual stage as
# most of the systems and components aren't shared

require_relative '../../RubySetupSystem/RubyCommon.rb'
require_relative '../../ThirdParty/Leviathan/Helpers/FileGen.rb'

abort 'no target files provided' if ARGV.count < 2

generator = Generator.new ARGV[0], separateFiles: true

generator.useNamespace 'thrive'
# generator.useExportMacro "THRIVE_EXPORT"
generator.useExportMacro nil
generator.addInclude 'Entities/GameWorld.h'
generator.addInclude 'Generated/StandardWorld.h'
# Needs script include for basic world functionality
generator.addInclude 'Script/ScriptTypeResolver.h'

generator.addInclude 'thrive_world_factory.h'

editor_world = GameWorldClass.new(
  'MicrobeEditorWorld',
  componentTypes: [],
  systems: [],
  networking: false
)

editor_world.WorldType = 'static_cast<int32_t>(thrive::THRIVE_WORLD_TYPE::MICROBE_EDITOR)'
editor_world.base 'Leviathan::StandardWorld'

generator.add editor_world

# Output the file
generator.run

bind_generator = Generator.new ARGV[1], bareOutput: true

bind_generator.add OutputText.new(editor_world.genAngelScriptBindings)

bind_generator.run
