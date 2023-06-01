#pragma once

#include "Jolt/Physics/Collision/ContactListener.h"

#include "core/Mutex.hpp"

namespace Thrive::Physics
{
/// \brief Contact listener implementation
class ContactListener : public JPH::ContactListener
{
    using CollisionPair = std::pair<JPH::RVec3, JPH::ContactPoints>;

public:
    JPH::ValidateResult OnContactValidate(const JPH::Body& body1, const JPH::Body& body2, JPH::RVec3Arg baseOffset,
        const JPH::CollideShapeResult& collisionResult) override;

    void OnContactAdded(const JPH::Body& body1, const JPH::Body& body2, const JPH::ContactManifold& manifold,
        JPH::ContactSettings& settings) override;

    void OnContactPersisted(const JPH::Body& body1, const JPH::Body& body2, const JPH::ContactManifold& manifold,
        JPH::ContactSettings& settings) override;

    void OnContactRemoved(const JPH::SubShapeIDPair& subShapePair) override;

    inline void SetNextListener(JPH::ContactListener* listener)
    {
        chainedListener = listener;
    }

private:
    Mutex currentCollisionsMutex;

    // TODO: JPH seems to use a custom allocator here so we might need to do so as well (for performance)
    std::unordered_map<JPH::SubShapeIDPair, CollisionPair> currentCollisions;

    JPH::ContactListener* chainedListener = nullptr;
};

} // namespace Thrive::Physics
