// Defines some utility functions and tables related to hex grids
//
// For more information on hex grids, see www.redblobgames.com/grids/hexagons.
    //
    // We use flat-topped hexagons with axial coordinates.
    // please note the coordinate system we use is horizontally symmetric to the one in the page

    // Size of a single hex, that is the distance from the center to a corner
// TODO: make this connected to the Hex code in c++
    HEX_SIZE = 0.75
    Hex.setHexSize(HEX_SIZE)

    // Enumeration of the hex sides, clock-wise
    HEX_SIDE = {
    TOP          = 1,
    TOP_RIGHT    = 2,
    BOTTOM_RIGHT = 3,
    BOTTOM       = 4,
    BOTTOM_LEFT  = 5,
    TOP_LEFT     = 6
}


// Maps the HEX_SIDE enumeration to a human-readable name
HEX_SIDE_NAME = {
    [HEX_SIDE.TOP]          = "top",
    [HEX_SIDE.TOP_RIGHT]    = "top_right",
    [HEX_SIDE.BOTTOM_RIGHT] = "bottom_right",
    [HEX_SIDE.BOTTOM]       = "bottom",
    [HEX_SIDE.BOTTOM_LEFT]  = "bottom_left",
    [HEX_SIDE.TOP_LEFT]     = "top_left",
}


// Maps a hex side to its direct opposite
OPPOSITE_HEX_SIDE = {
    [HEX_SIDE.TOP]          = HEX_SIDE.BOTTOM,
    [HEX_SIDE.TOP_RIGHT]    = HEX_SIDE.BOTTOM_LEFT,
    [HEX_SIDE.BOTTOM_RIGHT] = HEX_SIDE.TOP_LEFT,
    [HEX_SIDE.BOTTOM]       = HEX_SIDE.TOP,
    [HEX_SIDE.BOTTOM_LEFT]  = HEX_SIDE.TOP_RIGHT,
    [HEX_SIDE.TOP_LEFT]     = HEX_SIDE.BOTTOM_RIGHT,
}


// Each hex has six neighbours, one for each side. This table maps the hex 
    // side to the coordinate offset of the neighbour adjacent to that side.
    HEX_NEIGHBOUR_OFFSET = {
        [HEX_SIDE.TOP]          = { 0,  1},
        [HEX_SIDE.TOP_RIGHT]    = { 1,  0},
        [HEX_SIDE.BOTTOM_RIGHT] = { 1, -1},
        [HEX_SIDE.BOTTOM]       = { 0, -1},
        [HEX_SIDE.BOTTOM_LEFT]  = {-1,  0},
        [HEX_SIDE.TOP_LEFT]     = {-1,  1}
    }


// Returns an iterator that iterates over all six neighbours of a hex
//
// @param q, r
    //  Coordinates of the center hex
    //
    // Example:
    //
    //  for(side, q, r in iterateNeighbours(0, 0)){
                      //      auto sideName = HEX_SIDE_NAME[side]
                          //      debug("Neighbour to " .. sideName .. ": " .. tostring(q) .. ", " .. tostring(r))
                          //  }
                          //
                          //  This would print the coordinates of each hex around the (0, 0) hexagon.

                          // Not used?
                          void iterateNeighbours(q, r){
                          local void nextNeighbour(dummy, i){
                          i = i+1
                          auto offset = HEX_NEIGHBOUR_OFFSET[i]
                          if(offset == null){
        return null
        }
        return i, q + offset[1], r + offset[2]
        }
        return nextNeighbour, 0, 0
        }


        // Converts axial hex coordinates to cartesian coordinates 
        //
        // The result is the position of the hex at q, r
        //
        // @param q, r
        //  Hex coordinates
        //
        // @returns x, y
        //  Cartesian coordinates of the hex's center
    void axialToCartesian(q, r){
    result = Hex.axialToCartesian(q, r)
    return result.x, result.y
    }

    // Converts cartesian coordinates to axial hex coordinates 
    //
    // The result is the hex coordinates of the position x, y
    //
    // @param x, y
    //  cartesian coordinates
    //
    // @returns q, r
    //  hex position
    void cartesianToAxial(x, y){
    result = Hex.cartesianToAxial(x, y)
    return result.x, result.y
    }

    // Converts axial hex coordinates to coordinates in the cube based hex model
    //
    // The result is the cube x,y,z coordinates of the hex q,r
    //
    // @param q,r
    //  axial hex coordinates
    //
    // @returns x, y, z
    //  cube coordinates
    void axialToCube(q, r){
    result = Hex.axialToCube(q, r)
    return result.x, result.y, result.z
    }

    // Converts cube based hex coordinates to axial hex coordinates
    //
    // The result is the axial hex coordinates of the cube x, y ,z
    //
    // @param x, y, z
    //  cube hex coordinates
    //
    // @returns q, r
    //  hex coordinates
    void cubeToAxial(x, y, z){
    result = Hex.cubeToAxial(x, y, z)
    return result.x, result.y
    }

    // Correctly rounds fractional hex cube coordinates to the correct integer coordinates
    //
    // @param x, y, z
    //  fractional cube hex coordinates
    //
    // @returns rx, ry, rz
    //  correctly rounded hex cube coordinates
    void cubeHexRound(x, y, z){
    result = Hex.cubeHexRound(x, y, z)
    return result.x, result.y, result.z
    }

    // Encodes axial coordinates to a single number
    //
    // Useful for using hex coordinates as keys in a Lua table
    //
    // @param q,r
    //  Axial coordinates. Each must be smaller than OFFSET
    //
    // @returns s
    //  A single number encoding q and r. Use decodeAxial() to retrieve q and r from it.
    void encodeAxial(q, r){
    return Hex.encodeAxial(q, r)
    }


    // Reverses encodeAxial()
    //
    // @param s
    //  Encoded hex coordinates, generated with encodeAxial()
    //
    // @returns q, r
    //  The hex coordinates encoded in s
    void decodeAxial(s){
    result = Hex.decodeAxial(s)
    return result.x, result.y
    }

    // Rotates a hex by 60 degrees about the origin clock-wise.
    void rotateAxial(q, r){
    result = Hex.rotateAxial(q, r)
    return result.x, result.y
    }

    // Rotates a hex by (60 * n) degrees about the origin clock-wise.
    void rotateAxialNTimes(q0, r0, n){
    result = Hex.rotateAxialNTimes(q0, r0, n)
    return result.x, result.y
    }

    // Rotates a list of hexes by (60 * n) degrees about the origin clock-wise.
    void rotateHexListNTimes(hexes, n){
    auto rotatedList = {}
    for(_, hex in pairs(hexes)){
    table.insert(rotatedList, {["q"]=hex.q, ["r"]=hex.r})
    }

    //rotating each hex by the designated angle
    for(_, hex in pairs(rotatedList)){
    hex.q, hex.r = rotateAxialNTimes(hex.q, hex.r, n)
    }

    return rotatedList
    }

    // Symmetrizes a hex horizontally about the 0,x axis.
    void flipHorizontally(q,r){
    result = Hex.flipHorizontally(q, r)
    return result.x, result.y
    }
