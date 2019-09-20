// ------------------------------------ //
#include "timed_world_operations.h"

using namespace thrive;
// ------------------------------------ //
// WorldEffect
void
    WorldEffect::onRegisterToWorld(GameWorld* world)
{
    m_world = world;
}
// ------------------------------------ //
// WorldEffectLambda
WorldEffectLambda::WorldEffectLambda(
    std::function<void(double elapsed, long double totalTimePassed)> onPassed) :
    m_onPassed(onPassed)
{}
// ------------------------------------ //
void
    WorldEffectLambda::onTimePassed(double elapsed, long double totalTimePassed)
{
    m_onPassed(elapsed, totalTimePassed);
}
// ------------------------------------ //
// TimedWorldOperations
TimedWorldOperations::TimedWorldOperations(GameWorld& world) :
    Leviathan::PerWorldData(world)
{}
// ------------------------------------ //
void
    TimedWorldOperations::onTimePassed(double timePassed)
{
    if(m_effects.empty())
        return;

    m_totalPassedTime += timePassed;

    LOG_INFO("TimedWorldOperations: running effects. elapsed: " +
             std::to_string(timePassed) +
             " total passed: " + std::to_string(m_totalPassedTime));

    for(auto iter = m_effects.begin(); iter != m_effects.end(); ++iter) {
        iter->second->onTimePassed(timePassed, m_totalPassedTime);
    }
}
// ------------------------------------ //
void
    TimedWorldOperations::registerEffect(const std::string& name,
        std::unique_ptr<WorldEffect>&& effect)
{
    if(effect)
        effect->onRegisterToWorld(&InWorld);
    m_effects[name] = std::move(effect);
}
// ------------------------------------ //
void
    TimedWorldOperations::OnClear()
{
    m_totalPassedTime = 0;
}
