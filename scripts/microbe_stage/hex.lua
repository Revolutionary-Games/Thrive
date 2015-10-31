-- Defines some utility functions and tables related to hex grids
--
-- For more information on hex grids, see www.redblobgames.com/grids/hexagons.
--
-- We use flat-topped hexagons with axial coordinates.

-- Size of a single hex, that is the distance from the center to a corner
HEX_SIZE = 1.0


-- Enumeration of the hex sides, clock-wise
HEX_SIDE = {
    TOP          = 1,
    TOP_RIGHT    = 2,
    BOTTOM_RIGHT = 3,
    BOTTOM       = 4,
    BOTTOM_LEFT  = 5,
    TOP_LEFT     = 6
}


-- Maps the HEX_SIDE enumeration to a human-readable name
HEX_SIDE_NAME = {
    [HEX_SIDE.TOP]          = "top",
    [HEX_SIDE.TOP_RIGHT]    = "top_right",
    [HEX_SIDE.BOTTOM_RIGHT] = "bottom_right",
    [HEX_SIDE.BOTTOM]       = "bottom",
    [HEX_SIDE.BOTTOM_LEFT]  = "bottom_left",
    [HEX_SIDE.TOP_LEFT]     = "top_left",
}


-- Maps a hex side to its direct opposite
OPPOSITE_HEX_SIDE = {
    [HEX_SIDE.TOP]          = HEX_SIDE.BOTTOM,
    [HEX_SIDE.TOP_RIGHT]    = HEX_SIDE.BOTTOM_LEFT,
    [HEX_SIDE.BOTTOM_RIGHT] = HEX_SIDE.TOP_LEFT,
    [HEX_SIDE.BOTTOM]       = HEX_SIDE.TOP,
    [HEX_SIDE.BOTTOM_LEFT]  = HEX_SIDE.TOP_RIGHT,
    [HEX_SIDE.TOP_LEFT]     = HEX_SIDE.BOTTOM_RIGHT,
}


-- Each hex has six neighbours, one for each side. This table maps the hex 
-- side to the coordinate offset of the neighbour adjacent to that side.
HEX_NEIGHBOUR_OFFSET = {
    [HEX_SIDE.TOP]          = { 0,  1},
    [HEX_SIDE.TOP_RIGHT]    = { 1,  0},
    [HEX_SIDE.BOTTOM_RIGHT] = { 1, -1},
    [HEX_SIDE.BOTTOM]       = { 0, -1},
    [HEX_SIDE.BOTTOM_LEFT]  = {-1,  0},
    [HEX_SIDE.TOP_LEFT]     = {-1,  1}
}


-- Returns an iterator that iterates over all six neighbours of a hex
--
-- @param q, r
--  Coordinates of the center hex
--
-- Example:
--
--  for side, q, r in iterateNeighbours(0, 0) do
--      local sideName = HEX_SIDE_NAME[side]
--      debug("Neighbour to " .. sideName .. ": " .. tostring(q) .. ", " .. tostring(r))
--  end
--
--  This would print the coordinates of each hex around the (0, 0) hexagon.
function iterateNeighbours(q, r)
    local function nextNeighbour(dummy, i)
        i = i+1
        local offset = HEX_NEIGHBOUR_OFFSET[i]
        if offset == nil then
            return nil
        end
        return i, q + offset[1], r + offset[2]
    end
    return nextNeighbour, 0, 0
end


-- Converts axial hex coordinates to cartesian coordinates 
--
-- The result is the position of the hex at q, r
--
-- @param q, r
--  Hex coordinates
--
-- @returns x, y
--  Cartesian coordinates of the hex's center
function axialToCartesian(q, r)
    local x = q * HEX_SIZE * 3 / 2
    local y = HEX_SIZE * math.sqrt(3) * (r + q / 2)
    return x, y
end

-- Converts cartesian coordinates to axial hex coordinates 
--
-- The result is the hex coordinates of the position x, y
--
-- @param x, y
--  cartesian coordinates
--
-- @returns q, r
--  hex position
function cartesianToAxial(x, y)    
    local q = x / (HEX_SIZE * 3 / 2);
    local r = (y / (HEX_SIZE * math.sqrt(3))) - q/2;
    return q, r
end

-- Converts axial hex coordinates to coordinates in the cube based hex model
--
-- The result is the cube x,y,z coordinates of the hex q,r
--
-- @param q,r
--  axial hex coordinates
--
-- @returns x, y, z
--  cube coordinates
function axialToCube(q, r)
    return q, r, (-q-r)
end

-- Converts cube based hex coordinates to axial hex coordinates
--
-- The result is the axial hex coordinates of the cube x, y ,z
--
-- @param x, y, z
--  cube hex coordinates
--
-- @returns q, r
--  hex coordinates
function cubeToAxial(x, y, z)
    return x, z
end

-- Correctly rounds fractional hex cube coordinates to the correct integer coordinates
--
-- @param x, y, z
--  fractional cube hex coordinates
--
-- @returns rx, ry, rz
--  correctly rounded hex cube coordinates
function cubeHexRound(x, y, z)
    local rx = math.floor(x+0.5)
    local ry = math.floor(y+0.5)
    local rz = math.floor(z+0.5)

    local x_diff = math.abs(rx - x)
    local y_diff = math.abs(ry - y)
    local z_diff = math.abs(rz - z)

    if x_diff > y_diff and x_diff > z_diff then
        rx = -ry-rz
    elseif y_diff > z_diff then
        ry = -rx-rz
    else
        rz = -rx-ry
    end
    return rx, ry, rz
end

-- Maximum hex coordinate value that can be encoded with encodeAxial()
local OFFSET = 256

-- Multiplier for the q coordinate used in encodeAxial()
local SHIFT = OFFSET * 10


-- Encodes axial coordinates to a single number
--
-- Useful for using hex coordinates as keys in a Lua table
--
-- @param q,r
--  Axial coordinates. Each must be smaller than OFFSET
--
-- @returns s
--  A single number encoding q and r. Use decodeAxial() to retrieve q and r from it.
function encodeAxial(q, r)
    assert(
        math.abs(q) <= OFFSET and math.abs(r) <= OFFSET, 
        "Coordinates out of range, q and r need to be smaller than " .. OFFSET
    )
    return (q + OFFSET) * SHIFT + (r + OFFSET)
end


-- Reverses encodeAxial()
--
-- @param s
--  Encoded hex coordinates, generated with encodeAxial()
--
-- @returns q, r
--  The hex coordinates encoded in s
function decodeAxial(s)
    local r = (s % SHIFT) - OFFSET
    local q = (s - r - OFFSET) / SHIFT - OFFSET
    return q, r
end

-- Rotates a hex by 60 degrees about the origin
function rotateAxial(q, r)
	local q2 = -1*r
	local r2 = q + r
	return q2, r2
end
