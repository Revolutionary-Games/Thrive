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

    processes:  A table with all the processes this organelle does,
                and the capacity of the process
                TODO: put it in the procesOrganelle component?
]]

organelleTable = {
    ["nucleus"] = {
        mass = 0.7,
        gene = "N",

        components = {
            ["NucleusOrganelle"] = {}
        },

        processes = {
            ["FattyAcidSynthesis"] = 1,
            ["AminoAcidSynthesis"] = 1
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
        mpCost = 5,
        mesh = nil, --it's an empty hex
        hexes = {
            {["q"]=0,   ["r"]=0}
        }
    },

    ["chloroplast"] = {
        components = {
            ["ProcessOrganelle"] = {
                colourChangeFactor = 1.0,


            }
        },

        processes = {
            ["Photosynthesis"] = 1
        },

        mass = 0.4,
        gene = "H",
        mpCost = 20,
        mesh = "chloroplast.mesh",
        hexes = {
            {["q"]=0,   ["r"]=0},
            {["q"]=1,   ["r"]=0},
            {["q"]=0,   ["r"]=1}
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
            ["OxyToxySynthesis"] = 1
        },

        mass = 0.3,
        gene = "T",
        mpCost = 40,
        mesh = "oxytoxy.mesh",
        hexes = {
            {["q"]=0,   ["r"]=0}
        }
    },

    ["mitochondrion"] = {
        components = {
            ["ProcessOrganelle"] = {
                colourChangeFactor = 1.0,
            }
        },

        processes = {
            ["Respiration"] = 1
        },

        mass = 0.3,
        gene = "M",
        mpCost = 20,
        mesh = "mitochondrion.mesh",
        hexes = {
            {["q"]=0, ["r"]=0},
            {["q"]=0, ["r"]=1}
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
        mpCost = 15,
        mesh = "vacuole.mesh",
        hexes = {
            {["q"]=0,   ["r"]=0},
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
        mpCost = 25,
        mesh = "flagellum.mesh",
        hexes = {
            {["q"]=0,   ["r"]=0},
        }
    }
}