organelleTable = {
    ["nucleus"] = {
        mass = 0.7,
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
        mass = 0.1,
        mpCost = 5,
        mesh = nil, --it's an empty hex
        hexes = {
        {["q"]=0,   ["r"]=0}
        }
    },

    ["chloroplast"] = {
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
        mass = 0.3,
        mpCost = 40,
        mesh = "AgentVacuole.mesh",
        hexes = {
        {["q"]=0,   ["r"]=0}
        }
    },

    ["mitochondrion"] = {
        mass = 0.3,
        mpCost = 20,
        mesh = "mitochondrion.mesh",
        hexes = {
        {["q"]=0, ["r"]=0},
        {["q"]=0, ["r"]=1}
        }
    },

    ["vacuole"] = {
        mass = 0.4,
        mpCost = 15,
        mesh = "vacuole.mesh",
        hexes = {
        {["q"]=0,   ["r"]=0},
        }
    },

    ["flagellum"] = {
        mass = 0.3,
        mpCost = 25,
        mesh = "flagellum.mesh",
        hexes = {
        {["q"]=0,   ["r"]=0},
        }
    },

    --[[["pilus"] = {
        mpCost = 0,
        mesh = "pilus.mesh",
        hexes = {
        }
    },

    ["cilia"] = {
        mpCost = 0,
        mesh = "cilia.mesh",
        hexes = {
        }
    }]]
}