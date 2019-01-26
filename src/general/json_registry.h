// Template for registring stuff.

#pragma once

#include <Define.h>
#include <Exceptions.h>
#include <Include.h>

#include <json/json.h>

#include <fstream>
#include <limits>
#include <map>
#include <string>
#include <unordered_map>

// Base class of things to register.
class RegistryType {
public:
    RegistryType() {}

    //! \brief Helper for derived test constructors
    RegistryType(size_t id, const std::string& name) :
        id(id), displayName(name), internalName(name)
    {}

    // Used to search by id.
    size_t id = std::numeric_limits<size_t>::max(); // This would mean an error.

    // Used by the GUI and stuff.
    std::string displayName = "Error! Please report! :)";

    // Internal name for use inside the code
    std::string internalName = "error";
};

//! Template class that registers the stuff.
//! \todo give it more stuff like iterators and square brackets and stuff.
template<class T> class TJsonRegistry {
public:
    // Default constructor, just creates an empty registry.
    TJsonRegistry();

    // Constructor that loads information from a JSON file from the specified
    // path.
    TJsonRegistry(const std::string& defaultTypesFilePath);

    // Registers a new type with the specified properties.
    // Returns True if succeeded. False if the name is already in use.
    bool
        RegisterType(const T& Properties);

    // Returns the properties of a type. Or InvalidType if not found
    // Note: the returned value should NOT be changed
    T const&
        getTypeData(size_t id);

    // Same as above, but using the internal name. Sligthly less efficient.
    T const&
        getTypeData(const std::string& internalName);

    //! Returns the id matching a name
    size_t
        getTypeId(const std::string& internalName);

    // Get the amount of elements in the registry.
    size_t
        getSize();

    //! \returns The internal name from id
    const std::string&
        getInternalName(size_t id);

private:
    // Registered types
    std::map<size_t, T> registeredTypes;

    // Additional map for indexing the internal name.
    std::unordered_map<std::string, size_t> internalNameIndex;

    size_t nextId;
};

template<class T> TJsonRegistry<T>::TJsonRegistry()
{
    // Checking if the type passed actually inherits from the registry type.
    static_assert(std::is_base_of<RegistryType, T>::value,
        "The template parameter to a JsonRegistry should inherit from "
        "RegistryType");

    // Setting the first id to be used.
    nextId = 0;
}

template<class T>
TJsonRegistry<T>::TJsonRegistry(const std::string& defaultTypesFilePath) :
    TJsonRegistry()
{
    // Getting the JSON file where the data is stored.
    std::ifstream jsonFile;
    jsonFile.open(defaultTypesFilePath);
    if(!jsonFile.is_open())
        throw Leviathan::Exception(
            "The file '" + defaultTypesFilePath + "' failed to load!");
    Json::Value rootElement;

    try {
        jsonFile >> rootElement;
    } catch(const Json::RuntimeError& e) {
        LOG_ERROR(std::string("Syntax error in json file: '" +
                              defaultTypesFilePath + "'") +
                  ", description: " + std::string(e.what()));
        throw e;
    }

    jsonFile.close();

    // Loading the data into the registry.
    std::vector<std::string> internalTypesNames = rootElement.getMemberNames();
    for(std::string internalName : internalTypesNames) {
        registeredTypes.emplace(nextId, rootElement[internalName]);

        // Loading some values in the new type.
        registeredTypes[nextId].internalName = internalName;
        registeredTypes[nextId].id = nextId;
        registeredTypes[nextId].displayName =
            rootElement[internalName]["name"].asString();

        // Indexing the ids by the internal name.
        internalNameIndex.emplace(internalName, nextId);

        nextId++;
    }
}

template<class T>
bool
    TJsonRegistry<T>::RegisterType(const T& Properties)
{
    // TODO
    /*
    for (const auto& Type : RegisteredTypes) {

        if (Type.InternalName == Properties.InternalName) {

            UE_LOG(ThriveLog, Error, TEXT("Type internal name is already in use:
    %s"), *Properties.InternalName.ToString()); return false;
        }
    }

    RegisteredTypes.Add(Properties);
    return true;
    */
    DEBUG_BREAK;
    return false;
}

template<class T>
T const&
    TJsonRegistry<T>::getTypeData(size_t id)
{
    // The type exists.
    const auto iter = registeredTypes.find(id);
    if(iter == registeredTypes.end())
        throw Leviathan::InvalidArgument("Type not found!");
    return iter->second;
}

template<class T>
T const&
    TJsonRegistry<T>::getTypeData(const std::string& internalName)
{
    // The type exists.
    const auto iter = internalNameIndex.find(internalName);
    if(iter == internalNameIndex.end())
        throw Leviathan::InvalidArgument("Type not found!");
    return getTypeData(iter->second);
}

//! Returns the id matching a name
template<class T>
size_t
    TJsonRegistry<T>::getTypeId(const std::string& internalName)
{
    const auto iter = internalNameIndex.find(internalName);
    if(iter == internalNameIndex.end())
        throw Leviathan::InvalidArgument("Type not found!");
    return iter->second;
}

template<class T>
const std::string&
    TJsonRegistry<T>::getInternalName(size_t id)
{
    for(auto iter = internalNameIndex.begin(); iter != internalNameIndex.end();
        ++iter) {
        if(iter->second == id)
            return iter->first;
    }
    throw Leviathan::InvalidArgument("no name for id found in this registry");
}

template<class T>
size_t
    TJsonRegistry<T>::getSize()
{
    return registeredTypes.size();
}
