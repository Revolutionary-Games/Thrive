bacteriaTable = {
	["Rickettsia"] = {
		mesh = "mitochondrion.mesh",
		processes = {
            -- This should probably be something else?
			["Photosynthesis"] = 0.6,
		},

		compounds = {
			["co2"] = 80,
		},

		capacity = 50,
		mass = 0.4,
		health = 10
	},

    ["Cyanobacteria"] = {
		mesh = "chloroplast.mesh",
		processes = {
			["Photosynthesis"] = 0.6,
		},

		compounds = {
			["co2"] = 80,
		},

		capacity = 50,
		mass = 0.4,
		health = 10,
        organelle = "chloroplast"
	}
}
