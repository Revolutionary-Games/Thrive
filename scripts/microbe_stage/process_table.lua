--[[
Process atributes:
    inputs:   Table with the input compounds of this process, and its quantities.

    outputs:    Table with the output compounds of this process, and its quantities.
]]

processes = {
    ["Respiration"] = {
        inputs = {
            glucose = 1,
            oxygen = 6
        },

        outputs = {
            co2 = 6,
            atp = 36
        }
    },

    ["AminoAcidSynthesis"] = {
        inputs = {
            glucose = 1,
            ammonia = 1
        },

        outputs = {
            co2 = 1,
            atp = 1,
            aminoacids = 1
        }
    },

    ["FattyAcidSynthesis"] = {
        inputs = {
            glucose = 1,
            ammonia = 1
        },

        outputs = {
            co2 = 1,
            atp = 1,
            fattyacids = 1
        }
    },

    ["OxyToxySynthesis"] = {
        inputs = {
            atp = 1,
            oxygen = 3
        },

        outputs = {
            oxytoxy = 1
        }
    },

    ["Photosynthesis"] = {
        inputs = {
            co2 = 6
        },

        outputs = {
            glucose = 1,
            oxygen = 6
        }
    },

    ["SulfurRespiration"] = {
        inputs = {
            glucose = 1,
            sulfur = 6
        },

        outputs = {
            co2 = 6,
            hydrogensulfide = 6,
            atp = 16
        }
    },

    ["Chemosynthesis"] = {
        inputs = {
            co2 = 6,
            hydrogensulfide = 12
        },

        outputs = {
            glucose = 1,
            sulfur = 12
        }
    }
}
