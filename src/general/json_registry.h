// Template for registring stuff.

#pragma once

#include <fstream>
#include <string>
#include <map>

#include <jsoncpp\json.h>

//! Base class of things to register.
struct RegistryType {
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

	std::vector<std::string> debugtable;

private:
	// Registered types
	std::map<unsigned int, T> registeredTypes;

	// Additional map for indexing the internal name.
	std::unordered_map<std::string, unsigned int> internalNameIndex;

	// For returning references to invalid types.
	T invalidRegisterType;

	unsigned int nextId;
};

template<class T> TJsonRegistry<T>::TJsonRegistry() {
	// Checking if the type passed actually inherits from the registry type.
	static_assert(
		std::is_base_of<RegistryType, T>::value,
		"The template parameter to a JsonRegistry should inherit from RegistryType"
	);

	// Setting an invalid type.
	invalidRegisterType.id = 0;
	invalidRegisterType.internalName = "invalid";
	invalidRegisterType.displayName = "Invalid! Please report! :)";

	// Setting the first id to be used.
	nextId = 2; // 1 means invalid, 0 means error.
}

template<class T> TJsonRegistry<T>::TJsonRegistry(std::string defaultTypesFilePath):
	TJsonRegistry() {
	// Getting the JSON file where the data is stored.
	std::ifstream jsonFile;
	jsonFile.open(defaultTypesFilePath);
	LEVIATHAN_ASSERT(jsonFile.is_open(), "The file '" + defaultTypesFilePath + "' failed to load!");
	Json::Value rootElement;
	jsonFile >> rootElement;
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
	/*
	FString PathToFile = FPaths::GameContentDir() + Path;

	if (!FPlatformFileManager::Get().GetPlatformFile().FileExists(*PathToFile)) {

		LOG_FATAL("File missing"); //TODO: add the path to the error message.
		return;
	}


	FString RawJsonFile = "No chance!";
	FFileHelper::LoadFileToString(RawJsonFile, *PathToFile, 0);

	// Deserializing it.
	TSharedPtr<FJsonObject> Types;
	TSharedRef< TJsonReader<> > Reader = TJsonReaderFactory<>::Create(RawJsonFile);
	FJsonSerializer::Deserialize(Reader, Types);

	//Getting the data.
	for (auto Type : Types->Values) {
		const FString& InternalName = Type.Key;
		const TSharedPtr<FJsonObject>& TypeData = Type.Value->AsObject();

		T NewType = GetTypeFromJsonObject(TypeData);
		NewType.InternalName = FName(*InternalName);
		NewType.DisplayName = TypeData->GetStringField("name");

		RegisterType(NewType);
	}
	*/
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
	if (registeredTypes.count(id))
		return registeredTypes[id];

	// The type doesn't exist.
	return invalidRegisterType;
}

template<class T> T const&  TJsonRegistry<T>::getTypeData(std::string internalName) {
	// The type exists.
	if (internalNameIndex.count(internalName))
		return getTypeData(internalNameIndex[internalName]);

	// The type doesn't exist.
	return invalidRegisterType;
}
