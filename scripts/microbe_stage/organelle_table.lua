--[[
Organelle atributes:
    mass:   How heavy an organelle is. Affects speed, mostly.

    components: A table with the components an organelle has, plus
                the arguments the component needs to initialize.
                Refer to the particular component's lua file for
                more information.

    mpCost: The cost (in mutation points) an organelle costs in the
            microbe editor.

    mesh:   The name of the mesh file of the organelle.
            It has to be in the models folder.

    hexes:  A table of the hexes that the organelle occupies.
            Each hex it's represented by a table that looks like this:
                {["q"]=q,   ["r"]=r}
            where q and r are the hex position in axial coordinates.

    gene:   The letter that will be used by the auto-evo system to
            identify this organelle.

    chanceToCreate: The (relative) chance this organelle will appear in a randomly
                    generated or mutated microbe (to do roulette selection).

    processes:  A table with all the processes this organelle does,
                and the capacity of the process (the amount of
                process that can be made in one second).
                TODO: put it in the procesOrganelle component?

    composition:    A table with the compounds that compost the organelle.
                    They are needed in order to split the organelle, and a
                    percentage of them is released upon death of the microbe.
]]

organelleTable = {
    ["nucleus"] = {
        mass = 0.7,
        gene = "N",
        chanceToCreate = 0, -- Not randomly generated.

        components = {
            ["NucleusOrganelle"] = {}
        },

        processes = {
            ["FattyAcidSynthesis"] = 0.2,
            ["AminoAcidSynthesis"] = 0.2
        },

        mpCost = 0, --it's not supossed to be purchased.
        mesh = "nucleus.mesh",
        hexes = {
            {["q"]=0,   ["r"]=0},
            {["q"]=1,   ["r"]=0},
            {["q"]=0,   ["r"]=1},
            {["q"]=0,   ["r"]=-1},
            {["q"]=1,   ["r"]=-1},
            {["q"]=-1,  ["r"]=1},
            {["q"]=-1,  ["r"]=0},
            {["q"]=-1,  ["r"]=-1},
            {["q"]=0,   ["r"]=-2},
            {["q"]=1,   ["r"]=-2}
        },

        composition = {
            aminoacids = 4
        }
    },

    ["cytoplasm"] = {
        components = {
            ["StorageOrganelle"] = {
                capacity = 10.0
            }
        },

        mass = 0.1,
        gene = "Y",
        chanceToCreate = 1,
        mpCost = 5,
        mesh = nil, --it's an empty hex
        hexes = {
            {["q"]=0,   ["r"]=0}
        },

        composition = {
            aminoacids = 3,
            glucose = 2,
            -- fattyacids = 0 :/
        }
    },

    ["chloroplast"] = {
        components = {
            ["ProcessOrganelle"] = {
                colourChangeFactor = 1.0,


            }
        },

        processes = {
            ["Photosynthesis"] = 0.2
        },

        mass = 0.4,
        gene = "H",
        chanceToCreate = 2,
        mpCost = 20,
        mesh = "chloroplast.mesh",
        hexes = {
            {["q"]=0,   ["r"]=0},
            {["q"]=1,   ["r"]=0},
            {["q"]=0,   ["r"]=1}
        },

        composition = {
            aminoacids = 4,
            glucose = 2,
            -- fattyacids = 0 :/
        }
    },

    ["oxytoxy"] = {
        components = {
            ["AgentVacuole"] = {
                compound = "oxytoxy",
                process = "OxyToxySynthesis"
            }
        },

        processes = {
            ["OxyToxySynthesis"] = 0.05
        },

        mass = 0.3,
        gene = "T",
        chanceToCreate = 1,
        mpCost = 40,
        mesh = "oxytoxy.mesh",
        hexes = {
            {["q"]=0,   ["r"]=0}
        },

        composition = {
            aminoacids = 4,
            glucose = 2,
            -- fattyacids = 0 :/
        }
    },

    ["mitochondrion"] = {
        components = {
            ["ProcessOrganelle"] = {
                colourChangeFactor = 1.0,
            }
        },

        processes = {
            ["Respiration"] = 0.07
        },

        mass = 0.3,
        gene = "M",
        chanceToCreate = 3,
        mpCost = 20,
        mesh = "mitochondrion.mesh",
        hexes = {
            {["q"]=0, ["r"]=0},
            {["q"]=0, ["r"]=1}
        },

        composition = {
            aminoacids = 4,
            glucose = 2,
            -- fattyacids = 0 :/
        }
    },

    ["vacuole"] = {
        components = {
            ["StorageOrganelle"] = {
                capacity = 100.0
            }
        },

        mass = 0.4,
        gene = "V",
        chanceToCreate = 3,
        mpCost = 15,
        mesh = "vacuole.mesh",
        hexes = {
            {["q"]=0,   ["r"]=0},
        },

        composition = {
            aminoacids = 4,
            glucose = 2,
            -- fattyacids = 0 :/
        }
    },

    ["flagellum"] = {
        components = {
            ["MovementOrganelle"] = {
                momentum = 20,
                torque = 300
            }
        },

        mass = 0.3,
        gene = "F",
        chanceToCreate = 3,
        mpCost = 25,
        mesh = "flagellum.mesh",
        hexes = {
            {["q"]=0,   ["r"]=0},
        },

        composition = {
            aminoacids = 4,
            glucose = 2,
            -- fattyacids = 0 :/
        }
    }
}