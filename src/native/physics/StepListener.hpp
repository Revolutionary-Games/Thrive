#pragma once

#include "Jolt/Physics/PhysicsStepListener.h"

namespace Thrive::Physics
{

/// \brief Listener for physics steps to apply per-step physics state
class StepListener : public JPH::PhysicsStepListener
{
public:
    explicit StepListener(PhysicalWorld& world);

    void OnStep(float inDeltaTime, JPH::PhysicsSystem& inPhysicsSystem) override;

private:
    PhysicalWorld& notifyWorld;
};

} // namespace Thrive::Physics
