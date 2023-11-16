#pragma once

#include "Jolt/Math/Quat.h"

#include "Include.h"

namespace Thrive
{
/// \brief Math helper operations
class Math
{
private:
    Math() = delete;

public:
    static JPH::Quat ShortestRotation(JPH::Quat a, JPH::Quat b)
    {
        if (a.Dot(b) < 0)
        {
            return a * (b * -1).Inversed();
        }
        else
        {
            return a * b.Inversed();
        }
    }
};
} // namespace Thrive
