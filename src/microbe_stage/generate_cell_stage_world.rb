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

generator.addInclude "microbe_stage/membrane_system.h"
generator.addInclude "microbe_stage/compound_cloud_system.h"

world = GameWorldClass.new(
  "CellStageWorld", componentTypes: [
    EntityComponent.new("MembraneComponent", [ConstructorInfo.new(
                                         [
                                           #Variable.new("GetScene()", "",
                                           #             nonMethodParam: true),
                                         ])], releaseparams: ["GetScene()"]),
    EntityComponent.new("CompoundCloudComponent", [ConstructorInfo.new(
                                                     [
                                                       Variable.new("compoundId", "CompoundId",
                                                                    noRef: true),
                                                       Variable.new("red", "float",
                                                                    noRef: true),
                                                       Variable.new("green", "float",
                                                                    noRef: true),
                                                       Variable.new("blue", "float",
                                                                    noRef: true),
                                                     ])]),
  ],
  systems: [
    EntitySystem.new("MembraneSystem", ["MembraneComponent", "RenderNode"],
                     runrender: {group: 10, parameters: [
                                   "GetScene()"
                                 ]}),
    # EntitySystem.new("CompoundCloudSystem", [],
    #                  nostate: true,
    #                  init: [Variable.new("*this", "")],
    #                  release: [Variable.new("*this", "")],
    #                  runtick: {group: 5, parameters: [
    #                              "ComponentCompoundCloudComponent.GetIndex()",
    #                              "GetTickNumber()"
    #                            ]})
  ],
  systemspreticksetup: (<<-END
  const auto timeAndTickTuple = GetTickAndTime();
  const auto calculatedTick = std::get<0>(timeAndTickTuple);
  const auto progressInTick = std::get<1>(timeAndTickTuple);
  const auto tick = GetTickNumber();
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
