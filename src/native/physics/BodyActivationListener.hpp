#pragma once

#include "Jolt/Physics/Body/BodyActivationListener.h"

namespace Thrive::Physics
{
/// \brief Jolt physics body activation state listener (bodies that don't move for a while go to sleep)
class BodyActivationListener : public JPH::BodyActivationListener
{
public:
    void OnBodyActivated(const JPH::BodyID& bodyID, uint64_t bodyUserData) override;

    void OnBodyDeactivated(const JPH::BodyID& bodyID, uint64_t bodyUserData) override;
};

} // namespace Thrive::Physics
