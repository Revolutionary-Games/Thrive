// ------------------------------------ //
#include "organelle_template.h"

#include "general/hex.h"
#include "simulation_parameters.h"
#include "thrive_common.h"

#include <Script/CustomScriptRunHelpers.h>
#include <Script/ScriptConversionHelpers.h>
#include <Script/ScriptTypeResolver.h>
#include <add_on/scriptarray/scriptarray.h>
#include <add_on/scriptdictionary/scriptdictionary.h>

using namespace thrive;
// ------------------------------------ //
constexpr auto ORGANELLE_FACTORY_NAME_PREFIX = "organelleFactory_";

// ------------------------------------ //
// OrganelleTemplate::OrganelleComponentType
OrganelleTemplate::OrganelleComponentType::OrganelleComponentType(
    const std::string& name) :
    name(name)
{}

OrganelleTemplate::OrganelleComponentType::~OrganelleComponentType()
{
    SAFE_RELEASE(factoryFunction);
}

OrganelleTemplate::OrganelleComponentType::OrganelleComponentType(
    OrganelleComponentType&& other) :
    name(other.name),
    factoryFunction(other.factoryFunction),
    factoryParams(std::move(other.factoryParams))
{
    other.factoryFunction = nullptr;
}
// ------------------------------------ //
// OrganelleTemplate
OrganelleTemplate::OrganelleTemplate(const OrganelleType& parameters) :
    m_name(parameters.name), m_mass(parameters.mass), m_gene(parameters.gene),
    m_mesh(parameters.mesh), m_texture(parameters.texture),
    m_chanceToCreate(parameters.chanceToCreate),
    m_prokaryoteChance(parameters.prokaryoteChance), m_mpCost(parameters.mpCost)
{
    // Copy composition data
    for(const auto& [name, amount] : parameters.initialComposition) {
        m_initialComposition[SimulationParameters::compoundRegistry.getTypeId(
            name)] = amount;
    }

    // Copy process data and turn it into tweaked processes
    for(const auto& [name, rate] : parameters.processes) {
        const auto process =
            SimulationParameters::bioProcessRegistry.getTypeData(name);

        try {
            m_processes.push_back(
                TweakedProcess::MakeShared<TweakedProcess>(name, rate));
        } catch(const std::exception&) {
            LOG_ERROR("Invalid process in organelle definition");
            throw;
        }
    }

    // Copy component data. This is the most complex as we need to find and be
    // able to call script factory functions

    auto scripts = ThriveCommon::get()->getMicrobeScripts();

    LEVIATHAN_ASSERT(scripts, "scripts not loaded");
    LEVIATHAN_ASSERT(
        scripts->GetScriptModule(), "scripts ScriptModule doesn't exist");
    auto* scriptModule = scripts->GetScriptModule()->GetModule();
    LEVIATHAN_ASSERT(scriptModule, "script module not built");

    for(const auto& [type, parameters] : parameters.components) {

        OrganelleComponentType componentData(type);

        // Find a factory
        const auto factoryName = ORGANELLE_FACTORY_NAME_PREFIX + type;

        componentData.factoryFunction =
            scriptModule->GetFunctionByName(factoryName.c_str());

        if(!componentData.factoryFunction)
            throw InvalidArgument("Could not find a component factory for: " +
                                  type + ", expected name: " + factoryName);

        // The component data will hold the script function
        componentData.factoryFunction->AddRef();

        // TODO: check return type
        // componentData.factoryFunction->GetReturnTypeId();

        // Verify parameters and their order
        std::string error;

        for(unsigned i = 0; i < componentData.factoryFunction->GetParamCount();
            ++i) {

            // First look up parameter info
            // TODO: could add handling default value
            // const char** defaultArg

            int currentParamType;
            const char* paramName;
            asDWORD flags;

            if(componentData.factoryFunction->GetParam(
                   i, &currentParamType, &flags, &paramName) < 0) {
                throw InvalidArgument("failed to get func param type");
            }

            // Try to find a json value with a matching name
            const auto foundParamData = parameters.find(paramName);

            if(foundParamData == parameters.end()) {

                error = "JSON value is missing for parameter: " +
                        std::string(paramName);
                break;
            }

            const auto jsonValue = foundParamData->second;

            // // Remove const flag
            // currentParamType ^=

            // Then try to convert the given json value to match what the script
            // wants
            try {
                if(currentParamType ==
                    Leviathan::AngelScriptTypeIDResolver<float>::Get(
                        Leviathan::ScriptExecutor::Get())) {

                    componentData.factoryParams.push_back(jsonValue.asFloat());

                } else if(currentParamType ==
                          Leviathan::
                              AngelScriptTypeIDResolver<const std::string>::Get(
                                  Leviathan::ScriptExecutor::Get())) {

                    componentData.factoryParams.push_back(jsonValue.asString());

                } else {
                    error = "Unsupported parameter type (index: " +
                            std::to_string(i) + "): " +
                            Leviathan::ScriptExecutor::Get()->GetTypeName(
                                currentParamType);
                    break;
                }
            } catch(const Json::Exception& e) {
                error = "JSON value is wrong type for parameter (index: " +
                        std::to_string(i) + "): " + e.what();
                break;
            }
        }

        if(!error.empty())
            throw InvalidArgument(
                "Invalid parameters in organelle for component: " + type +
                " error: " + error);

        m_components.emplace_back(std::move(componentData));
    }

    // Add hexes //
    for(const auto& hex : parameters.hexes) {
        if(!addHex(hex.X, hex.Y))
            LOG_WARNING(
                "Adding hex to organelle failed: " + Convert::ToString(hex));
    }

    // Calculate organelleCost and compoundsLeft
    calculateCost(m_initialComposition);

    createScriptInitialComposition();
}
OrganelleTemplate::~OrganelleTemplate()
{
    // Don't leak cached script arrays
    for(auto& pair : m_rotatedHexesCache) {
        SAFE_RELEASE(pair.second);
    }

    SAFE_RELEASE(m_initialCompositionDictionary);
}
// ------------------------------------ //
bool
    OrganelleTemplate::containsHex(int q, int r) const
{
    Int2 hex{q, r};
    for(const auto& existing : m_hexes) {
        if(existing == hex)
            return true;
    }
    return false;
}
// ------------------------------------ //
std::vector<Int2>
    OrganelleTemplate::getRotatedHexes(int rotation) const
{
    const int times = rotation / 60;

    std::vector<Int2> result;
    result.reserve(m_hexes.size());

    for(const auto& hex : m_hexes) {

        result.push_back(Hex::rotateAxialNTimes(hex, times));
    }

    return result;
}

const CScriptArray*
    OrganelleTemplate::getRotatedHexesWrapper(int rotation) const
{
    const int times = rotation / 60;

    // Check cache
    const auto cached = m_rotatedHexesCache.find(times);

    if(cached != m_rotatedHexesCache.end()) {
        // Need to add a reference for the returned value
        if(cached->second)
            cached->second->AddRef();
        return cached->second;
    }

    // Not cached, need to calculate
    const auto resultData = getRotatedHexes(rotation);

    auto result = Leviathan::ConvertVectorToASArray(resultData,
        Leviathan::ScriptExecutor::Get()->GetASEngine(), "array<Int2>");

    m_rotatedHexesCache.insert(std::make_pair(times, result));

    // Add our cached reference
    result->AddRef();

    return result;
}
// ------------------------------------ //
Float3
    OrganelleTemplate::calculateCenterOffset() const
{
    Float3 offset = Float3(0, 0, 0);

    for(const auto& hex : m_hexes) {
        offset += Hex::axialToCartesian(hex.X, hex.Y);
    }

    offset /= m_hexes.size();
    return offset;
}

Float3
    OrganelleTemplate::calculateModelOffset() const
{
    return ((calculateCenterOffset() /= getHexCount())) * DEFAULT_HEX_SIZE;
}
// ------------------------------------ //
asIScriptObject*
    OrganelleTemplate::createComponent(uint64_t index) const
{
    if(index >= m_components.size())
        throw InvalidArgument("index out of range");

    const auto& component = m_components[index];

    auto* executor = Leviathan::ScriptExecutor::Get();

    auto run = executor->PrepareCustomScriptRun(component.factoryFunction);

    if(!run) {
        LOG_ERROR("OrganelleTemplate failed to create custom run");
        return nullptr;
    }

    // Pass our preparsed parameters
    for(auto& param : component.factoryParams) {

        bool success = false;

        if(auto* ptr = std::get_if<float>(&param); ptr) {

            success = Leviathan::PassParameterToCustomRun(run, *ptr);

        } else if(auto* ptr = std::get_if<std::string>(&param); ptr) {

            success = Leviathan::PassParameterToCustomRun(run,
                const_cast<std::string*>(ptr),
                Leviathan::AngelScriptTypeIDResolver<const std::string>::Get(
                    executor));

        } else {
            LOG_FATAL("unhandled type in OrganelleComponentType factoryParams");
        }

        if(!success) {

            LOG_ERROR("OrganelleTemplate failed to pass a parameter to the "
                      "factory function");
            return nullptr;
        }
    }

    auto result = executor->ExecuteCustomRun<asIScriptObject*>(run);

    if(result.Result != SCRIPT_RUN_RESULT::Success || !result.Value) {
        LOG_ERROR("OrganelleTemplate failed to run component factory function "
                  "or it returned null");
        return nullptr;
    }

    // Returned object needs to have refcount incremented
    result.Value->AddRef();
    return result.Value;
}
// ------------------------------------ //
const CScriptDictionary*
    OrganelleTemplate::getInitialCompositionDictionary() const
{
    if(m_initialCompositionDictionary)
        m_initialCompositionDictionary->AddRef();
    return m_initialCompositionDictionary;
}
// ------------------------------------ //
bool
    OrganelleTemplate::addHex(int q, int r)
{
    if(containsHex(q, r))
        return false;

    m_hexes.emplace_back(q, r);
    return true;
}
// ------------------------------------ //
void
    OrganelleTemplate::calculateCost(const OrganelleComposition& composition)
{
    m_organelleCost = 0;

    for(const auto [compound, amount] : composition) {

        m_organelleCost += amount;
    }
}
// ------------------------------------ //
void
    OrganelleTemplate::createScriptInitialComposition()
{
    // Create a script version of initial composition
    m_initialCompositionDictionary = CScriptDictionary::Create(
        Leviathan::ScriptExecutor::Get()->GetASEngine());

    const auto floatType = Leviathan::AngelScriptTypeIDResolver<float>::Get(
        Leviathan::ScriptExecutor::Get());

    for(const auto [compound, amount] : m_initialComposition) {

        const std::string key = std::to_string(compound);

        // float amountFloat = amount;

        // m_initialCompositionDictionary->Set(key, &amountFloat, floatType);

        // This seems to be the right way to set the data so that the scripts
        // can read it like they expect
        m_initialCompositionDictionary->Set(
            key, static_cast<double>(floatType));
    }
}
