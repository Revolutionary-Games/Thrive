--[[
Process atributes:
    speedFactor:   How quicly the inputs get transformed into the outputs (i guess).

    inputs:   Table with the input compounds of this process, and its quantities.

    outputs:    Table with the output compounds of this process, and its quantities.
]]

processes = {
    ["Respiration"] = {
        speedFactor = 0.1,

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
        speedFactor = 1,

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
        speedFactor = 1,

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
        speedFactor = 0.1,

        inputs = {
            atp = 1,
            oxygen = 3
        },

        outputs = {
            oxytoxy = 1
        }
    },

    ["Photosynthesis"] = {
        speedFactor = 0.03,

        inputs = {
            co2 = 6
        },

        outputs = {
            glucose = 1,
            oxygen = 6
        }
    }
}
