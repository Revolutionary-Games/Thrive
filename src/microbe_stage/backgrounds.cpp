// ------------------------------------ //
#include "backgrounds.h"

using namespace thrive;
// ------------------------------------ //
Background::Background(Json::Value value)
{
    const auto textures = value["textures"];
    if(textures.isString()) {

        layers.push_back(textures.asString());

    } else {
        if(!textures.isArray())
            throw InvalidArgument(
                "object has no textures or it is not an array");

        if(textures.size() < 1)
            throw InvalidArgument("textures array is empty");

        for(Json::ArrayIndex i = 0; i < textures.size(); ++i) {
            layers.push_back(textures[i].asString());
        }
    }
}
