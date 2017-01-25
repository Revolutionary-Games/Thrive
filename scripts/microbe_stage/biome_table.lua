--[[
Biome atributes:
    temperature:    The average temperature of the biome.
                    Measured in celsius degrees.

    sunlight:       Percentage of sunligth that reaches the biome.

    background:     Name of the background material used in this biome.

    compounds:      Table with the amount of each compound in the biome.
]]

biomeTable = {
    ["default"] = { --Where would this biome be anyways?
        temperature = 30,
        sunlight = 75,
        background = "Background",
        compounds = {
            oxygen = 32500,
            co2 = 32500,
            ammonia = 32500,
            glucose = 32500
        }
    },

    ["volcanic_vent"] = {
        temperature = 100,
        sunlight = 40,
        background = "Background_Vent",
        compounds = {
            oxygen = 30000,
            co2 = 75000,
            ammonia = 42500,
            glucose = 32500
        }
    },

    ["abyss"] = {
        temperature = 35,
        sunlight = 5,
        background = "Background_Abyss",
        compounds = {
            oxygen = 25000,
            co2 = 35000,
            ammonia = 35000,
            glucose = 40000
        }
    },

    ["shallow"] = {
        temperature = 25,
        sunlight = 90,
        background = "Background_Shallow",
        compounds = {
            oxygen = 50000,
            co2 = 35000,
            ammonia = 30000,
            glucose = 35000
        }
    }
}

--TODO: check this numbers.
