HEX_SIZE = 1.0

HEX_SIDE = {
    TOP          = 1,
    TOP_RIGHT    = 2,
    BOTTOM_RIGHT = 3,
    BOTTOM       = 4,
    BOTTOM_LEFT  = 5,
    TOP_LEFT     = 6
}


HEX_SIDE_NAME = {
    [HEX_SIDE.TOP]          = "top",
    [HEX_SIDE.TOP_RIGHT]    = "top_right",
    [HEX_SIDE.BOTTOM_RIGHT] = "bottom_right",
    [HEX_SIDE.BOTTOM]       = "bottom",
    [HEX_SIDE.BOTTOM_LEFT]  = "bottom_left",
    [HEX_SIDE.TOP_LEFT]     = "top_left",
}


OPPOSITE_HEX_SIDE = {
    [HEX_SIDE.TOP]          = HEX_SIDE.BOTTOM,
    [HEX_SIDE.TOP_RIGHT]    = HEX_SIDE.BOTTOM_LEFT,
    [HEX_SIDE.BOTTOM_RIGHT] = HEX_SIDE.TOP_LEFT,
    [HEX_SIDE.BOTTOM]       = HEX_SIDE.TOP,
    [HEX_SIDE.BOTTOM_LEFT]  = HEX_SIDE.TOP_RIGHT,
    [HEX_SIDE.TOP_LEFT]     = HEX_SIDE.BOTTOM_RIGHT,
}


HEX_NEIGHBOUR_OFFSET = {
    [HEX_SIDE.TOP]          = { 0,  1},
    [HEX_SIDE.TOP_RIGHT]    = { 1,  0},
    [HEX_SIDE.BOTTOM_RIGHT] = { 1, -1},
    [HEX_SIDE.BOTTOM]       = { 0, -1},
    [HEX_SIDE.BOTTOM_LEFT]  = {-1,  0},
    [HEX_SIDE.TOP_LEFT]     = {-1,  1}
}

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

function axialToCartesian(q, r)
    local x = q * HEX_SIZE * 3 / 2
    local y = HEX_SIZE * math.sqrt(3) * (r + q / 2)
    return x, y
end

local OFFSET = 100
local SHIFT = OFFSET * 10

function encodeAxial(q, r)
    assert(
        math.abs(q) <= OFFSET and math.abs(r) <= OFFSET, 
        "Coordinates out of range, q and r need to be smaller than " .. OFFSET
    )
    return (q + OFFSET) * SHIFT + (r + OFFSET)
end

function decodeAxial(s)
    local r = (s % SHIFT) - OFFSET
    local q = (s - r - OFFSET) / SHIFT - OFFSET
    return q, r
end
