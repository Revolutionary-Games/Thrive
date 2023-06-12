#pragma once

#include "Jolt/Physics/Collision/ContactListener.h"

#include "core/Mutex.hpp"

#ifdef JPH_DEBUG_RENDERER
namespace JPH
{
class DebugRenderer;
} // namespace JPH
#endif

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

#ifdef JPH_DEBUG_RENDERER
    void DrawActiveContacts(JPH::DebugRenderer& debugRenderer);

    inline void SetDebugDraw(JPH::DebugRenderer* debugRenderer)
    {
        debugDrawer = debugRenderer;
    }

    /// \brief When using physics debug draw rate limiting we only want to draw new things to avoid missing drawing
    /// anything
    inline void SetDrawOnlyNewContacts(bool onlyNew)
    {
        drawOnlyNew = onlyNew;
    }
#endif

private:
    Mutex currentCollisionsMutex;

    // TODO: JPH seems to use a custom allocator here so we might need to do so as well (for performance)
    std::unordered_map<JPH::SubShapeIDPair, CollisionPair> currentCollisions;

    JPH::ContactListener* chainedListener = nullptr;

#ifdef JPH_DEBUG_RENDERER
    JPH::DebugRenderer* debugDrawer = nullptr;
    bool drawOnlyNew = false;
#endif
};

} // namespace Thrive::Physics
