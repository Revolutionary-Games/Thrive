--[[
Process atributes:
    speedFactor:   How quicly the inputs get transformed into the outputs (i guess).

    inputs:   Table with the input compounds of this process, and its quantities.

    outputs:    Table with the output compounds of this process, and its quantities.
]]

processes = {
    ["Respiration"] = {
        speedFactor = 0.1,

        -- We asume Ac-CoA as an implicit intermediary.
        inputs = {
            pyruvate = 1,
            oxygen = 3
        },

        outputs = {
            co2 = 3,
            atp = 18
        }
    },

    ["AminoAcidSynthesis"] = {
        speedFactor = 1,

        inputs = {
            pyruvate = 1,
            atp = 3,
            ammonia = 1
        },

        outputs = {
            aminoacids = 1
        }
    },

    ["FattyAcidSynthesis"] = {
        speedFactor = 1,

        inputs = {
            pyruvate = 9,
            atp = 56
        },

        outputs = {
            co2 = 9,
            fattyacids = 1
        }
    },

    -- Again, assuming conversion between pyruvate and Acetyl CoA. It might just be easier to have both...
    ["FattyAcidDigestion"] = {
        speedFactor = 1,

        inputs = {
            fattyacids = 1,
        },

        outputs = {
            pyruvate = 6,
            atp = 45
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
    },

    ["Glycolysis"] = {
        speedFactor = 0.03,

        inputs = {
            glucose = 1
        },

        outputs = {
            pyruvate = 2,
            atp = 2
        }
    },

    ["ProteinSynthesis"] = {
        speedFactor = 0.03,

        inputs = {
            aminoacids = 1,
            atp = 4
        },

        outputs = {
            proteins = 1,
        }
    },

    ["ProteinDigestion"] = {
        speedFactor = 0.03,

        inputs = {
            proteins = 1
        },

        outputs = {
            aminoacids = 1 -- Yup, you don't get the ATP back.
        }
    },

    -- Deamination produces an NADH which is equivalent to 2 ATP through the electron chain.
    ["Deamination"] = {
        speedFactor = 0.03,

        inputs = {
            aminoacids = 1
        },

        outputs = {
            atp = 2,
            pyruvate = 1,
            ammonia = 1
        }
    },

    ["NucleotideSynthesis"] = {
        speedFactor = 0.03,

        inputs = {
            glucose = 1,
            atp = 7,
            aminoacids = 2
        },

        outputs = {
            nucleotide = 1
        }
    },

    -- Not sure yet. Possibly directly to uric acid, or not digested at all but reused.
    --[[
    ["NucleotideDigestion"] = {
        speedFactor = 1,

        inputs = {},

        outputs = {}
    },
    ]]

    ["NucleicAcidSynthesis"] = {
        speedFactor = 0.03,

        -- ATP represents the energy within the triphosphate group.
        inputs = {
            nucleotide = 1,
            atp = 1
        },

        outputs = {
            nucleicacids = 1
        }
    },

    ["NucleicAcidDigestion"] = {
        speedFactor = 0.03,

        inputs = {
            nucleicacids = 1
        },

        outputs = {
            nucleotide = 1
        }
    },

    ["Thermosynthesis"] = {
        speedFactor = 1,

        inputs = {
            co2 = 6
        },

        outputs = {
            glucose = 1,
            oxygen = 6
        }
    },

    ["Chemosynthesis"] = {
        speedFactor = 1,

        inputs = {
            hydrogensulfide = 12,
            co2 = 6
        },

        outputs = {
            glucose = 1
            -- +6 sulfurs, but they're kinda useless atm.
        }
    }
}
