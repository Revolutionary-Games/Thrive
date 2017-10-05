#include "general/hex.h"
#include "scripting/luajit.h"

#include <assert.h>
#include <cmath>

using namespace thrive;

double Hex::hexSize = DEFAULT_HEX_SIZE;

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

void Hex::setHexSize(double newSize) {
    Hex::hexSize = newSize;
}

double Hex::getHexSize() {
    return Hex::hexSize;
}

Ogre::Vector3 Hex::axialToCartesian(double q, double r) {
    double x = q * Hex::hexSize * 3.0/2.0;
    double y = Hex::hexSize * std::sqrt(3) * (r + q/2.0);
    return Ogre::Vector3(x, y, 0);
}

Ogre::Vector3 Hex::cartesianToAxial(double x, double y) {
    double q = x * (2.0/3.0) / Hex::hexSize;
    double r = y / (Hex::hexSize * std::sqrt(3)) - q/2.0;
    return Ogre::Vector3(q, r, 0);
}

Ogre::Vector3 Hex::axialToCube(double q, double r) {
    return Ogre::Vector3(q, -(q + r), r);
}

Ogre::Vector3 Hex::cubeToAxial(double x, double y, double) {
    return Ogre::Vector3(x, y, 0);
}

Ogre::Vector3 Hex::cubeHexRound(double x, double y, double z) {
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

    return Ogre::Vector3(rx, ry, rz);
}

long Hex::encodeAxial(double q, double r) {
    if(std::abs(q) >= ENCODE_AXIAL_OFFSET || std::abs(r) >= ENCODE_AXIAL_OFFSET)
           assert(false && "Coordinates out of range, q and r need to be smaller than ENCODE_AXIAL_OFFSET");

    return (q + ENCODE_AXIAL_OFFSET) * ENCODE_AXIAL_SHIFT + r + ENCODE_AXIAL_OFFSET;
}

Ogre::Vector3 Hex::decodeAxial(long s) {
    int r = (s % ENCODE_AXIAL_SHIFT) - ENCODE_AXIAL_OFFSET;
    int q = (s - r - ENCODE_AXIAL_OFFSET) / ENCODE_AXIAL_SHIFT - ENCODE_AXIAL_OFFSET;
    return Ogre::Vector3(q, r, 0);
}

Ogre::Vector3 Hex::rotateAxial(double q, double r) {
    return Ogre::Vector3(-r, q + r, 0);
}

Ogre::Vector3 Hex::rotateAxialNTimes(double q0, double r0, unsigned n) {
    Ogre::Vector3 result(q0, r0, 0);

    for(unsigned i = 0; i < n % 6; i++)
        result = rotateAxial(result);

    return result;
}

Ogre::Vector3 Hex::flipHorizontally(double q, double r) {
    return Ogre::Vector3(-q, q + r, 0);
}

// The Vector3 versions of the functions just unpack
// the vector and call the normal functions.
Ogre::Vector3 Hex::axialToCartesian(Ogre::Vector3 hex) {
    return Hex::axialToCartesian(hex.x, hex.y);
}

Ogre::Vector3 Hex::cartesianToAxial(Ogre::Vector3 hex) {
    return Hex::cartesianToAxial(hex.x, hex.y);
}

Ogre::Vector3 Hex::axialToCube(Ogre::Vector3 hex) {
    return Hex::axialToCube(hex.x, hex.y);
}

Ogre::Vector3 Hex::cubeToAxial(Ogre::Vector3 hex) {
    return Hex::cubeToAxial(hex.x, hex.y, hex.z);
}

Ogre::Vector3 Hex::cubeHexRound(Ogre::Vector3 hex) {
    return Hex::cubeHexRound(hex.x, hex.y, hex.z);
}

long Hex::encodeAxial(Ogre::Vector3 hex) {
    return Hex::encodeAxial(hex.x, hex.y);
}

Ogre::Vector3 Hex::rotateAxial(Ogre::Vector3 hex) {
    return Hex::rotateAxial(hex.x, hex.y);
}

Ogre::Vector3 Hex::rotateAxialNTimes(Ogre::Vector3 hex, unsigned n) {
    return Hex::rotateAxialNTimes(hex.x, hex.y, n);
}

Ogre::Vector3 Hex::flipHorizontally(Ogre::Vector3 hex) {
    return Hex::flipHorizontally(hex.x, hex.y);
}
