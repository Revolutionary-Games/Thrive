#include "general/hex.h"

#include <cmath>

using namespace thrive;

double Hex::hexSize = DEFAULT_HEX_SIZE;

/*
void Hex::luaBindings(
    sol::state &lua
) {
    lua.new_usertype<Hex>("Hex",
        // It's a static class.
        "new", sol::no_constructor,

        "setHexSize", &Hex::setHexSize,
        "getHexSize", &Hex::getHexSize,
        "decodeAxial", &Hex::decodeAxial,

        // Overloaded methods.
        "axialToCartesian", sol::overload(
            static_cast<Ogre::Vector3(*)(double, double)>(&Hex::axialToCartesian),
            static_cast<Ogre::Vector3(*)(Ogre::Vector3)>(&Hex::axialToCartesian)
        ),

        "cartesianToAxial", sol::overload(
            static_cast<Ogre::Vector3(*)(double, double)>(&Hex::cartesianToAxial),
            static_cast<Ogre::Vector3(*)(Ogre::Vector3)>(&Hex::cartesianToAxial)
        ),

        "axialToCube", sol::overload(
            static_cast<Ogre::Vector3(*)(double, double)>(&Hex::axialToCube),
            static_cast<Ogre::Vector3(*)(Ogre::Vector3)>(&Hex::axialToCube)
        ),

        "cubeToAxial", sol::overload(
            static_cast<Ogre::Vector3(*)(double, double, double)>(&Hex::cubeToAxial),
            static_cast<Ogre::Vector3(*)(Ogre::Vector3)>(&Hex::cubeToAxial)
        ),

        "cubeHexRound", sol::overload(
            static_cast<Ogre::Vector3(*)(double, double, double)>(&Hex::cubeHexRound),
            static_cast<Ogre::Vector3(*)(Ogre::Vector3)>(&Hex::cubeHexRound)
        ),

        "encodeAxial", sol::overload(
            static_cast<long(*)(double, double)>(&Hex::encodeAxial),
            static_cast<long(*)(Ogre::Vector3)>(&Hex::encodeAxial)
        ),

        "rotateAxial", sol::overload(
            static_cast<Ogre::Vector3(*)(double, double)>(&Hex::rotateAxial),
            static_cast<Ogre::Vector3(*)(Ogre::Vector3)>(&Hex::rotateAxial)
        ),

        "rotateAxialNTimes", sol::overload(
            static_cast<Ogre::Vector3(*)(double, double, unsigned)>(&Hex::rotateAxialNTimes),
            static_cast<Ogre::Vector3(*)(Ogre::Vector3, unsigned)>(&Hex::rotateAxialNTimes)
        ),

        "flipHorizontally", sol::overload(
            static_cast<Ogre::Vector3(*)(double, double)>(&Hex::flipHorizontally),
            static_cast<Ogre::Vector3(*)(Ogre::Vector3)>(&Hex::flipHorizontally)
        )
    );
}
*/

void Hex::setHexSize(double newSize) {
    Hex::hexSize = newSize;
}

double Hex::getHexSize() {
    return Hex::hexSize;
}

Float3 Hex::axialToCartesian(double q, double r) {
    double x = q * Hex::hexSize * 3.0/2.0;
    double y = Hex::hexSize * std::sqrt(3) * (r + q/2.0);
    return Float3(x, y, 0);
}

Int2 Hex::cartesianToAxial(double x, double z) {
    double q = x * (2.0/3.0) / Hex::hexSize;
    double r = z / (Hex::hexSize * std::sqrt(3)) - q/2.0;
    return Int2(q, r);
}

Int3 Hex::axialToCube(double q, double r) {
    return Int3(q, -(q + r), r);
}

Int2 Hex::cubeToAxial(double x, double y, double z) {
    return Int2(x, y);
}

Int3 Hex::cubeHexRound(double x, double y, double z) {
    double rx = round(x);
    double ry = round(y);
    double rz = round(z);

    double xDiff = std::abs(rx - x);
    double yDiff = std::abs(ry - y);
    double zDiff = std::abs(rz - z);

    if(xDiff > yDiff && xDiff > zDiff)
        rx = -(ry + rz);

    else if(yDiff > zDiff)
        ry = -(rx + rz);

    else
        rz = -(ry + rx);

    return Int3(rx, ry, rz);
}

long Hex::encodeAxial(double q, double r) {
    if(std::abs(q) >= ENCODE_AXIAL_OFFSET || std::abs(r) >= ENCODE_AXIAL_OFFSET)
           LEVIATHAN_ASSERT(false, "Coordinates out of range, q and r need to be smaller than ENCODE_AXIAL_OFFSET");

    return (q + ENCODE_AXIAL_OFFSET) * ENCODE_AXIAL_SHIFT + r + ENCODE_AXIAL_OFFSET;
}

Int2 Hex::decodeAxial(long s) {
    int r = (s % ENCODE_AXIAL_SHIFT) - ENCODE_AXIAL_OFFSET;
    int q = (s - r - ENCODE_AXIAL_OFFSET) / ENCODE_AXIAL_SHIFT - ENCODE_AXIAL_OFFSET;
    return Int2(q, r);
}

Int2 Hex::rotateAxial(double q, double r) {
    return Int2(-r, q + r);
}

Int2 Hex::rotateAxialNTimes(double q0, double r0, unsigned n) {
    Int2 result(q0, r0);

    for(unsigned i = 0; i < n % 6; i++)
        result = rotateAxial(result);

    return result;
}

Int2 Hex::flipHorizontally(double q, double r) {
    return Int2(-q, q + r);
}

// The Vector3 versions of the functions just unpack
// the vector and call the normal functions.
Float3 Hex::axialToCartesian(const Int2 &hex) {
    return Hex::axialToCartesian(hex.X, hex.Y);
}

Int2 Hex::cartesianToAxial(const Float3 &hex) {
    return Hex::cartesianToAxial(hex.X, hex.Z);
}

Int3 Hex::axialToCube(const Int2 &hex) {
    return Hex::axialToCube(hex.X, hex.Y);
}

Int2 Hex::cubeToAxial(const Int3 &hex) {
    return Hex::cubeToAxial(hex.X, hex.Y, hex.Z);
}

Int3 Hex::cubeHexRound(const Float3 &hex) {
    return Hex::cubeHexRound(hex.X, hex.Y, hex.Z);
}

long Hex::encodeAxial(const Int2 &hex) {
    return Hex::encodeAxial(hex.X, hex.Y);
}

Int2 Hex::rotateAxial(const Int2 &hex) {
    return Hex::rotateAxial(hex.X, hex.Y);
}

Int2 Hex::rotateAxialNTimes(const Int2 &hex, unsigned n) {
    return Hex::rotateAxialNTimes(hex.X, hex.Y, n);
}

Int2 Hex::flipHorizontally(const Int2 &hex) {
    return Hex::flipHorizontally(hex.X, hex.Y);
}
