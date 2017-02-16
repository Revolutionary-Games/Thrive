-- Defines some often-used colours by name for easier access
local COLOURS = {
    Transparent = {0.0, 0.0, 0.0, 0.0},
    Black       = {0.0, 0.0, 0.0, 0.0},
    DarkGrey    = {0.25, 0.25, 0.25, 1.0},
    Grey        = {0.5, 0.5, 0.5, 1.0},
    LightGrey   = {0.75, 0.75, 0.75, 1.0},
    White       = {1.0, 1.0, 1.0, 1.0},
    -- Primary colours
    Red         = {1.0, 0.0, 0.0, 1.0},
    Green       = {0.0, 1.0, 0.0, 1.0},
    Blue        = {0.0, 0.0, 1.0, 1.0},
}

-- Put the above defined colours into COLOURS table
COLOURS = {}
for name, components in pairs(COLOURS) do
    COLOURS[name] = ColourValue.new(
        components[1],
        components[2],
        components[3],
        components[4]
    )
end
