
#include "microbe_stage/bio_process_registry.h"
#include "microbe_stage/compound.h"
#include "microbe_stage/compound_registry.h"
#include "scripting/luajit.h"

using namespace thrive;

void BioProcessRegistry::luaBindings(
    sol::state &lua
){
    lua.new_usertype<BioProcessRegistry>("BioProcessRegistry",

        "new", sol::no_constructor,

        "loadFromLua", &BioProcessRegistry::loadFromLua,
        "getDisplayName", &BioProcessRegistry::getDisplayName,
        "getInternalName", &BioProcessRegistry::getInternalName,
        "getId", &BioProcessRegistry::getId,

        "getList", [](sol::this_state s){

            THRIVE_BIND_ITERATOR_TO_TABLE(BioProcessRegistry::getList());
        },

        "getInputCompounds", &BioProcessRegistry::getInputCompounds,
        "getOutputCompounds", &BioProcessRegistry::getOutputCompounds
    );

    // class_<std::pair<CompoundId, int>>("RecipyCompound")
    //     .def_readonly("compoundId", &std::pair<CompoundId, int>::first)
    //     .def_readonly("amount", &std::pair<CompoundId, int>::second)
}

namespace {
    struct BioProcessEntry
    {
        std::string internalName;
        std::string displayName;
        std::vector<std::pair<CompoundId, int>> inputCompounds;
        std::vector<std::pair<CompoundId, int>> outputCompounds;
    };
}

static std::unordered_map<std::string, BioProcessId>&
processRegistryMap() {
    static std::unordered_map<std::string, BioProcessId> processRegistry;
    return processRegistry;
}

static std::vector<BioProcessEntry>&
processRegistry() {
    static std::vector<BioProcessEntry> processRegistry;
    return processRegistry;
}

void
BioProcessRegistry::loadFromLua(
    sol::table processTable
    )
{

    for(const auto& pair : processTable){

        const auto key = pair.first.as<std::string>();

        if(!pair.second.is<sol::table>())
            throw std::runtime_error("BioProcessRegistry value is not a table");

        auto data = pair.second.as<sol::table>();

        sol::table inputTable = data.get<sol::table>("inputs");
        sol::table outputTable = data.get<sol::table>("outputs");

        std::vector<std::pair<CompoundId, int>> inputs;
        std::vector<std::pair<CompoundId, int>> outputs;

        for(const auto& inputsPair : inputTable){

            const auto compound = inputsPair.first.as<std::string>();
            const auto amount = inputsPair.second.as<float>();
            inputs.push_back({CompoundRegistry::getCompoundId(compound), amount});
        }

        for(const auto& outputsPair : outputTable){

            const auto compound = outputsPair.first.as<std::string>();
            const auto amount = outputsPair.second.as<float>();
            outputs.push_back({CompoundRegistry::getCompoundId(compound), amount});
        }

        registerBioProcess(
               key,
               key,
               std::move(inputs),
               std::move(outputs)
            );
    }
}

BioProcessId
BioProcessRegistry::registerBioProcess(
    const std::string& internalName,
    const std::string& displayName,
    std::vector<std::pair<CompoundId, int>> inputCompounds,
    std::vector<std::pair<CompoundId, int>> outputCompounds
) {
    if (processRegistryMap().count(internalName) == 0) {
        BioProcessEntry entry;
        entry.internalName = internalName;
        entry.displayName = displayName;
        entry.inputCompounds = inputCompounds;
        entry.outputCompounds = outputCompounds;
        processRegistry().push_back(entry);
        processRegistryMap().emplace(std::string(internalName), processRegistry().size());
        return processRegistry().size();
    }
    else {
        throw std::invalid_argument("Duplicate internalName not allowed.");
    }
}

std::string
BioProcessRegistry::getDisplayName(
    BioProcessId id
) {
    if (static_cast<std::size_t>(id) > processRegistry().size())
        throw std::out_of_range("Index of process does not exist.");
    return processRegistry()[id-1].displayName;
}


std::string
BioProcessRegistry::getInternalName(
    BioProcessId id
) {
    if (static_cast<std::size_t>(id) > processRegistry().size())
        throw std::out_of_range("Index of process does not exist.");
    return processRegistry()[id-1].internalName;
}

BioProcessId
BioProcessRegistry::getId(
    const std::string& internalName
) {
    BioProcessId processId;
    try
    {
        processId = processRegistryMap().at(internalName);
    }
    catch(std::out_of_range&)
    {
        throw std::out_of_range("Internal name of process does not exist.");
    }
    return processId;
}


const BoostCompoundMapIterator
BioProcessRegistry::getList(
) {
    return processRegistryMap() | boost::adaptors::map_values;
}

const std::vector<std::pair<CompoundId, int>>&
BioProcessRegistry::getInputCompounds(
    BioProcessId id
) {
    if (static_cast<std::size_t>(id) > processRegistry().size())
        throw std::out_of_range("Index of process does not exist.");
    return processRegistry()[id-1].inputCompounds;
}

const std::vector<std::pair<CompoundId, int>>&
BioProcessRegistry::getOutputCompounds(
    BioProcessId id
) {
    if (static_cast<std::size_t>(id) > processRegistry().size())
        throw std::out_of_range("Index of process does not exist.");
    return processRegistry()[id-1].outputCompounds;
}
