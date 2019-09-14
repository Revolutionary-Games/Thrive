// ------------------------------------ //
#include "patch_manager.h"

#include "simulation_parameters.h"

#include "ThriveGame.h"
#include "generated/cell_stage_world.h"

using namespace thrive;
// ------------------------------------ //

constexpr auto MICROBE_SPAWN_RADIUS = 150;
constexpr auto CLOUD_SPAWN_RADIUS = 150;

constexpr auto STARTING_SPAWN_DENSITY = 70000.0f;
constexpr auto MAX_SPAWN_DENSITY = 20000.0f;

PatchManager::PatchManager(GameWorld& world) :
    Leviathan::PerWorldData(world),
    cellWorld(dynamic_cast<CellStageWorld&>(world))
{}
// ------------------------------------ //
void
    PatchManager::applyPatchSettings()
{
    if(!currentMap)
        throw InvalidState("no current map");

    const auto patch = currentMap->getCurrentPatch();

    if(!patch)
        throw InvalidState("currently selected patch is invalid");

    LOG_INFO("PatchManager: applying patch settings");

    unmarkAllSpawners();

    const auto& biome = patch->getBiome();

    handleChunkSpawns(biome);
    handleCloudSpawns(biome);
    handleCellSpawns(*patch);

    removeNonMarkedSpawners();

    // Change the lighting
    updateLight(biome);

    // Changing the background.
    ThriveGame::get()->setBackgroundMaterial(biome.background);

    // Update environment for process system
    cellWorld.GetProcessSystem().setProcessBiome(biome);

    updateBiomeStatsForGUI(biome);
    updateCurrentPatchInfoForGUI(*patch);
}

void
    PatchManager::handleChunkSpawns(const Biome& biome)
{
    LOG_INFO("chunks.length = " + std::to_string(biome.chunks.size()));

    for(auto iter = biome.chunks.begin(); iter != biome.chunks.end(); ++iter) {
        const ChunkData& chunk = iter->second;

        if(chunk.density <= 0) {
            LOG_WARNING("chunk spawn density is 0. It won't spawn");
            continue;
        }

        const auto existing = std::find_if(chunkSpawners.begin(),
            chunkSpawners.end(),
            [&](const auto& spawner) { return spawner.thing == chunk.name; });

        if(existing != chunkSpawners.end()) {

            existing->marked = true;

            if(existing->setDensity != chunk.density) {

                existing->setDensity = chunk.density;
                cellWorld.GetSpawnSystem().updateDensity(
                    existing->id, existing->setDensity);
            }
            continue;
        }
        // New spawner needed
        LOG_INFO("registering chunk: Name: " + chunk.name +
                 " density: " + std::to_string(chunk.density));

        chunkSpawners.emplace_back(
            cellWorld.GetSpawnSystem().addSpawnType(
                [=](CellStageWorld& world, Float3 pos) {
                    ScriptRunningSetup setup = ScriptRunningSetup("spawnChunk");

                    auto result = ThriveCommon::get()
                                      ->getMicrobeScripts()
                                      ->ExecuteOnModule<ObjectID>(
                                          setup, false, &world, &chunk, pos);

                    if(result.Result != SCRIPT_RUN_RESULT::Success) {

                        LOG_ERROR("Failed to run chunk spawn");
                        return NULL_OBJECT;
                    }

                    return result.Value;
                },
                chunk.density, MICROBE_SPAWN_RADIUS),
            chunk.name, chunk.density);
    }
}

void
    PatchManager::handleCloudSpawns(const Biome& biome)
{
    LOG_INFO(
        "biomeCompounds.length = " + std::to_string(biome.compounds.size()));

    for(auto iter = biome.compounds.begin(); iter != biome.compounds.end();
        ++iter) {
        const CompoundId compoundId = iter->first;
        const BiomeCompoundData& compound = iter->second;

        const auto& compoundData =
            SimulationParameters::compoundRegistry.getTypeData(compoundId);

        if(!compoundData.isCloud)
            continue;

        if(compound.density <= 0) {
            LOG_WARNING("Compound density is 0. It won't spawn");
            continue;
        }

        const auto existing = std::find_if(cloudSpawners.begin(),
            cloudSpawners.end(), [&](const auto& spawner) {
                return spawner.thing == compoundData.internalName;
            });

        if(existing != cloudSpawners.end()) {

            existing->marked = true;

            if(existing->setDensity != compound.density) {

                existing->setDensity = compound.density;
                cellWorld.GetSpawnSystem().updateDensity(
                    existing->id, existing->setDensity);
            }

            continue;
        }

        // New spawner needed
        LOG_INFO("registering cloud: " + compoundData.internalName +
                 ", density: " + std::to_string(compound.density));

        cloudSpawners.emplace_back(
            cellWorld.GetSpawnSystem().addSpawnType(
                [=](CellStageWorld& world, Float3 pos) {
                    ScriptRunningSetup setup =
                        ScriptRunningSetup("spawnCompoundCloud");

                    auto result =
                        ThriveCommon::get()
                            ->getMicrobeScripts()
                            ->ExecuteOnModule<void>(setup, false, &world,
                                compoundId, compound.amount, pos);

                    if(result.Result != SCRIPT_RUN_RESULT::Success) {

                        LOG_ERROR("Failed to run compound spawn");
                        return NULL_OBJECT;
                    }

                    // Clouds never spawn as entities
                    return NULL_OBJECT;
                },
                compound.density, CLOUD_SPAWN_RADIUS),
            compoundData.internalName, compound.density);
    }
}

void
    PatchManager::handleCellSpawns(const Patch& patch)
{
    for(const auto& speciesInPatch : patch.getSpecies()) {

        if(speciesInPatch.population <= 0)
            continue;

        const auto density = 1.0f / (STARTING_SPAWN_DENSITY -
                                        (std::min<float>(MAX_SPAWN_DENSITY,
                                            speciesInPatch.population * 5)));

        const auto& name = speciesInPatch.species->name;

        const auto existing =
            std::find_if(microbeSpawners.begin(), microbeSpawners.end(),
                [&](const auto& spawner) { return spawner.thing == name; });

        if(existing != microbeSpawners.end()) {

            existing->marked = true;

            if(existing->setDensity != density) {

                existing->setDensity = density;
                cellWorld.GetSpawnSystem().updateDensity(
                    existing->id, existing->setDensity);
            }

            continue;
        }
        // New spawner needed
        LOG_INFO("registering species spawn: " + name +
                 ", initial density: " + std::to_string(density));

        microbeSpawners.emplace_back(
            cellWorld.GetSpawnSystem().addSpawnType(
                [=](CellStageWorld& world, Float3 pos) {
                    if(!speciesInPatch.species->isBacteria) {

                        // TODO: this spawns a ton of things but only one of
                        // them is returned, meaning the rest may not
                        // despawn properly
                        ScriptRunningSetup setup =
                            ScriptRunningSetup("bacteriaColonySpawn");

                        auto result =
                            ThriveCommon::get()
                                ->getMicrobeScripts()
                                ->ExecuteOnModule<ObjectID>(setup, false,
                                    &world, pos, speciesInPatch.species->name);

                        if(result.Result != SCRIPT_RUN_RESULT::Success) {

                            LOG_ERROR("Failed to run bacteriaColonySpawn");
                            return NULL_OBJECT;
                        }

                        return result.Value;

                    } else {

                        ScriptRunningSetup setup = ScriptRunningSetup(
                            "ObjectID "
                            "MicrobeOperations::spawnMicrobe("
                            "CellStageWorld@, Float3, const string &in, "
                            "bool, bool)");
                        setup.FullDeclaration = true;

                        auto result =
                            ThriveCommon::get()
                                ->getMicrobeScripts()
                                ->ExecuteOnModule<ObjectID>(setup, false,
                                    &world, pos, speciesInPatch.species->name,
                                    true, false);

                        if(result.Result != SCRIPT_RUN_RESULT::Success) {

                            LOG_ERROR("Failed to run "
                                      "MicrobeOperations::spawnMicrobe");
                            return NULL_OBJECT;
                        }

                        return result.Value;
                    }
                },
                density, MICROBE_SPAWN_RADIUS),
            name, density);
    }
}
// ------------------------------------ //
void
    PatchManager::updateLight(const Biome& biome)
{
    LOG_INFO("TODO: redo PatchManager::updateLight");
    // Light properties isnt working for some reason
    // world.SetLightProperties(getCurrentBiome().diffuseColors,
    // getCurrentBiome().specularColors,
    //     bs::Vector3(Float3(0.55f, -0.3f, 0.75f).Normalize()),
    //     getCurrentBiome().lightPower,
    //     //
    //     https://ogrecave.github.io/ogre/api/2.1/class_ogre_1_1_scene_manager.html#a56cd9aa2c4dee4eec9eb07ce1372fb52
    //     getCurrentBiome().upperAmbientColor,
    //     getCurrentBiome().lowerAmbientColor,
    //     -Float3(0.55f, -0.3f, 0.75f).Normalize() + Float3::UnitVUp * 0.2f
    // );
}
// ------------------------------------ //
void
    PatchManager::updateBiomeStatsForGUI(const Biome& biome)
{
    // Update oxygen and carbon dioxide numbers
    auto oxyId = SimulationParameters::compoundRegistry.getTypeId("oxygen");
    auto c02Id =
        SimulationParameters::compoundRegistry.getTypeId("carbondioxide");
    auto n2Id = SimulationParameters::compoundRegistry.getTypeId("nitrogen");

    auto updateDissolvedGasses =
        GenericEvent::MakeShared<GenericEvent>("UpdateDissolvedGasses");

    auto vars = updateDissolvedGasses->GetVariables();

    vars->Add(std::make_shared<NamedVariableList>("oxygenPercent",
        new Leviathan::IntBlock(
            cellWorld.GetProcessSystem().getDissolved(oxyId) * 100)));

    vars->Add(std::make_shared<NamedVariableList>("co2Percent",
        new Leviathan::IntBlock(
            cellWorld.GetProcessSystem().getDissolved(c02Id) * 100)));

    vars->Add(std::make_shared<NamedVariableList>("n2Percent",
        new Leviathan::IntBlock(
            cellWorld.GetProcessSystem().getDissolved(n2Id) * 100)));

    Engine::Get()->GetEventHandler()->CallEvent(updateDissolvedGasses);
}

void
    PatchManager::updateCurrentPatchInfoForGUI(const Patch& patch)
{
    auto event = GenericEvent::MakeShared<GenericEvent>("UpdatePatchDetails");

    auto vars = event->GetVariables();

    vars->Add(std::make_shared<NamedVariableList>(
        "patchName", new Leviathan::StringBlock(patch.getName())));

    Engine::Get()->GetEventHandler()->CallEvent(event);
}
// ------------------------------------ //
void
    PatchManager::removeNonMarkedSpawners()
{
    clearUnmarkedSingle(chunkSpawners);
    clearUnmarkedSingle(cloudSpawners);
    clearUnmarkedSingle(microbeSpawners);
}

void
    PatchManager::clearUnmarkedSingle(std::vector<ExistingSpawn>& spawners)
{
    for(auto iter = spawners.begin(); iter != spawners.end();) {
        if(!iter->marked) {
            LOG_INFO("removing spawner: " + iter->thing);
            cellWorld.GetSpawnSystem().removeSpawnType(iter->id);

            iter = spawners.erase(iter);
        } else {
            ++iter;
        }
    }
}

void
    PatchManager::unmarkAllSpawners()
{
    unmarkSingle(chunkSpawners);
    unmarkSingle(cloudSpawners);
    unmarkSingle(microbeSpawners);
}

void
    PatchManager::unmarkSingle(std::vector<ExistingSpawn>& spawners)
{
    for(auto iter = spawners.begin(); iter != spawners.end(); ++iter) {
        iter->marked = false;
    }
}
// ------------------------------------ //
