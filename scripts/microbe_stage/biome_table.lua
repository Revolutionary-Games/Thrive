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
            oxygen = 7500,
            co2 = 7500,
            ammonia = 7500,
            glucose = 7500
        }
    },

    ["volcanic_vent"] = {
        temperature = 100,
        sunlight = 40,
        background = "Background_Vent",
        compounds = {
            oxygen = 6000,
            co2 = 15000,
            ammonia = 8500,
            glucose = 6500
        }
    },

    ["abyss"] = {
        temperature = 35,
        sunlight = 5,
        background = "Background_Abyss",
        compounds = {
            oxygen = 5000,
            co2 = 7000,
            ammonia = 7000,
            glucose = 8000
        }
    },

    ["shallow"] = {
        temperature = 25,
        sunlight = 90,
        background = "Background_Shallow",
        compounds = {
            oxygen = 10000,
            co2 = 7000,
            ammonia = 6000,
            glucose = 7000
        }
    }
}

--TODO: check this numbers.
