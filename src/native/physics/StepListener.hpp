#pragma once

#include "Jolt/Physics/PhysicsStepListener.h"

namespace Thrive::Physics
{

/// \brief Listener for physics steps to apply per-step physics state
class StepListener : public JPH::PhysicsStepListener
{
public:
    explicit StepListener(PhysicalWorld& world);

    /// \summary Called each physics step, but only if there is at least one non-sleeping physics body
    void OnStep(float inDeltaTime, JPH::PhysicsSystem& inPhysicsSystem) override;

private:
    PhysicalWorld& notifyWorld;
};

} // namespace Thrive::Physics
