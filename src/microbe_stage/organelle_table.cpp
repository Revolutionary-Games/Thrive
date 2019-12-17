// ------------------------------------ //
#include "organelle_table.h"

#include "simulation_parameters.h"

#include <Script/ScriptConversionHelpers.h>

#include <boost/range/adaptor/map.hpp>

using namespace thrive;
// ------------------------------------ //
std::unordered_map<std::string, OrganelleTemplate::pointer>
    OrganelleTable::mainOrganelleTable;

bool OrganelleTable::initialized = false;
// ------------------------------------ //
bool
    OrganelleTable::initIfNeeded()
{
    if(initialized)
        return false;

    initialized = true;

    LOG_INFO("Initializing organelles");

    for(size_t organelleId = 0;
        organelleId < SimulationParameters::organelleRegistry.getSize();
        ++organelleId) {
        const auto name =
            SimulationParameters::organelleRegistry.getInternalName(
                organelleId);

        try {
            mainOrganelleTable[name] =
                OrganelleTemplate::MakeShared<OrganelleTemplate>(
                    SimulationParameters::organelleRegistry.getTypeData(
                        organelleId));
        } catch(const std::exception& e) {
            LOG_ERROR(
                "Organelle '" + name + "' initialization failed: " + e.what());
            throw;
        }
    }

    return true;
}

void
    OrganelleTable::release()
{
    mainOrganelleTable.clear();
}
// ------------------------------------ //
OrganelleTemplate*
    OrganelleTable::getOrganelleDefinition(const std::string& name)
{
    const auto found = mainOrganelleTable.find(name);

    if(found == mainOrganelleTable.end()) {
        LOG_ERROR("getOrganelleDefinition: no organelle named '" + name + "'");
        throw Leviathan::InvalidArgument("no such organelle: " + name);
    }

    // This should never be null, but just in case
    if(found->second)
        found->second->AddRef();

    return found->second.get();
}
// ------------------------------------ //
CScriptArray*
    OrganelleTable::getOrganelleNames()
{
    return Leviathan::ConvertIteratorToASArray(
        (mainOrganelleTable | boost::adaptors::map_keys).begin(),
        (mainOrganelleTable | boost::adaptors::map_keys).end(),
        Leviathan::ScriptExecutor::Get()->GetASEngine(), "array<string>");
}
