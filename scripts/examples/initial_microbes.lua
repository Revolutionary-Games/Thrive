-- The initial microbes pre 0.3.3.
-- Kept in here as an example of organelle placement

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
        colour = {r=1,g=1,b=1},
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
        colour = {r=1,g=1,b=0.3},
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
        colour = {r=0.4,g=0.4,b=0.8},
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
        colour = {r=0.1,g=1,b=0.5},
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
        colour = {r=1,g=0.3,b=0.5},
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
        colour = {r=0.4,g=0.6,b=1},
    },

    Gluttonous = {
        spawnDensity = 1/18000,
        compounds = {
            atp = {amount = 60},
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
        colour = {r=0.3,g=1,b=0.8},
    },
}
