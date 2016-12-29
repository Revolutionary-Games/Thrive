--[[
Biome atributes:
    temperature:    The average temperature of the biome.
                    Measured in celsius degrees.

    sunlight:    Percentage of sunligth that reaches the biome.

    background:     Name of the background material used in this biome.
]]

biomeTable = {
    ["default"] = { --Where would this biome be anyways?
        temperature = 30,
        sunlight = 75,
        background = "Background"
    },

    ["volcanic_vent"] = {
        temperature = 100,
        sunlight = 40,
        background = "Background_Vent"
    },

    ["abyss"] = {
        temperature = 35,
        sunlight = 5,
        background = "Background_Abyss"
    },

    ["shallow"] = {
        temperature = 25,
        sunlight = 90,
        background = "Background_Shallow"
    }
}

--TODO: check this numbers.
