#include "StepListener.hpp"
#include "PhysicalWorld.hpp"

namespace Thrive::Physics
{

StepListener::StepListener(PhysicalWorld& world) : notifyWorld(world)
{
}

void StepListener::OnStep(const JPH::PhysicsStepListenerContext &inContext)
{
    // Assuming this operation uses the delta time from the context
    notifyWorld.PerformPhysicsStepOperations(inContext.mDeltaTime);
}

} // namespace Thrive::Physics
