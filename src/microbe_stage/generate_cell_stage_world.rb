#!/usr/bin/env ruby
# Generates Thrive world for the cell stage

require_relative '../../RubySetupSystem/RubyCommon.rb'
require_relative '../../ThirdParty/leviathan/Helpers/FileGen.rb'

abort "no target files provided" if ARGV.count < 2

generator = Generator.new ARGV[0], separateFiles: true

generator.useNamespace "thrive"
generator.addInclude "Entities/GameWorld.h"
generator.addInclude "Generated/StandardWorld.h"

world = GameWorldClass.new(
  "CellStageWorld", componentTypes: [
  ],
  systems: [
  ],
  systemspreticksetup: (<<-END
  const auto timeAndTickTuple = GetTickAndTime();
  const auto calculatedTick = std::get<0>(timeAndTickTuple);
  const auto progressInTick = std::get<1>(timeAndTickTuple);
  const auto tick = GetTickNumber();
  // TODO: thrive systems
END
                       ),  
)

world.base "Leviathan::StandardWorld"

generator.add world



# Output the file
generator.run


bindGenerator = Generator.new ARGV[1], bareOutput: true


bindGenerator.add OutputText.new(world.genAngelScriptBindings)


bindGenerator.run
