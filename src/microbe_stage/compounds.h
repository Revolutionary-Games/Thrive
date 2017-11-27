#pragma once

#include "general\json_registry.h"

struct Compound : public RegistryType {
public:
	double volume = 1.0;
	bool isCloud = false;
	bool isUseful = false;
	Ogre::ColourValue colour;

	Compound() {}

	Compound(Json::Value value) {
		volume = value["volume"].asDouble();
		isCloud = value["isCloud"].asBool();
		isUseful = value["isUseful"].asBool();

		// Setting the cloud colour.
		float r = value["colour"]["r"].asFloat();
		float g = value["colour"]["g"].asFloat();
		float b = value["colour"]["b"].asFloat();
		colour = Ogre::ColourValue(r, g, b, 1.0);
	}
};
