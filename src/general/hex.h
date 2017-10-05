#pragma once

#include <OgreVector3.h>

/*
Defines some utility functions and tables related to hex grids.

For more information on hex grids, see www.redblobgames.com/grids/hexagons.

We use flat-topped hexagons with axial coordinates.
Please note the coordinate system we use is
horizontally symmetric to the one shown in the page.
*/

// The default size of the hexagons, used in calculations.
#define DEFAULT_HEX_SIZE 0.75

// Maximum hex coordinate value that can be encoded with encodeAxial()
#define ENCODE_AXIAL_OFFSET 256

// Multiplier for the q coordinate used in encodeAxial()
#define ENCODE_AXIAL_SHIFT (ENCODE_AXIAL_OFFSET * 10)

namespace sol {
class state;
}

namespace thrive {
class Hex {
public:
    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - MicrobeCameraSystem()
    *
    * @return
    */
    static void luaBindings(sol::state &lua);

    /**
    * @brief Sets the hex size.
    */
    static void setHexSize(double newSize);

    /**
    * @brief Gets the hex size.
    */
    static double getHexSize();

    /**
    * @brief Converts axial hex coordinates to cartesian coordinates.
    *
    * The result is the position of the hex at q, r.
    *
    * @param q, r
    *  Hex coordinates.
    *
    * @return x, y
    *  Cartesian coordinates of the hex's center.
    */
    static Ogre::Vector3 axialToCartesian(double q, double r);
    static Ogre::Vector3 axialToCartesian(Ogre::Vector3 hex);

    /**
    * @brief Converts cartesian coordinates to axial hex coordinates.
    *
    * The result is the hex coordinates of the position x, y.
    *
    * @param x, y
    *  Cartesian coordinates of the hex's center.
    *
    * @returns q, r
    *  Hex position.
    */
    static Ogre::Vector3 cartesianToAxial(double x, double y);
    static Ogre::Vector3 cartesianToAxial(Ogre::Vector3 hex);

    /**
    * @brief Converts axial hex coordinates to coordinates in the cube based hex model
    *
    * The result is the cube x,y,z coordinates of the hex q,r
    *
    * @param q,r
    *  axial hex coordinates
    *
    * @returns x, y, z
    *  cube coordinates
    */
    static Ogre::Vector3 axialToCube(double q, double r);
    static Ogre::Vector3 axialToCube(Ogre::Vector3 hex);

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
    static Ogre::Vector3 cubeToAxial(double x, double y, double z);
    static Ogre::Vector3 cubeToAxial(Ogre::Vector3 hex);

    /**
    * @brief Correctly rounds fractional hex cube coordinates to the correct integer coordinates
    *
    * @param x, y, z
    *  fractional cube hex coordinates
    *
    * @returns rx, ry, rz
    *  correctly rounded hex cube coordinates
    */
    static Ogre::Vector3 cubeHexRound(double x, double y, double z);
    static Ogre::Vector3 cubeHexRound(Ogre::Vector3 hex);

    /**
    * @brief Encodes axial coordinates to a single number.
    *
    * Useful for using hex coordinates as keys in a table.
    *
    * @param q,r
    *  Axial coordinates. Each must be smaller than ENCODE_AXIAL_OFFSET.
    *
    * @returns s
    *  A single number encoding q and r. Use decodeAxial() to retrieve q and r from it.
    */
    static long encodeAxial(double q, double r);
    static long encodeAxial(Ogre::Vector3 hex);

    /**
    * @brief Reverses encodeAxial().
    *
    * @param s
    *  Encoded hex coordinates, generated with encodeAxial()
    *
    * @returns q, r
    *  The hex coordinates encoded in s
    */
    static Ogre::Vector3 decodeAxial(long s);

    /**
    * @brief Rotates a hex by 60 degrees about the origin clock-wise.
    */
    static Ogre::Vector3 rotateAxial(double q, double r);
    static Ogre::Vector3 rotateAxial(Ogre::Vector3 hex);

    /**
    * @brief Rotates a hex by (60 * n) degrees about the origin clock-wise.
    */
    static Ogre::Vector3 rotateAxialNTimes(double q0, double r0, unsigned n);
    static Ogre::Vector3 rotateAxialNTimes(Ogre::Vector3 hex, unsigned n);

    /**
    * @brief Symmetrizes a hex horizontally about the (0,x) axis.
    */
    static Ogre::Vector3 flipHorizontally(double q, double r);
    static Ogre::Vector3 flipHorizontally(Ogre::Vector3 hex);

private:
    static double hexSize;
};

}
