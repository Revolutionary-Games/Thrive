-- Holds config file contents, translated into lua table form

compounds = {
    atp = {
        name = "ATP",
        weight = 1,
        mesh = "ATP.mesh",
        size = 0.1
    },
    oxygen = {
        name = "Oxygen",
        weight = 1,
        mesh = "molecule.mesh",
        size = 0.3
    },
    reproductase = {
        name = "Reproductase",
        weight = 1,
        mesh = "reproductase.mesh",
        size = 0.09
    },
    aminoacids = {
        name = "Amino Acids",
        weight = 1,
        mesh = "aminoacid.mesh",
        size = 0.2
    },
    ammonia = {
        name = "Ammonia",
        weight = 1,
        mesh = "ammonia.mesh",
        size = 0.16
    },
    glucose = {
        name = "Glucose",
        weight = 1,
        mesh = "glucose.mesh",
        size = 0.3
    },
    co2 = {
        name = "CO2",
        weight = 1,
        mesh = "co2.mesh",
        size = 0.16
    }
}

agents = {
    oxytoxy = {
        name = "OxyToxy NT",
        mesh = "oxytoxy.mesh",
        size = 0.3,
        effect = oxyToxyEffect 
        -- we'll have to be careful with this referencing
    }
}

processes = {
    Respiration = {
        speedFactor = 0.1,
        energyCost = 0,
        inputs = {
            glucose = 1,
            oxygen = 6
        },
        outputs = {
            co2 = 6,
            atp = 36
        },
    },
    ReproductaseSynthesis = {
        speedFactor = 0.5,
        energyCost = 6,
        inputs = {
            aminoacids = 6,
            glucose = 6,
            oxygen = 6,
            atp = 6
        },
        outputs = {
            reproductase = 5
        }
    },
    AminoAcidSynthesis = {
        speedFactor = 1,
        energyCost = 0,
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
    OxyToxySynthesis = {
        speedFactor = 0.1,
        energyCost = 1,
        inputs = {
            oxygen = 3
        },
        outputs = {
            oxytoxy = 1
        }
    },
    Photosynthesis = {
        speedFactor = 0.03,
        energyCost = 0,
        inputs = {
            co2 = 6
        },
        outputs = {
            glucose = 1,
            oxygen = 6
        }
    }
}

starter_microbes = {
    Default = {
        compounds = {
            atp = {priority=10,amount=40},
            glucose = {amount = 5},
            reproductase = {priority = 8}
        },
        organelles = {
            {name="nucleus",q=0,r=0},
            {name="mitochondrion",q=1,r=-1},
            {name="vacuole",q=0,r=-1},
            {name="vacuole",q=-1,r=0},
            {name="flagellum",q=0,r=1},
            {name="flagellum",q=-1,r=1},
            {name="flagellum",q=1,r=0},
            {name="flagellum",q=0,r=-2},
            {name="flagellum",q=-1,r=-1},
            {name="flagellum",q=1,r=-2}
        }
    },
    Teeny = {
        compounds = {
            atp = {amount=60}
        },
        organelles = {
            {name="nucleus",q=0,r=0},
            {name="vacuole",q=-1,r=0},
            {name="mitochondrion",q=1,r=-1},
            {name="flagellum",q=0,r=-1}
        }
    },
}
