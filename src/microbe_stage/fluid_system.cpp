#include "fluid_system.h"

using namespace thrive;

const Float2 FluidSystem::scale(0.1f, 0.1f);
const float FluidSystem::timeScale = 0.001;

FluidEffectComponent::FluidEffectComponent() : Leviathan::Component(TYPE) {}

FluidSystem::FluidSystem() : noiseX(69), noiseY(13) {}

void
    FluidSystem::Run(GameWorld& world)
{
    millisecondsPassed += Leviathan::TICKSPEED * timeScale; // TODO: get this thing plugged to FPS.

    auto& index = CachedComponents.GetIndex();
    for(auto iter = index.begin(); iter != index.end(); ++iter) {
        Leviathan::Physics& rigidBody = std::get<1>(*iter->second);
    }
}

Float2
    FluidSystem::getVelocityAt(Float2 position)
{
    Float2 sample = sampleNoise(position, millisecondsPassed) * 2 - Float2(1, 1);
           //sampleNoise(position + Float2(1.0f, 0.0f), millisecondsPassed);
    return sample;
}

Float2
    FluidSystem::sampleNoise(Float2 pos, float time)
{
    const Float2 scaledPos = pos * scale;
    return Float2(noiseX.noise(scaledPos.X, scaledPos.Y, time),
        noiseY.noise(scaledPos.X, scaledPos.Y, time));
}
