#include "general/hex.h"

#include <cmath>

using namespace thrive;

double Hex::hexSize = DEFAULT_HEX_SIZE;

// void Hex::setHexSize(double newSize) {
//     Hex::hexSize = newSize;
// }

double
    Hex::getHexSize()
{
    return Hex::hexSize;
}

Float3
    Hex::axialToCartesian(double q, double r)
{
    double x = q * Hex::hexSize * 3.0 / 2.0;
    double z = Hex::hexSize * std::sqrt(3) * (r + q / 2.0);
    return Float3(x, 0, z);
}

Int2
    Hex::cartesianToAxial(double x, double y)
{
	// Getting the cube coordinates.
    double cx = x * (2.0 / 3.0) / Hex::hexSize;
    double cy = y / (Hex::hexSize * std::sqrt(3)) - cx / 2.0;
    double cz = -(cx + cy);

    // Rounding the result.
    double rx = round(cx);
    double ry = round(cy);
    double rz = round(cz);

    double xDiff = std::abs(rx - cx);
    double yDiff = std::abs(ry - cy);
    double zDiff = std::abs(rz - cz);

    if(xDiff > yDiff && xDiff > zDiff)
        rx = -(ry + rz);

    else if(yDiff > zDiff)
        ry = -(rx + rz);

	// Returning the axial coordinates.
    return cubeToAxial(rx, ry, rz);
}

Int3
    Hex::axialToCube(double q, double r)
{
    return Int3(q, r, -(q + r));
}

Int2
    Hex::cubeToAxial(double x, double y, double z)
{
    (void)z;
    return Int2(x, y);
}

Int3
    Hex::cubeHexRound(double x, double y, double z)
{
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

int64_t
    Hex::encodeAxial(double q, double r)
{
    if(std::abs(q) >= ENCODE_AXIAL_OFFSET || std::abs(r) >= ENCODE_AXIAL_OFFSET)
        LEVIATHAN_ASSERT(false, "Coordinates out of range, q and r need to be "
                                "smaller than ENCODE_AXIAL_OFFSET");

    return (q + ENCODE_AXIAL_OFFSET) * ENCODE_AXIAL_SHIFT + r +
           ENCODE_AXIAL_OFFSET;
}

Int2
    Hex::decodeAxial(int64_t s)
{
    int r = (s % ENCODE_AXIAL_SHIFT) - ENCODE_AXIAL_OFFSET;
    int q = (s - r - ENCODE_AXIAL_OFFSET) / ENCODE_AXIAL_SHIFT -
            ENCODE_AXIAL_OFFSET;
    return Int2(q, r);
}

Int2
    Hex::rotateAxial(double q, double r)
{
    return Int2(-r, q + r);
}

Int2
    Hex::rotateAxialNTimes(double q0, double r0, uint32_t n)
{
    Int2 result(q0, r0);

    for(uint32_t i = 0; i < n % 6; i++)
        result = rotateAxial(result);

    return result;
}

Int2
    Hex::flipHorizontally(double q, double r)
{
    return Int2(-q, q + r);
}

// The Vector3 versions of the functions just unpack
// the vector and call the normal functions.
Float3
    Hex::axialToCartesian(const Int2& hex)
{
    return Hex::axialToCartesian(hex.X, hex.Y);
}

Int2
    Hex::cartesianToAxial(const Float3& hex)
{
    return Hex::cartesianToAxial(hex.X, hex.Z);
}

Int3
    Hex::axialToCube(const Int2& hex)
{
    return Hex::axialToCube(hex.X, hex.Y);
}

Int2
    Hex::cubeToAxial(const Int3& hex)
{
    return Hex::cubeToAxial(hex.X, hex.Y, hex.Z);
}

Int3
    Hex::cubeHexRound(const Float3& hex)
{
    return Hex::cubeHexRound(hex.X, hex.Y, hex.Z);
}

int64_t
    Hex::encodeAxial(const Int2& hex)
{
    return Hex::encodeAxial(hex.X, hex.Y);
}

Int2
    Hex::rotateAxial(const Int2& hex)
{
    return Hex::rotateAxial(hex.X, hex.Y);
}

Int2
    Hex::rotateAxialNTimes(const Int2& hex, uint32_t n)
{
    return Hex::rotateAxialNTimes(hex.X, hex.Y, n);
}

Int2
    Hex::flipHorizontally(const Int2& hex)
{
    return Hex::flipHorizontally(hex.X, hex.Y);
}
