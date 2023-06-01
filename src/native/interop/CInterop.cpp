// ------------------------------------ //
#include "CInterop.h"

#include "physics/PhysicalWorld.hpp"

#pragma clang diagnostic push
#pragma ide diagnostic ignored "cppcoreguidelines-pro-type-reinterpret-cast"

// ------------------------------------ //
int32_t CheckAPIVersion()
{
    return THRIVE_LIBRARY_VERSION;
}

int32_t InitThriveLibrary()
{
    // TODO: any startup actions needed
    return 0;
}

void ShutdownThriveLibrary()
{
    // TODO: shutdown actions
}

// ------------------------------------ //
PhysicalWorld* CreatePhysicalWorld()
{
    return reinterpret_cast<PhysicalWorld*>(new Thrive::Physics::PhysicalWorld());
}

void DestroyPhysicalWorld(PhysicalWorld* physicalWorld)
{
    if (physicalWorld == nullptr)
        return;

    delete reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld);
}

#pragma clang diagnostic pop
