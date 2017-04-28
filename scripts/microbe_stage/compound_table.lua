--[[
Compound atributes:
    name:       The name of the compound, used in the GUI.
    
    volume:     The volume of an unit of compound.

    isCloud:    Whether the compound forms clouds (maybe it should be checked
                automatically in the biomes?). By default is false.

    colour:     A table with the colour of the compound, used in the clouds and in the GUI.

    isUseful:   Whether this compound has some use for microbes besides being transformed
                into other compounds (i.e.: ATP is useful because the microbe spends it to move).

	convertsTo: An optional attribute, a table of compounds with some amount.
                For each "unit" of the original compound that would be created as a cloud,
                an amount specified on the table will be created instead.
                If this attribute is used then the attribute "isCloud" is ignored.
]]

compoundTable = {
    ["atp"] = {
        name = "ATP",
        volume = 1,
        isUseful = true,

        convertsTo = {
            glucose = 1 / 40,
            oxygen = 1 / 5
        } -- should it be less?
    },

    ["oxygen"] = {
        name = "Oxygen",
        volume = 1,
        isCloud = true,

        colour = {
            r = 60,
            g = 160,
            b = 180
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
            r = 150,
            g = 170,
            b = 180
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
        convertsTo = {ammonia = 0.5}
    }
}