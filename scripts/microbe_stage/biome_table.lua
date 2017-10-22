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
            oxygen = {amount = 65000, density = 1/5000},
            co2 = {amount = 65000, density = 1/5000},
            ammonia = {amount = 65000, density = 1/5000},
            glucose = {amount = 65000, density = 1/5000}
        },

        bacteria = {
            Rickettsia = 20
        }
    },

    ["volcanic_vent"] = {
        temperature = 100,
        sunlight = 40,
        background = "Background_Vent",
        compounds = {
            oxygen = {amount = 60000, density = 1/5000},
            co2 = {amount = 100000, density = 1/5000},
            ammonia = {amount = 85000, density = 1/5000},
            glucose = {amount = 65000, density = 1/5000}
        },

        bacteria = {
            Rickettsia = 20
        }
    },

    ["abyss"] = {
        temperature = 35,
        sunlight = 5,
        background = "Background_Abyss",
        compounds = {
            oxygen = {amount = 50000, density = 1/5000},
            co2 = {amount = 70000, density = 1/5000},
            ammonia = {amount = 70000, density = 1/5000},
            glucose = {amount = 80000, density = 1/5000}
        },

        bacteria = {
            Rickettsia = 20
        }
    },

    ["shallow"] = {
        temperature = 25,
        sunlight = 90,
        background = "Background_Shallow",
        compounds = {
            oxygen = {amount = 100000, density = 1/5000},
            co2 = {amount = 70000, density = 1/5000},
            ammonia = {amount = 60000, density = 1/5000},
            glucose = {amount = 70000, density = 1/5000}
        },

        bacteria = {
            Rickettsia = 20
        }
    }
}

--TODO: check this numbers.
