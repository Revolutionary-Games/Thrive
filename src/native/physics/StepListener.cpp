// ------------------------------------ //
#include "StepListener.hpp"

#include "PhysicalWorld.hpp"

// ------------------------------------ //
namespace Thrive::Physics
{

StepListener::StepListener(PhysicalWorld& world) : notifyWorld(world)
{
}

void StepListener::OnStep(const JPH::PhysicsStepListenerContext &inContext)
{
    // We assume here that the physics system is our target world's system
    notifyWorld.PerformPhysicsStepOperations(inContext.mDeltaTime);
}

} // namespace Thrive::Physics
