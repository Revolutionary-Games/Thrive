// Template for registring stuff.

#pragma once

#include "Define.h"

#include <fstream>
#include <string>
#include <map>
#include <unordered_map>
#include <jsoncpp/json.h>

//! Base class of things to register.
class RegistryType {
public:
	// Used to search by id.
	unsigned int id = 0;

	// Used by the GUI and stuff.
	std::string displayName = "Error! Please report! :)";

	// Internal name for use inside the code
	std::string internalName = "error";
};

template<class T> class TJsonRegistry {
public:
	// Default constructor, just creates an empty registry.
	TJsonRegistry();

	// Constructor that loads information from a JSON file from the specified path.
	TJsonRegistry(std::string defaultTypesFilePath);

	// Registers a new type with the specified properties.
	// Returns True if succeeded. False if the name is already in use.
	bool RegisterType(T &Properties);

	// Returns the properties of a type. Or InvalidType if not found
	// Note: the returned value should NOT be changed
	T const& getTypeData(unsigned int id);

	// Same as above, but using the internal name. Sligthly less efficient.
	T const& getTypeData(std::string internalName);

private:
	// Registered types
	std::map<unsigned int, T> registeredTypes;

	// Additional map for indexing the internal name.
	std::unordered_map<std::string, unsigned int> internalNameIndex;

	unsigned int nextId;
};

template<class T> TJsonRegistry<T>::TJsonRegistry() {
	// Checking if the type passed actually inherits from the registry type.
	static_assert(
		std::is_base_of<RegistryType, T>::value,
		"The template parameter to a JsonRegistry should inherit from RegistryType"
	);

	// Setting the first id to be used.
	nextId = 1; // 0 means error.
}

template<class T> TJsonRegistry<T>::TJsonRegistry(std::string defaultTypesFilePath):
	TJsonRegistry() {
	// Getting the JSON file where the data is stored.
	std::ifstream jsonFile;
	jsonFile.open(defaultTypesFilePath);
	LEVIATHAN_ASSERT(jsonFile.is_open(), "The file '" + defaultTypesFilePath + "' failed to load!");
	Json::Value rootElement;
	jsonFile >> rootElement;
	// TODO: add some sort of validation of the receiving JSON file, otherwise it fails silently and makes the screen go black.
	jsonFile.close();

	// Loading the data into the registry.
	std::vector<std::string> internalTypesNames = rootElement.getMemberNames();
	for (std::string internalName : internalTypesNames) {
		registeredTypes.emplace(nextId, rootElement[internalName]);

		// Loading some values in the new type.
		registeredTypes[nextId].internalName = internalName;
		registeredTypes[nextId].id = nextId;
		registeredTypes[nextId].displayName = rootElement[internalName]["name"].asString();

		// Indexing the ids by the internal name.
		internalNameIndex.emplace(internalName, nextId);

		nextId++;
	}
}

template<class T> bool TJsonRegistry<T>::RegisterType(T &Properties) {
	/*
	for (const auto& Type : RegisteredTypes) {

		if (Type.InternalName == Properties.InternalName) {

			UE_LOG(ThriveLog, Error, TEXT("Type internal name is already in use: %s"),
				*Properties.InternalName.ToString());
			return false;
		}
	}

	RegisteredTypes.Add(Properties);
	return true;
	*/
}

template<class T> T const&  TJsonRegistry<T>::getTypeData(unsigned int id) {
	// The type exists.
	LEVIATHAN_ASSERT(registeredTypes.count(id), "Type not found!");
	return registeredTypes[id];
}

template<class T> T const&  TJsonRegistry<T>::getTypeData(std::string internalName) {
	// The type exists.
	LEVIATHAN_ASSERT(internalNameIndex.count(internalName) == 1, "Type not found!");
	return getTypeData(internalNameIndex[internalName]);
}
