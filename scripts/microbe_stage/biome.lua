local currentBiome = {}

function setBiome(biomeName)
    currentBiome = {}
    local baseBiome = biomeTable[biomeName]

    --Setting the new biome attributes
    currentBiome.name = biomeName
    currentBiome.temperature = baseBiome.temperature
    currentBiome.sunlight = baseBiome.sunlight
    currentBiome.background = baseBiome.background

    --Changing the background.
    local entity = Entity("background")
    local skyplane = SkyPlaneComponent()
    skyplane.properties.plane.normal = Vector3(0, 0, 2000)
    skyplane.properties.materialName = currentBiome.background
	skyplane.properties.scale = 200
    skyplane.properties:touch()
    entity:addComponent(skyplane)
end

function setRandomBiome()
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
    setBiome(currentBiomeName)
end