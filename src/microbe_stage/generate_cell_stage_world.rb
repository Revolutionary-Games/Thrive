#!/usr/bin/env ruby
# Generates Thrive world for the cell stage

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

generator.addInclude 'microbe_stage/fluid_system.h'
generator.addInclude 'microbe_stage/membrane_system.h'
generator.addInclude 'microbe_stage/compound_cloud_system.h'
generator.addInclude 'microbe_stage/process_system.h'
generator.addInclude 'microbe_stage/compound_venter_system.h'
generator.addInclude 'microbe_stage/spawn_system.h'
generator.addInclude 'microbe_stage/agent_cloud_system.h'
generator.addInclude 'microbe_stage/compound_absorber_system.h'
generator.addInclude 'microbe_stage/microbe_camera_system.h'
generator.addInclude 'microbe_stage/player_microbe_control.h'
generator.addInclude 'microbe_stage/microbe_editor_key_handler.h'
generator.addInclude 'microbe_stage/player_hover_info.h'
generator.addInclude 'microbe_stage/patch_manager.h'
generator.addInclude 'general/properties_component.h'
generator.addInclude 'general/timed_life_system.h'
generator.addInclude 'general/timed_world_operations.h'

cellWorld = GameWorldClass.new(
  'CellStageWorld',
  componentTypes: [
    EntityComponent.new('FluidEffectComponent', [ConstructorInfo.new([])]),
    EntityComponent.new('ProcessorComponent', [ConstructorInfo.new([])]),
    EntityComponent.new('CompoundBagComponent', [ConstructorInfo.new([])]),
    EntityComponent.new('CompoundVenterComponent', [ConstructorInfo.new([])]),
    EntityComponent.new('EngulfableComponent', [ConstructorInfo.new([])]),
    EntityComponent.new('MembraneComponent', [ConstructorInfo.new(
      [
        Variable.new('type', 'MembraneTypeId',
                     noRef: true,
                     memberaccess: 'membraneType',
                     serializeas: 'uint16_t')
        # Variable.new("GetScene()", "",
        #             nonMethodParam: true),
      ]
    )], releaseparams: ['GetScene()']),
    EntityComponent.new('CompoundCloudComponent', [
                          # Don't actually call this from other places than CompoundCloudSystem
                          ConstructorInfo.new(
                            [
                              Variable.new('owner', 'CompoundCloudSystem',
                                           noConst: true),
                              Variable.new('first', 'Compound*',
                                           noRef: true),
                              Variable.new('second', 'Compound*',
                                           noRef: true),
                              Variable.new('third', 'Compound*',
                                           noRef: true),
                              Variable.new('fourth', 'Compound*',
                                           noRef: true)
                            ], noangelscript: true
                          )
                        ],
                        releaseparams: ['GetScene()'],
                        nosynchronize: true),
    EntityComponent.new('AgentCloudComponent', [ConstructorInfo.new(
      [
        Variable.new('compoundId', 'CompoundId',
                     noRef: true,
                     memberaccess: 'm_compoundId'),
        Variable.new('red', 'float',
                     noRef: true,
                     memberaccess: 'getRed()'),
        Variable.new('green', 'float',
                     noRef: true,
                     memberaccess: 'getGreen()'),
        Variable.new('blue', 'float',
                     noRef: true,
                     memberaccess: 'getBlue()')
      ]
    )]),
    EntityComponent.new('SpawnedComponent', [ConstructorInfo.new(
      [
        Variable.new('newSpawnRadius', 'double',
                     noRef: true)
      ]
    )],
                        nosynchronize: true),
    EntityComponent.new('CompoundAbsorberComponent', [ConstructorInfo.new([])]),
    EntityComponent.new('TimedLifeComponent', [ConstructorInfo.new(
      [
        Variable.new('timeToLive', 'float',
                     noRef: true)
      ]
    )],
                        nosynchronize: true),
    EntityComponent.new('AgentProperties', [ConstructorInfo.new([])]),
    EntityComponent.new('DamageOnTouchComponent', [ConstructorInfo.new([])])

  ],
  systems: [
    EntitySystem.new('MembraneSystem', %w[MembraneComponent RenderNode],
                     # This is ran only once and the animation is in
                     # the vertex shader. That's why this isn't in
                     # "runrender"
                     runtick: { group: 100, parameters: [
                       'GetScene()'
                     ] }),

    EntitySystem.new('FluidSystem', %w[FluidEffectComponent Physics],
                     runtick: { group: 49, parameters: ['elapsed'] }),

    EntitySystem.new('SpawnSystem', %w[SpawnedComponent Position],
                     runtick: { group: 50, parameters: ['elapsed'] },
                     visibletoscripts: true,
                     release: []),
    EntitySystem.new('CompoundCloudSystem', [],
                     runtick: { group: 51, parameters: [
                       'elapsed'
                     ] },
                     visibletoscripts: true,
                     init: [
                       Variable.new('*this', '',
                                    nonMethodParam: true)
                     ],
                     release: [
                       Variable.new('*this', '',
                                    nonMethodParam: true)
                     ]),

    EntitySystem.new('AgentCloudSystem', %w[Position AgentCloudComponent RenderNode],
                     runtick: { group: 5, parameters: [] }),

    EntitySystem.new('CompoundAbsorberSystem', %w[AgentCloudComponent Position
                                                  MembraneComponent
                                                  CompoundAbsorberComponent],
                     runtick: { group: 6, parameters: [
                       'ComponentCompoundCloudComponent.GetIndex()', 'elapsed'
                     ] }),

    EntitySystem.new('MicrobeCameraSystem', [],
                     runtick: { group: 1000, parameters: ['elapsed'] }),
    EntitySystem.new('PlayerMicrobeControlSystem', [],
                     runtick: { group: 5, parameters: [] }),
    EntitySystem.new('PlayerHoverInfoSystem', %w[MembraneComponent Position],
                     runtick: { group: 900, parameters: ['elapsed'] }),

    EntitySystem.new('ProcessSystem', %w[CompoundBagComponent ProcessorComponent],
                     runtick: { group: 10, parameters: ['elapsed'] },
                     visibletoscripts: true),
    EntitySystem.new('CompoundVenterSystem',
                     %w[CompoundBagComponent CompoundVenterComponent Position],
                     runtick: { group: 11, parameters: ['elapsed'] }),
    EntitySystem.new('TimedLifeSystem', [],
                     runtick: { group: 45, parameters: [
                       'ComponentTimedLifeComponent.GetIndex()', 'elapsed'
                     ] })
  ],
  perworlddata: [
    Variable.new('_PatchManager', 'PatchManager'),
    Variable.new('_TimedWorldOperations', 'TimedWorldOperations')
  ]
)

cellWorld.WorldType = 'static_cast<int32_t>(thrive::THRIVE_WORLD_TYPE::CELL_STAGE)'
cellWorld.base 'Leviathan::StandardWorld'

generator.add cellWorld

# Output the file
generator.run

bind_generator = Generator.new ARGV[1], bareOutput: true

bind_generator.add OutputText.new(cellWorld.genAngelScriptBindings)

bind_generator.run
