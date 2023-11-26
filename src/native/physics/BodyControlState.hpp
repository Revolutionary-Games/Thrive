#pragma once

namespace Thrive::Physics
{

/// \brief Variables needed for tracking body control over multiple physics updates to make sure the control is stable
class BodyControlState
{
public:
    BodyControlState() = default;

    JPH::Quat targetRotation = {};

    JPH::Vec3 movement = {};

    /// \brief Rotation speed divisor. Should at smallest be 1 for full speed, any higher value has chance of causing
    /// unwanted oscillation. Higher values slow down rotation.
    float rotationRate = 1;
};

} // namespace Thrive::Physics
