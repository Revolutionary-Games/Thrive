// Defines some utility functions and tables related to hex grids
//
// For more information on hex grids, see www.redblobgames.com/grids/hexagons.
//
// We use flat-topped hexagons with axial coordinates.
// please note the coordinate system we use is horizontally symmetric to the one in the page

// Size of a single hex, that is the distance from the center to a corner
const float HEX_SIZE = Hex::getHexSize();

// Enumeration of the hex sides, clock-wise
enum HEX_SIDE{
    TOP          = 1,
    TOP_RIGHT    = 2,
    BOTTOM_RIGHT = 3,
    BOTTOM       = 4,
    BOTTOM_LEFT  = 5,
    TOP_LEFT     = 6
}


// Maps the HEX_SIDE enumeration to a human-readable name
const dictionary HEX_SIDE_NAME = {
    {HEX_SIDE::TOP, "top"},
    {HEX_SIDE::TOP_RIGHT, "top_right"},
    {HEX_SIDE::BOTTOM_RIGHT, "bottom_right"},
    {HEX_SIDE::BOTTOM, "bottom"},
    {HEX_SIDE::BOTTOM_LEFT, "bottom_left"},
    {HEX_SIDE::TOP_LEFT, "top_left"}
};


// Maps a hex side to its direct opposite
const dictionary OPPOSITE_HEX_SIDE = {
    {HEX_SIDE::TOP, HEX_SIDE::BOTTOM},
    {HEX_SIDE::TOP_RIGHT, HEX_SIDE::BOTTOM_LEFT},
    {HEX_SIDE::BOTTOM_RIGHT, HEX_SIDE::TOP_LEFT},
    {HEX_SIDE::BOTTOM, HEX_SIDE::TOP},
    {HEX_SIDE::BOTTOM_LEFT, HEX_SIDE::TOP_RIGHT},
    {HEX_SIDE::TOP_LEFT, HEX_SIDE::BOTTOM_RIGHT}
};


// Each hex has six neighbours, one for each side. This table maps the hex
// side to the coordinate offset of the neighbour adjacent to that side.
const dictionary HEX_NEIGHBOUR_OFFSET = {
    {HEX_SIDE::TOP, Int2 = { 0,  1}},
    {HEX_SIDE::TOP_RIGHT, Int2 = { 1,  0}},
    {HEX_SIDE::BOTTOM_RIGHT, Int2 = { 1, -1}},
    {HEX_SIDE::BOTTOM, Int2 = { 0, -1}},
    {HEX_SIDE::BOTTOM_LEFT, Int2 = {-1,  0}},
    {HEX_SIDE::TOP_LEFT, Int2 = {-1,  1}}
};


// // Returns an iterator that iterates over all six neighbours of a hex
// //
// // @param q, r
// //  Coordinates of the center hex
// //
// // Example:
// //
// //  for(side, q, r in iterateNeighbours(0, 0)){
// //      auto sideName = HEX_SIDE::NAME[side]
// //      debug("Neighbour to " .. sideName .. ": " .. tostring(q) .. ", " .. tostring(r))
// //  }
// //
// //  This would print the coordinates of each hex around the (0, 0) hexagon.
// // Not used?
// void iterateNeighbours(int q, int r){

//     void nextNeighbour(int dummy, int i){
//         i = i+1;
//         auto offset = HEX_NEIGHBOUR_OFFSET[i];
//         if(offset == null){
//             return null;
//         }
//         return i, q + offset[1], r + offset[2];
//     }
//     return nextNeighbour(0, 0);
// }

// Call Hex::axialToCartesian directly
// and also all the others that were here before
// This might need tweaking in places that call this:
// void cubeToAxial(x, y, z){
//     result = Hex.cubeToAxial(x, y, z);
//     return result.x, result.y;
// }
