#pragma once

#include "engine/typedefs.h"
#include "general/json_registry.h"

#include <Common/ReferenceCounted.h>

#include <map>

namespace thrive {

class SimulationParameters;

//! \brief JSON loaded process info
class BioProcess : public RegistryType {
public:
    // The amount of compounds required/obtained.
    std::map<CompoundId, double> inputs;
    std::map<CompoundId, double> outputs;

    BioProcess();

    BioProcess(Json::Value value);
};

//! \brief A tweaked process rate, contained in a organelle
class TweakedProcess : public Leviathan::ReferenceCounted {
    // These are protected: for only constructing properly reference
    // counted instances through MakeShared
    friend ReferenceCounted;
    //! \param processName name of the process as it is in PROCESS_TABLE
    TweakedProcess(const std::string& processName, float tweakRate);

public:
    float
        getTweakRate() const
    {
        return m_tweakRate;
    }

    //! The setup needs the process capacity for some reason
    //! This isn't even configrable anywhere
    float
        getCapacity() const
    {
        return 1.0f;
        // return capacity;
    }

    REFERENCE_COUNTED_PTR_TYPE(TweakedProcess);

    const BioProcess process;

private:
    float m_tweakRate = 1.0;
};


} // namespace thrive
