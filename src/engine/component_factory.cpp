#include "engine/component_factory.h"

using namespace thrive;

std::unique_ptr<Component>
ComponentFactory::load(
    const std::string& typeName,
    const StorageContainer& storage
) {
    auto iter = ComponentFactory::registry().find(typeName);
    if (iter == ComponentFactory::registry().end()) {
        return nullptr;
    }
    else {
        return iter->second(storage);
    }
}


ComponentFactory::Registry&
ComponentFactory::registry() {
    static Registry registry;
    return registry;
}
