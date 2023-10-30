// ------------------------------------ //
#include "StepListener.hpp"

#include "PhysicalWorld.hpp"

// ------------------------------------ //
namespace Thrive::Physics
{

StepListener::StepListener(PhysicalWorld& world) : notifyWorld(world)
{
}

void StepListener::OnStep(float inDeltaTime, JPH::PhysicsSystem& inPhysicsSystem)
{
    // We assume here that the physics system is our target world's system
    UNUSED(inPhysicsSystem);

    notifyWorld.PerformPhysicsStepOperations(inDeltaTime);
}

} // namespace Thrive::Physics
