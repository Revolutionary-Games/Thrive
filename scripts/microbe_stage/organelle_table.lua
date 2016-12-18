organelleTable = {
    ["nucleus"] = {
        mass = 0.7,
        components = {
            "NucleusOrganelle"
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
            "StorageOrganelle"
        },
        mass = 0.1,
        mpCost = 5,
        mesh = nil, --it's an empty hex
        hexes = {
            {["q"]=0,   ["r"]=0}
        }
    },

    ["chloroplast"] = {
        components = {
            "ProcessOrganelle"
        },
        mass = 0.4,
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
            "AgentVacuole"
        },
        mass = 0.3,
        mpCost = 40,
        mesh = "AgentVacuole.mesh",
        colour = ColourValue(0, 1, 1, 0),
        hexes = {
            {["q"]=0,   ["r"]=0}
        }
    },

    ["mitochondrion"] = {
        components = {
            "ProcessOrganelle"
        },
        mass = 0.3,
        mpCost = 20,
        mesh = "mitochondrion.mesh",
        hexes = {
            {["q"]=0, ["r"]=0},
            {["q"]=0, ["r"]=1}
        }
    },

    ["vacuole"] = {
        components = {
            "StorageOrganelle"
        },
        mass = 0.4,
        mpCost = 15,
        mesh = "vacuole.mesh",
        hexes = {
            {["q"]=0,   ["r"]=0},
        }
    },

    ["flagellum"] = {
        components = {
            "MovementOrganelle"
        },
        mass = 0.3,
        mpCost = 25,
        mesh = "flagellum.mesh",
        hexes = {
            {["q"]=0,   ["r"]=0},
        }
    }
}