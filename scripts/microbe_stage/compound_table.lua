--[[
Compound atributes:
    name:   The name of the compound, used in the GUI.

    mass:   How heavy an unit of compound is.

    size:   The volume of an unit of compound.

    isCloud:   whether the compound forms clouds (maybe it should be checked
                automatically in the biomes?). By default is false.

    colour: A table with the colour of thecompound, used in the clouds and in the GUI.
]]

compoundTable = {
    ["atp"] = {
        name = "ATP",
        weight = 1,
        size = 0.1
    },

    ["oxygen"] = {
        name = "Oxygen",
        weight = 1,
        size = 0.3,
        isCloud = true,

        colour = {
            r = 60,
            g = 160,
            b = 180
        }
    },

    ["aminoacids"] = {
        name = "Amino Acids",
        weight = 1,
        size = 0.2,
        isCloud = true, --Note that in the biome-table branch no aminoacid cloud would form.

        colour = {
            r = 255,
            g = 150,
            b = 200
        }
    },

    ["ammonia"] = {
        name = "Ammonia",
        weight = 1,
        size = 0.16,
        isCloud = true,

        colour = {
            r = 255,
            g = 220,
            b = 50
        }
    },

    ["glucose"] = {
        name = "Glucose",
        weight = 1,
        size = 0.3,
        isCloud = true,

        colour = {
            r = 150,
            g = 170,
            b = 180
        }
    },

    ["co2"] = {
        name = "CO2",
        weight = 1,
        size = 0.16,
        isCloud = true,

        colour = {
            r = 20,
            g = 50,
            b = 100
        }
    },

    ["fattyacids"] = {
        name = "Fatty Acids",
        weight = 1,
        size = 0.16
    }
}