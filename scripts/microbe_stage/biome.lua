--Global table which stores the current biome the player is in.
currentBiome = {}

--Setting the current biome to the one with the specified name.
function setBiome(biomeName, gameState)
    assert(gameState ~= nil, "setBiome requires gameState")
    
    --Getting the base biome to change to.
    local baseBiome = biomeTable[biomeName]

    --Setting the new biome attributes
    currentBiome = {}
    currentBiome.name = biomeName
    currentBiome.temperature = baseBiome.temperature
    currentBiome.sunlight = baseBiome.sunlight
    currentBiome.background = baseBiome.background
    currentBiome.compounds = {}

    for compoundName, compoundData in pairs(baseBiome.compounds) do
        currentBiome.compounds[compoundName] = compoundData.amount

        if compoundTable[compoundName].isCloud then
            local spawnCloud =  function(pos)
                return createCompoundCloud(compoundName, pos.x, pos.y)
            end

            gSpawnSystem:removeSpawnType(compoundSpawnTypes[compoundName])
            compoundSpawnTypes[compoundName] = gSpawnSystem:addSpawnType(spawnCloud, compoundData.density, CLOUD_SPAWN_RADIUS)
        end
    end

    --Changing the background.
    local entity = Entity.new("background", gameState.wrapper)
    local skyplane = SkyPlaneComponent.new()
    skyplane.properties.plane.normal = Vector3(0, 0, 2000)
    skyplane.properties.materialName = currentBiome.background
	skyplane.properties.scale = 200
    skyplane.properties:touch()
    entity:addComponent(skyplane)
end

--Setting the current biome to a random biome selected from the biome table.
function setRandomBiome(gameState)
    --Getting the size of the biome table.
    local numberOfBiomes = 0
    local biomeNameTable = {}
    for biomeName, _ in pairs(biomeTable) do
        numberOfBiomes = numberOfBiomes + 1
        table.insert(biomeNameTable, biomeName)
    end

    --Selecting a random biome.
    math.randomseed(os.time())
    local rand = math.random(1, numberOfBiomes)
    local currentBiomeName = biomeNameTable[rand]

    --Switching to that biome.
    setBiome(currentBiomeName, gameState)
end
