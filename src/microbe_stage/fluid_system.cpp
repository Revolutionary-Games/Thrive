#include "fluid_system.h"

using namespace thrive;

const Float2 FluidSystem::scale(0.05f, 0.05f);

FluidEffectComponent::FluidEffectComponent() : Leviathan::Component(TYPE) {}

FluidSystem::FluidSystem() :
    noiseDisturbancesX(69), noiseDisturbancesY(13), noiseCurrentsX(420),
    noiseCurrentsY(1337)
{}

void
    FluidSystem::Run(GameWorld& world, float elapsed)
{
    millisecondsPassed += elapsed / 1000.f;

    for(auto& [id, components] : CachedComponents.GetIndex()) {
        Leviathan::PhysicsBody* rigidBody = std::get<1>(*components).GetBody();

        if(!rigidBody) // Missing body for some reason.
            continue;

        Float3 pos = rigidBody->GetPosition();
        Float2 vel = getVelocityAt(Float2(pos.X, pos.Z)) * maxForceApplied;

        rigidBody->GiveImpulse(Float3(vel.X, 0.0f, vel.Y));
    }
}

// TODO: also figure out if there's a way to do this that doesn't generate only
// horiontal or vertical currents
Float2
    FluidSystem::getVelocityAt(Float2 position)
{
    const Float2 scaledPosition = position * positionScaling;

    const float disturbances_x =
        noiseDisturbancesX.noise(scaledPosition.X, scaledPosition.Y,
            millisecondsPassed * disturbanceTimescale) *
            2.0f -
        1.0f;
    ;
    const float disturbances_y =
        noiseDisturbancesY.noise(scaledPosition.X, scaledPosition.Y,
            millisecondsPassed * disturbanceTimescale) *
            2.0f -
        1.0f;

    const float currents_x =
        noiseCurrentsX.noise(scaledPosition.X * currentsStretchingMultiplier,
            scaledPosition.Y, millisecondsPassed * currentsTimescale) *
            2.0f -
        1.0f;
    const float currents_y =
        noiseCurrentsY.noise(scaledPosition.X,
            scaledPosition.Y * currentsStretchingMultiplier,
            millisecondsPassed * currentsTimescale) *
            2.0f -
        1.0f;

    const Float2 disturbancesVelocity(disturbances_x, disturbances_y);
    const Float2 currentsVelocity(
        std::abs(currents_x) > minCurrentIntensity ? currents_x : 0.0f,
        std::abs(currents_y) > minCurrentIntensity ? currents_y : 0.0f);

    return (disturbancesVelocity * disturbanceToCurrentsRatio +
            currentsVelocity * (1.0f - disturbanceToCurrentsRatio));
}
