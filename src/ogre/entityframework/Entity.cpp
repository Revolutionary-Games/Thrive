#include "Entity.h"

Entity::Entity() {
}

Entity::~Entity(void) {
    //delete componentMap;
}

void Entity::add(Component* component){
        std::string name  = typeid(component).name();
	componentMap[name] = component;
}

Component* Entity::get(std::string name)
{
    return componentMap[name];
}
