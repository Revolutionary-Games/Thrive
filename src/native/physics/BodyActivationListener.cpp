// ------------------------------------ //
#include "BodyActivationListener.hpp"

#include "PhysicsBody.hpp"

// ------------------------------------ //
namespace Thrive::Physics
{

void BodyActivationListener::OnBodyActivated(const JPH::BodyID& bodyID, uint64_t bodyUserData)
{
    UNUSED(bodyID);

    auto bodyWrapper = PhysicsBody::FromJoltBody(bodyUserData);

    if (bodyWrapper != nullptr)
        bodyWrapper->NotifyActiveStatus(true);
}

void BodyActivationListener::OnBodyDeactivated(const JPH::BodyID& bodyID, uint64_t bodyUserData)
{
    UNUSED(bodyID);

    auto bodyWrapper = PhysicsBody::FromJoltBody(bodyUserData);

    if (bodyWrapper != nullptr)
        bodyWrapper->NotifyActiveStatus(false);
}

} // namespace Thrive::Physics
