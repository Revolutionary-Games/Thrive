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
            oxygen = 65000,
            co2 = 65000,
            ammonia = 65000,
            glucose = 65000
        }
    },

    ["volcanic_vent"] = {
        temperature = 100,
        sunlight = 40,
        background = "Background_Vent",
        compounds = {
            oxygen = 60000,
            co2 = 100000,
            ammonia = 85000,
            glucose = 65000,
            hydrogensulfide = 80000
        }
    },

    ["abyss"] = {
        temperature = 35,
        sunlight = 5,
        background = "Background_Abyss",
        compounds = {
            oxygen = 50000,
            co2 = 70000,
            ammonia = 70000,
            glucose = 80000
        }
    },

    ["shallow"] = {
        temperature = 25,
        sunlight = 90,
        background = "Background_Shallow",
        compounds = {
            oxygen = 100000,
            co2 = 70000,
            ammonia = 60000,
            glucose = 70000
        }
    }
}

--TODO: check this numbers.
