#pragma once

#include <Entities/PerWorldData.h>

#include <functional>
#include <map>
#include <memory>

namespace thrive {

//! \brief Base world effect class
class WorldEffect {
public:
    virtual ~WorldEffect() = default;

    //! \brief Called when added to a world. The best time to do dynamic casts
    //! etc.
    virtual void
        onRegisterToWorld(GameWorld* world);

    virtual void
        onTimePassed(double elapsed, long double totalTimePassed) = 0;

protected:
    GameWorld* m_world = nullptr;
};

//! \brief World effect with a lambda
class WorldEffectLambda : public WorldEffect {
public:
    WorldEffectLambda(
        std::function<void(double elapsed, long double totalTimePassed)>
            onPassed);

    void
        onTimePassed(double elapsed, long double totalTimePassed) override;

protected:
    std::function<void(double elapsed, long double totalTimePassed)> m_onPassed;
};

//! \brief Handles running timed operations as time in the GameWorld passes
//!
//! Used for example to change patch conditions over time
class TimedWorldOperations : public Leviathan::PerWorldData {
public:
    TimedWorldOperations(GameWorld& world);

    //! \brief Called when time passes
    //! \note This is different than realtime gameplay time, these are mostly
    //! the time jumps that happen in the editor
    void
        onTimePassed(double timePassed);

    //! \brief Registers an effect to run when time passes
    void
        registerEffect(const std::string& name,
            std::unique_ptr<WorldEffect>&& effect);

    //! \brief Clears the elapsed time but doesn't clear effects
    void
        OnClear() override;

private:
    //! This probably needs to be changed to a huge precision number depending
    //! on what timespans we'll end up using
    long double m_totalPassedTime = 0;

    std::map<std::string, std::unique_ptr<WorldEffect>> m_effects;
};
} // namespace thrive
