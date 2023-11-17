#pragma once

#include "Jolt/Physics/Collision/ContactListener.h"

#include "core/Mutex.hpp"

namespace JPH
{
#ifdef JPH_DEBUG_RENDERER
class DebugRenderer;
#endif

class Shape;
} // namespace JPH

namespace Thrive::Physics
{
uint32_t ResolveTopLevelSubShapeId(const JPH::Body* body, JPH::SubShapeID subShapeId);
uint32_t ResolveSubShapeId(const JPH::Shape* shape, JPH::SubShapeID subShapeId, JPH::SubShapeID& remainder);

/// \brief Contact listener implementation
class ContactListener : public JPH::ContactListener
{
    using CollisionPair = std::pair<JPH::RVec3, JPH::ContactPoints>;

public:
    ContactListener();

    JPH::ValidateResult OnContactValidate(const JPH::Body& body1, const JPH::Body& body2, JPH::RVec3Arg baseOffset,
        const JPH::CollideShapeResult& collisionResult) override;

    void OnContactAdded(const JPH::Body& body1, const JPH::Body& body2, const JPH::ContactManifold& manifold,
        JPH::ContactSettings& settings) override;

    void OnContactPersisted(const JPH::Body& body1, const JPH::Body& body2, const JPH::ContactManifold& manifold,
        JPH::ContactSettings& settings) override;

    void OnContactRemoved(const JPH::SubShapeIDPair& subShapePair) override;

    inline void ReportStepNumber(uint32_t step) noexcept
    {
        physicsStep = step;
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

    // This is currently only necessary when debug drawing
#ifdef JPH_DEBUG_RENDERER
    // TODO: JPH seems to use a custom allocator here so we might need to do so as well (for performance)
    std::unordered_map<JPH::SubShapeIDPair, CollisionPair> currentCollisions;
#endif

    uint32_t physicsStep = std::numeric_limits<uint32_t>::max();

#ifdef JPH_DEBUG_RENDERER
    JPH::DebugRenderer* debugDrawer = nullptr;
    bool drawOnlyNew = false;
#endif
};

} // namespace Thrive::Physics
