--[[
Compound atributes:
    name:       The name of the compound, used in the GUI.
    
    volume:     The volume of an unit of compound.

    isCloud:    Whether the compound forms clouds (maybe it should be checked
                automatically in the biomes?). By default is false.

    colour:     A table with the colour of the compound, used in the clouds and in the GUI.

    isUseful:   Whether this compound has some use for microbes besides being transformed
                into other compounds (i.e.: ATP is useful because the microbe spends it to move).
]]

compoundTable = {
    ["atp"] = {
        name = "ATP",
        volume = 1,
        isUseful = true
    },

    ["oxygen"] = {
        name = "Oxygen",
        volume = 1,
        isCloud = true,

        colour = {
            r = 100,
            g = 230,
            b = 230
        }
    },

    ["aminoacids"] = {
        name = "Amino Acids",
        volume = 1,
        isCloud = true, --Note that in the biome-table branch no aminoacid cloud would form.
        isUseful = true,

        colour = {
            r = 255,
            g = 150,
            b = 200
        }
    },

    ["ammonia"] = {
        name = "Ammonia",
        volume = 1,
        isCloud = true,

        colour = {
            r = 255,
            g = 220,
            b = 50
        }
    },

    ["glucose"] = {
        name = "Glucose",
        volume = 1,
        isCloud = true,

        colour = {
            r = 220,
            g = 220,
            b = 220
        }
    },

    ["co2"] = {
        name = "CO2",
        volume = 1,
        isCloud = true,

        colour = {
            r = 20,
            g = 50,
            b = 100
        }
    },

    ["fattyacids"] = {
        name = "Fatty Acids",
        volume = 1,
    }
}