#!/usr/bin/env ruby
# Generates basic GameWorld with all standard components

require_relative '../../RubySetupSystem/RubyCommon.rb'
require_relative '../../ThirdParty/leviathan/Helpers/FileGen.rb'

abort "no target file provided" if ARGV.count < 1

generator = Generator.new ARGV[0], true

generator.useNamespace "thrive"
generator.addInclude "Entities/GameWorld.h"
generator.addInclude "Generated/StandardWorld.h"

world = GameWorldClass.new(
  "CellStageWorld", componentTypes: [
  ],
  systems: [
  ],
  tickrunmethod: <<-END
  // TODO: thrive systems
END
)

world.base "Leviathan::StandardWorld"

generator.add world



# Output the file
generator.run


