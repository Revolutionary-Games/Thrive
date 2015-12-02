-- Holds config file contents, translated into lua table form

--[[
Style tips:
- Always indent, only with spaces, 4 spaces to an indent.
- Always leave trailing commas on the last elements of multiline tables
    then, the next time something gets added, you don't need to change the line above
    just keeps things cleaner.
- Always leave a trailing newline at the end of the file. 
    Google it, it is the right way to end text files.
]]

compounds = {
    atp = {
        name = "ATP",
        weight = 1,
        mesh = "ATP.mesh",
        size = 0.1,
    },
    oxygen = {
        name = "Oxygen",
        weight = 1,
        mesh = "molecule.mesh",
        size = 0.3,
    },
    reproductase = {
        name = "Reproductase",
        weight = 1,
        mesh = "reproductase.mesh",
        size = 0.09,
    },
    aminoacids = {
        name = "Amino Acids",
        weight = 1,
        mesh = "aminoacid.mesh",
        size = 0.2,
    },
    ammonia = {
        name = "Ammonia",
        weight = 1,
        mesh = "ammonia.mesh",
        size = 0.16,
    },
    glucose = {
        name = "Glucose",
        weight = 1,
        mesh = "glucose.mesh",
        size = 0.3,
    },
    co2 = {
        name = "CO2",
        weight = 1,
        mesh = "co2.mesh",
        size = 0.16,
    },
}

agents = {
    oxytoxy = {
        name = "OxyToxy NT",
        mesh = "oxytoxy.mesh",
        size = 0.3,
        effect = "oxytoxyEffect",
        --[[
        we'll have to be careful with this referencing. 
        should we link the function directly? Or just name? 
        link should be cleaner to use, but may be more difficult to set up?
        ]]
    },
}

processes = {
    Respiration = {
        speedFactor = 0.1,
        energyCost = 0,
        inputs = {
            glucose = 1,
            oxygen = 6,
        },
        outputs = {
            co2 = 6,
            atp = 36,
        },
    },
    ReproductaseSynthesis = {
        speedFactor = 0.5,
        energyCost = 6,
        inputs = {
            aminoacids = 6,
            glucose = 6,
            oxygen = 6,
            atp = 6,
        },
        outputs = {
            reproductase = 5,
        },
    },
    AminoAcidSynthesis = {
        speedFactor = 1,
        energyCost = 0,
        inputs = {
            glucose = 1,
            ammonia = 1,
        },
        outputs = {
            co2 = 1,
            atp = 1,
            aminoacids = 1,
        },
    },
    OxyToxySynthesis = {
        speedFactor = 0.1,
        energyCost = 1,
        inputs = {
            oxygen = 3,
        },
        outputs = {
            oxytoxy = 1,
        },
    },
    Photosynthesis = {
        speedFactor = 0.03,
        energyCost = 0,
        inputs = {
            co2 = 6,
        },
        outputs = {
            glucose = 1,
            oxygen = 6,
        },
    },
}

--[[
Placing organelles can get downright annoying if you don't
map them out. To make it easier, download a few sheets of hexgrid 
off the internet. Before you print them though, set up the axes
properly. See http://i.imgur.com/kTxHFMC.png for how. When you're
drawing out your microbe, keep in mind that it faces forward along
the +r direction.

0 degrees is considered up for the rotation (+r), and you can rotate
in 60 degree intervals counter clockwise.
]]

starter_microbes = {
    Default = {
        spawnDensity = 1/14000,
        compounds = {
            atp = {priority=10,amount=40},
            glucose = {amount = 5},
            reproductase = {priority = 8},
        },
        organelles = {
            {name="nucleus",q=0,r=0, rotation=0},
            {name="mitochondrion",q=-1,r=-2, rotation=240},
            {name="vacuole",q=1,r=-3, rotation=0},
            {name="flagellum",q=1,r=-4, rotation=0},
            {name="flagellum",q=-1,r=-3, rotation=0},
        },
    },
    Teeny = {
        spawnDensity = 1/9000,
        compounds = {atp = {amount = 60}},
        organelles = {
            {name="nucleus",q=0,r=0, rotation=0},
            {name="vacuole",q=-2,r=0, rotation=0},
            {name="vacuole",q=-2,r=1, rotation=0},
            {name="mitochondrion",q=2,r=-1, rotation=180},
            {name="flagellum",q=0,r=-3, rotation=0},
        },
    },
    Speedy = {
        spawnDensity = 1/15000,
        compounds = {atp = {amount = 30}},
        organelles = {
            {name="nucleus",q=0,r=0, rotation=0},
            {name="mitochondrion",q=-1,r=-2, rotation=240},
            {name="vacuole",q=1,r=-3, rotation=0},
            {name="flagellum",q=1,r=-4, rotation=0},
            {name="flagellum",q=-1,r=-3, rotation=0},
            {name="flagellum",q=0,r=-4, rotation=0},
            {name="flagellum",q=-2,r=-2, rotation=0},
            {name="flagellum",q=2,r=-4, rotation=0},
        },
    },
    Algae = {
        spawnDensity = 1/12000,
        compounds = {atp = {amount = 20}},
        organelles = {
            {name="nucleus", q=0, r=0, rotation=0},
            {name="mitochondrion", q=0, r=2, rotation=240},
            {name="vacuole", q=-1, r=2, rotation=0},
            {name="chloroplast", q=-2, r=2, rotation=120},
            {name="chloroplast", q=-2, r=0, rotation=120},
            {name="chloroplast", q=-1, r=-2, rotation=120},
            {name="chloroplast", q=1, r=-3, rotation=180},
            {name="chloroplast", q=2, r=-2, rotation=180},
            {name="chloroplast", q=2, r=0, rotation=180},
        },
    },
    Poisonous = {
        spawnDensity = 1/32000,
        compounds = {
            atp = {amount = 30},
            oxytoxy = {amount = 15},
        },
        organelles = {
            {name="nucleus", q=0, r=0, rotation=0},
            {name="mitochondrion", q=1, r=1, rotation=240},
            {name="mitochondrion", q=-1, r=2, rotation=120},
            {name="vacuole", q=-2, r=0, rotation=0},
            {name="vacuole", q=-2, r=1, rotation=0},
            {name="vacuole", q=2, r=-1, rotation=0},
            {name="vacuole", q=2, r=-2, rotation=0},
            {name="vacuole", q=0, r=2, rotation=0},
            {name="vacuole", q=-2, r=3, rotation=0},
            {name="vacuole", q=2, r=1, rotation=0},
            {name="vacuole", q=0, r=-3, rotation=0},
            {name="flagellum", q=-2, r=-1, rotation=0},
            {name="flagellum", q=-1, r=-2, rotation=0},
            {name="flagellum", q=2, r=-3, rotation=0},
            {name="flagellum", q=1, r=-3, rotation=0},
        },
    },
    Predator = {
        spawnDensity = 1/15000,
        compounds = {
            atp = {amount = 30},
            oxytoxy = {amount = 20},
        },
        organelles = {
            {name="nucleus", q=0, r=0, rotation=0},
            {name="vacuole", q=-2, r=1, rotation=0},
            {name="vacuole", q=2, r=-1, rotation=0},
            {name="mitochondrion", q=2, r=-2, rotation=180},
            {name="mitochondrion", q=-2, r=1, rotation=180},
            {name="flagellum", q=0, r=-3, rotation=0},
            {name="flagellum", q=1, r=-3, rotation=0},
            {name="flagellum", q=-1, r=-2, rotation=0},
            {name="oxytoxy", q=0, r=2, rotation=0},
        },
    },
    Gluttonous = {
        spawnDensity = 1/18000,
        compounds = {
            atp = {amount = 30},
        },
        organelles = {
            {name="nucleus", q=0, r=0, rotation=0},
            {name="mitochondrion", q=-2,  r=1, rotation=180},
            {name="mitochondrion", q=2, r=-1, rotation=180},
            {name="mitochondrion", q=-3,  r=2, rotation=0},
            {name="mitochondrion", q=3,  r=-1, rotation=0},
            {name="mitochondrion", q=-1,  r=2, rotation=120},
            {name="mitochondrion", q=1, r=1, rotation=240},
            {name="vacuole", q=0, r=2, rotation=0},
            {name="vacuole", q=-2, r=3, rotation=0},
            {name="vacuole", q=-3, r=4, rotation=0},
            {name="vacuole", q=-3, r=1, rotation=0},
            {name="vacuole", q=-1, r=-2, rotation=0},
            {name="vacuole", q=1, r=-3, rotation=0},
            {name="vacuole", q=3, r=-2, rotation=0},
            {name="vacuole", q=3, r=1, rotation=0},
            {name="vacuole", q=2, r=1, rotation=0},
            {name="flagellum", q=-2, r=-1, rotation=0},
            {name="flagellum", q=0, r=-3, rotation=0},
            {name="flagellum", q=2, r=-3, rotation=0},
        },
    },
}
