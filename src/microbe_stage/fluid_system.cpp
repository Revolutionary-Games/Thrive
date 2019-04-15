#include "fluid_system.h"

using namespace thrive;

const Float2 FluidSystem::scale(0.05f, 0.05f);
const float FluidSystem::timeScale = 0.001f;
const float FluidSystem::maxForceApplied = 10.0f;

FluidEffectComponent::FluidEffectComponent() : Leviathan::Component(TYPE) {}

FluidSystem::FluidSystem() : noiseX(69), noiseY(13) {}

void
    FluidSystem::Run(GameWorld& world)
{
    millisecondsPassed += Leviathan::TICKSPEED *
                          timeScale; // TODO: get this thing plugged to FPS.

    for(auto& [id, components] : CachedComponents.GetIndex()) {
        Leviathan::PhysicsBody* rigidBody = std::get<1>(*components).GetBody();

        if(!rigidBody) // Missing body for some reason.
            continue;

        Float3 pos = rigidBody->GetPosition();
        Float2 vel = getVelocityAt(Float2(pos.X, pos.Z)) * maxForceApplied;
        rigidBody->GiveImpulse(Float3(vel.X, 0.0f, vel.Y));
    }
}

// TODO: refactor this nonsense.
// TODO: also figure out if there's a way to do this that doesn't generate only
// horiontal or vertical currents
Float2
    FluidSystem::getVelocityAt(Float2 position)
{
    constexpr float sampleProp = 0.15f;
    constexpr float minCurrentIntensity = 0.4f;

    Float2 sample =
        sampleNoise(position, millisecondsPassed) * 2 - Float2(1, 1);
    const Float2 scaledPos = position * scale / 2;
    Float2 sample2 = Float2(noiseX.noise(scaledPos.X / 10, scaledPos.Y,
                                millisecondsPassed / 500),
                         noiseY.noise(scaledPos.X, scaledPos.Y / 10,
                             millisecondsPassed / 500)) *
                         2 -
                     Float2(1, 1);

    Float2 sample2x = Float2(0.0f);
    if(std::abs(sample2.X) > minCurrentIntensity)
        sample2x += Float2(sample2.X, 0.0f);
    if(std::abs(sample2.Y) > minCurrentIntensity)
        sample2x += Float2(0.0f, sample2.Y);

    // Normalize the sample?
    return sample * sampleProp + sample2x * (1.0f - sampleProp);
}

Float2
    FluidSystem::sampleNoise(Float2 pos, float time)
{
    const Float2 scaledPos = pos * scale;
    return Float2(noiseX.noise(scaledPos.X, scaledPos.Y, time),
        noiseY.noise(scaledPos.X, scaledPos.Y, time));
}
