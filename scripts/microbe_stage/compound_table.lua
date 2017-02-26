--[[
Compound atributes:
    name:   The name of the compound, used in the GUI.

    mass:   How heavy an unit of compound is.

    size:   The volume of an unit of compound.

    isCloud:   whether the compound forms clouds (maybe it should be checked
                automatically in the biomes?). By default is false.

    colour: A table with the colour of thecompound, used in the clouds and in the GUI.

    default_treshold:   A table with a treshold. TODO: write what a treshold is (i got no idea).
]]

compoundTable = {
    ["atp"] = {
        name = "ATP",
        weight = 1,
        size = 0.1,

        default_treshold = {
            low = 13,
            high = 16,
            vent = 1000
        }
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
        },

        default_treshold = {
            low = 22,
            high = 40,
            vent = 70
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
        },

        default_treshold = {
            low = 0,
            high = 30,
            vent = 70
        }
    },

    ["ammonia"] = {
        name = "Ammonia",
        weight = 1,
        size = 0.16,
        isCloud = true,

        colour = {
            r = 200,
            g = 180,
            b = 25
        },

        default_treshold = {
            low = 12,
            high = 30,
            vent = 70
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
        },

        default_treshold = {
            low = 16,
            high = 30,
            vent = 70
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
        },

        default_treshold = {
            low = 0,
            high = 0,
            vent = 0
        }
    },

    ["pyruvate"] = {
        name = "Pyruvate",
        weight = 1,
        size = 0.16,
        isCloud = true,

        -- No idea about this one :/
        colour = {
            r = 40,
            g = 160,
            b = 60
        },

        default_treshold = {
            low = 0,
            high = 30,
            vent = 70
        }
    },

    ["fattyacids"] = {
        name = "Fatty Acids",
        weight = 1,
        size = 0.16,

        default_treshold = {
            low = 0,
            high = 30,
            vent = 70
        }
    },

    ["proteins"] = {
        name = "Proteins",
        weight = 1,
        size = 0.16,
        isCloud = true,

        colour = {
            r = 240,
            g = 50,
            b = 30
        },

        default_treshold = {
            low = 0,
            high = 30,
            vent = 70
        }
    },

    ["nucleotide"] = {
        name = "Nucleotide",
        weight = 1,
        size = 0.16,

        default_treshold = {
            low = 0,
            high = 30,
            vent = 70
        }
    },

    ["nucleicacids"] = {
        name = "Nucleic Acids",
        weight = 1,
        size = 0.16,

        default_treshold = {
            low = 0,
            high = 30,
            vent = 70
        }
    },

    ["hydrogensulfide"] = {
        name = "Hydrogen Sulfide",
        weight = 1,
        size = 0.16,
        isCloud = true,

        colour = {
            r = 255,
            g = 230,
            b = 100
        },

        default_treshold = {
            low = 0,
            high = 30,
            vent = 70
        }
    }
}