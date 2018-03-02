#include "microbe_stage/compounds.h"
#include "microbe_stage/simulation_parameters.h"
#include "biomes.h"

#include <Script/ScriptConversionHelpers.h>
#include <Script/ScriptExecutor.h>

#include <boost/range/adaptor/map.hpp>

#include <vector>

using namespace thrive;

Biome::Biome() {}

Biome::Biome(Json::Value value) {
	background = value["background"].asString();

	// Getting the compound information.
	Json::Value compoundData = value["compounds"];
	std::vector<std::string> compoundInternalNames = compoundData.getMemberNames();

	for (std::string compoundInternalName : compoundInternalNames) {
		unsigned int amount = compoundData[compoundInternalName]["amount"].asUInt();
		double density = compoundData[compoundInternalName]["density"].asDouble();


		// Getting the compound id from the compound registry.
		size_t id = SimulationParameters::compoundRegistry.getTypeData(compoundInternalName).id;

		compounds.emplace(std::piecewise_construct,
			std::forward_as_tuple(id),
			std::forward_as_tuple(amount, density));
	}
}
// ------------------------------------ //
BiomeCompoundData* Biome::getCompound(size_t type){
    return &compounds[type];
}

CScriptArray* Biome::getCompoundKeys() const{
    return Leviathan::ConvertIteratorToASArray((compounds | boost::adaptors::map_keys).begin(),
        (compounds | boost::adaptors::map_keys).end(),
        Leviathan::ScriptExecutor::Get()->GetASEngine(), "array<uint64>");
}
