#pragma once

#include <Common/Types.h>

/*
Defines some utility functions and tables related to hex grids.

For more information on hex grids, see www.redblobgames.com/grids/hexagons.

We use flat-topped hexagons with axial coordinates.
Please note the coordinate system we use is
horizontally symmetric to the one shown in the page.
*/

namespace thrive {

// The default size of the hexagons, used in calculations.
// The script will also retrieve this on startup
constexpr float DEFAULT_HEX_SIZE = 0.75f;

// Maximum hex coordinate value that can be encoded with encodeAxial()
constexpr int ENCODE_AXIAL_OFFSET = 256;

// Multiplier for the q coordinate used in encodeAxial()
constexpr int ENCODE_AXIAL_SHIFT = (ENCODE_AXIAL_OFFSET * 10);

//! \brief Contains utility functions for axial coordinates
class Hex {
public:
    // This will cause a lot of issues if this is called
    // /**
    // * @brief Sets the hex size.
    // */
    // static void setHexSize(double newSize);

    /**
     * @brief Gets the hex size.
     */
    static double
        getHexSize();

    /**
     * @brief Converts axial hex coordinates to cartesian coordinates.
     *
     * The result is the position of the hex at q, r.
     *
     * @param q, r
     *  Hex coordinates.
     *
     * @return x, z
     *  Cartesian coordinates of the hex's center.
     */
    static Float3
        axialToCartesian(double q, double r);
    static Float3
        axialToCartesian(const Int2& hex);

    /**
     * @brief Converts cartesian coordinates to axial hex coordinates.
     *
     * The result is the hex coordinates of the position x, z.
     *
     * @param x, z
     *  Cartesian coordinates of the hex's center.
     *
     * @returns q, r
     *  Hex position.
     */
    static Int2
        cartesianToAxial(double x, double z);
    static Int2
        cartesianToAxial(const Float3& coordinates);

    /**
     * @brief Converts axial hex coordinates to coordinates in the cube based
     * hex model
     *
     * The result is the cube x,y,z coordinates of the hex q,r
     *
     * @param q,r
     *  axial hex coordinates
     *
     * @returns x, y, z
     *  cube coordinates
     */
    static Int3
        axialToCube(double q, double r);
    static Int3
        axialToCube(const Int2& hex);

    /**
     * @brief Converts cube based hex coordinates to axial hex coordinates
     *
     * The result is the axial hex coordinates of the cube x, y ,z
     *
     * @param x, y, z
     *  cube hex coordinates
     *
     * @returns q, r
     *  hex coordinates
     */
    static Int2
        cubeToAxial(double x, double y, double z);
    static Int2
        cubeToAxial(const Int3& hex);

    /**
     * @brief Correctly rounds fractional hex cube coordinates to the correct
     * integer coordinates
     *
     * @param x, y, z
     *  fractional cube hex coordinates
     *
     * @returns rx, ry, rz
     *  correctly rounded hex cube coordinates
     */
    static Int3
        cubeHexRound(double x, double y, double z);
    static Int3
        cubeHexRound(const Float3& hex);

    /**
     * @brief Encodes axial coordinates to a single number.
     *
     * Useful for using hex coordinates as keys in a table.
     *
     * @param q,r
     *  Axial coordinates. Each must be smaller than ENCODE_AXIAL_OFFSET.
     *
     * @returns s
     *  A single number encoding q and r. Use decodeAxial() to retrieve q and r
     * from it.
     */
    static int64_t
        encodeAxial(double q, double r);
    static int64_t
        encodeAxial(const Int2& hex);

    /**
     * @brief Reverses encodeAxial().
     *
     * @param s
     *  Encoded hex coordinates, generated with encodeAxial()
     *
     * @returns q, r
     *  The hex coordinates encoded in s
     */
    static Int2
        decodeAxial(int64_t s);

    /**
     * @brief Rotates a hex by 60 degrees about the origin clock-wise.
     */
    static Int2
        rotateAxial(double q, double r);
    static Int2
        rotateAxial(const Int2& hex);

    /**
     * @brief Rotates a hex by (60 * n) degrees about the origin clock-wise.
     */
    static Int2
        rotateAxialNTimes(double q0, double r0, uint32_t n);
    static Int2
        rotateAxialNTimes(const Int2& hex, uint32_t n);

    /**
     * @brief Symmetrizes a hex horizontally about the (0,x) axis.
     */
    static Int2
        flipHorizontally(double q, double r);
    static Int2
        flipHorizontally(const Int2& hex);

private:
    static double hexSize;
};

} // namespace thrive
