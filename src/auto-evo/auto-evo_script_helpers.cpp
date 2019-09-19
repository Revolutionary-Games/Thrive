// ------------------------------------ //
#include "auto-evo_script_helpers.h"

#include "thrive_common.h"

#include <Script/ScriptExecutor.h>

using namespace thrive;
using namespace autoevo;
// ------------------------------------ //
uint64_t
    SimulationConfiguration::getExcludedSpeciesCount() const
{
    return excludedSpecies.size();
}

Species const*
    SimulationConfiguration::getExcludedSpecies(uint64_t index) const
{
    if(index >= excludedSpecies.size())
        throw Leviathan::InvalidArgument("index out of range");

    excludedSpecies[index]->AddRef();
    return excludedSpecies[index].get();
}

uint64_t
    SimulationConfiguration::getExtraSpeciesCount() const
{
    return extraSpecies.size();
}

Species const*
    SimulationConfiguration::getExtraSpecies(uint64_t index) const
{
    if(index >= extraSpecies.size())
        throw Leviathan::InvalidArgument("index out of range");

    extraSpecies[index]->AddRef();
    return extraSpecies[index].get();
}
// ------------------------------------ //
Species::pointer
    thrive::autoevo::getMutationForSpecies(const Species::pointer& species)
{
    ScriptRunningSetup setup = ScriptRunningSetup("createMutatedSpecies");

    auto result =
        ThriveCommon::get()->getMicrobeScripts()->ExecuteOnModule<Species*>(
            setup, false, species.get());

    if(result.Result != SCRIPT_RUN_RESULT::Success) {

        LOG_ERROR("Failed to run createMutatedSpecies");
        return nullptr;
    }

    if(!result.Value) {
        LOG_ERROR("createMutatedSpecies returned null");
        return nullptr;
    }

    result.Value->AddRef();
    return Species::WrapPtr(result.Value);
}
// ------------------------------------ //
void
    thrive::autoevo::applySpeciesMutation(const Species::pointer& species,
        const Species::pointer& mutation)
{
    if(!species || !mutation)
        return;

    ScriptRunningSetup setup =
        ScriptRunningSetup("applyMutatedSpeciesProperties");

    auto result =
        ThriveCommon::get()->getMicrobeScripts()->ExecuteOnModule<void>(setup,
            false, species.get(), const_cast<Species const*>(mutation.get()));

    if(result.Result != SCRIPT_RUN_RESULT::Success) {

        LOG_ERROR("Failed to run applyMutatedSpeciesProperties");
    }
}
// ------------------------------------ //
RunResults::pointer
    thrive::autoevo::simulatePatchMapPopulations(const PatchMap::pointer& map,
        const SimulationConfiguration::pointer& config)
{
    ScriptRunningSetup setup =
        ScriptRunningSetup("simulatePatchMapPopulations");

    auto result =
        ThriveCommon::get()->getMicrobeScripts()->ExecuteOnModule<RunResults*>(
            setup, false, const_cast<PatchMap const*>(map.get()),
            const_cast<SimulationConfiguration const*>(config.get()));

    if(result.Result != SCRIPT_RUN_RESULT::Success) {

        LOG_ERROR("Failed to run simulatePatchMapPopulations");
    }

    if(!result.Value) {
        LOG_ERROR("simulatePatchMapPopulations returned null");
        return nullptr;
    }

    result.Value->AddRef();
    return Species::WrapPtr(result.Value);
}
// ------------------------------------ //
void
    thrive::autoevo::simulatePatchPopulations(const Patch::pointer& patch,
        RunResults& results,
        const SimulationConfiguration::pointer& config)
{
    ScriptRunningSetup setup = ScriptRunningSetup("simulatePatchPopulations");

    auto result =
        ThriveCommon::get()->getMicrobeScripts()->ExecuteOnModule<void>(setup,
            false, const_cast<Patch const*>(patch.get()), results,
            const_cast<SimulationConfiguration const*>(config.get()));

    if(result.Result != SCRIPT_RUN_RESULT::Success) {

        LOG_ERROR("Failed to run simulatePatchPopulations");
    }
}
