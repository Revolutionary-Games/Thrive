#!/usr/bin/env ruby
# Generates Thrive world for the cell stage

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

generator.addInclude "microbe_stage/membrane_system.h"
generator.addInclude "microbe_stage/compound_cloud_system.h"
generator.addInclude "microbe_stage/process_system.h"
generator.addInclude "microbe_stage/compound_venter_system.h"
generator.addInclude "microbe_stage/species_component.h"
generator.addInclude "microbe_stage/spawn_system.h"
generator.addInclude "microbe_stage/agent_cloud_system.h"
generator.addInclude "microbe_stage/compound_absorber_system.h"
generator.addInclude "microbe_stage/microbe_camera_system.h"
generator.addInclude "microbe_stage/player_microbe_control.h"
generator.addInclude "microbe_stage/microbe_editor_key_handler.h"
generator.addInclude "microbe_stage/player_hover_info.h"
generator.addInclude "general/properties_component.h"
generator.addInclude "general/timed_life_system.h"

cellWorld = GameWorldClass.new(
  "CellStageWorld", componentTypes: [
    EntityComponent.new("ProcessorComponent", [ConstructorInfo.new([])]),
    EntityComponent.new("CompoundBagComponent", [ConstructorInfo.new([])]),
    EntityComponent.new("CompoundVenterComponent", [ConstructorInfo.new([])]),
    EntityComponent.new("EngulfableComponent", [ConstructorInfo.new([])]),
    EntityComponent.new("SpeciesComponent", [ConstructorInfo.new([
                                                Variable.new("name", "std::string",
                                                             memberaccess: "name",
                                                             noRef: false)
                                                                 ])],
                        # Disabled to not send empty species to the
                        # client. This should be re-enabled ones
                        # synchronizing species data works
                        nosynchronize: true),
    EntityComponent.new("MembraneComponent", [ConstructorInfo.new(
                                         [
											Variable.new("type", "MEMBRANE_TYPE",
                                                         noRef: true,
                                                         memberaccess: "membraneType",
                                                         serializeas: "uint16_t"),
                                           #Variable.new("GetScene()", "",
                                           #             nonMethodParam: true),
                                         ])], 
										 releaseparams: ["GetScene()"]),
    EntityComponent.new("CompoundCloudComponent", [
                          # Don't actually call this from other places than CompoundCloudSystem
                          ConstructorInfo.new(
                            [
                              Variable.new("owner", "CompoundCloudSystem",
                                           noConst: true),
                              Variable.new("first", "Compound*",
                                           noRef: true),
                              Variable.new("second", "Compound*",
                                           noRef: true),
                              Variable.new("third", "Compound*",
                                           noRef: true),
                              Variable.new("fourth", "Compound*",
                                           noRef: true),
                            ], noangelscript: true,
                          )],
                        releaseparams: ["GetScene()"],
                        nosynchronize: true),
    EntityComponent.new("AgentCloudComponent", [ConstructorInfo.new(
                                                  [
                                                    Variable.new("compoundId", "CompoundId",
                                                                 noRef: true,
                                                                 memberaccess: "m_compoundId"),
                                                    Variable.new("red", "float",
                                                                 noRef: true,
                                                                 memberaccess: "getRed()"),
                                                    Variable.new("green", "float",
                                                                 noRef: true,
                                                                 memberaccess: "getGreen()"),
                                                    Variable.new("blue", "float",
                                                                 noRef: true,
                                                                 memberaccess: "getBlue()"),
                                                  ])]),
    EntityComponent.new("SpawnedComponent", [ConstructorInfo.new(
                                               [
                                                 Variable.new("newSpawnRadius", "double",
                                                              noRef: true)
                                               ])],
                        nosynchronize: true,),
    EntityComponent.new("CompoundAbsorberComponent", [ConstructorInfo.new([])]),
    EntityComponent.new("TimedLifeComponent", [ConstructorInfo.new(
                                                 [
                                                   Variable.new("timeToLive", "int",
                                                                noRef: true),
                                                 ])],
                        nosynchronize: true),
    EntityComponent.new("AgentProperties", [ConstructorInfo.new([])]),
    
  ],
  systems: [
    EntitySystem.new("MembraneSystem", ["MembraneComponent", "RenderNode"],
                     # This is ran only once and the animation is in
                     # the vertex shader. That's why this isn't in
                     # "runrender"
                     runtick: {group: 100, parameters: [
                                   "GetScene()"
                                 ]}),

    EntitySystem.new("SpawnSystem", ["SpawnedComponent", "Position"],
                     runtick: {group: 50, parameters: []},
                     visibletoscripts: true,
                     release: []),
    EntitySystem.new("CompoundCloudSystem", [],
                    runtick: {group: 51, parameters: [
                                
                              ]},
                    visibletoscripts: true,
                    init: [
                      Variable.new("*this", "",
                                   nonMethodParam: true)
                    ],
                    release: [
                      Variable.new("*this", "",
                                   nonMethodParam: true)
                    ]),
	
    EntitySystem.new("AgentCloudSystem", ["Position", "AgentCloudComponent", "RenderNode"],
                     runtick: {group: 5, parameters: []}),

    EntitySystem.new("CompoundAbsorberSystem", ["AgentCloudComponent", "Position",
                                                "MembraneComponent",
                                                "CompoundAbsorberComponent"],
                     runtick: {group: 6, parameters: [
                                 "ComponentCompoundCloudComponent.GetIndex()"]}),

    EntitySystem.new("MicrobeCameraSystem", [],
                     runtick: {group: 1000, parameters: []}),
    EntitySystem.new("PlayerMicrobeControlSystem", [],
                     runtick: {group: 5, parameters: []}),
    EntitySystem.new("PlayerHoverInfoSystem", ["MembraneComponent", "Position"],
                     runtick: {group: 900, parameters: []}),

    EntitySystem.new("ProcessSystem", ["CompoundBagComponent", "ProcessorComponent"],
                     runtick: {group: 10, parameters: []},
                     visibletoscripts: true),
    EntitySystem.new("CompoundVenterSystem", ["CompoundBagComponent", "CompoundVenterComponent", "Position"],
                     runtick: {group: 11, parameters: []}),
    EntitySystem.new("TimedLifeSystem", [],
                     runtick: {group: 45, parameters: [
                                 "ComponentTimedLifeComponent.GetIndex()"
                               ]}),

    #EntitySystem.new("ProcessSystem", ["CompoundBagComponent", "ProcessorComponent"],

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

cellWorld.WorldType = "static_cast<int32_t>(thrive::THRIVE_WORLD_TYPE::CELL_STAGE)"
cellWorld.base "Leviathan::StandardWorld"

generator.add cellWorld



# Output the file
generator.run


bindGenerator = Generator.new ARGV[1], bareOutput: true


bindGenerator.add OutputText.new(cellWorld.genAngelScriptBindings)


bindGenerator.run
