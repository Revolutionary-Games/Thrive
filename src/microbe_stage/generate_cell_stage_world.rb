#!/usr/bin/env ruby
# Generates Thrive world for the cell stage

require_relative '../../RubySetupSystem/RubyCommon.rb'
require_relative '../../ThirdParty/leviathan/Helpers/FileGen.rb'

abort "no target files provided" if ARGV.count < 2

generator = Generator.new ARGV[0], separateFiles: true

generator.useNamespace "thrive"
# generator.useExportMacro "THRIVE_EXPORT"
generator.useExportMacro nil
generator.addInclude "Entities/GameWorld.h"
generator.addInclude "Generated/StandardWorld.h"
generator.addInclude "thrive_include.h"

generator.addInclude "microbe_stage/membrane_system.h"

world = GameWorldClass.new(
  "CellStageWorld", componentTypes: [
    EntityComponent.new("MembraneComponent", [ConstructorInfo.new(
                                         [
                                           #Variable.new("GetScene()", "",
                                           #             nonMethodParam: true),
                                         ])], releaseparams: ["GetScene()"]),
  ],
  systems: [
    EntitySystem.new("MembraneSystem", ["MembraneComponent", "RenderNode"],
                     runrender: {group: 10, parameters: [
                                   "GetScene()"
                                 ]}),
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
