#include "Entity.h"

Entity::Entity() {
}

Entity::~Entity(void) {
    //delete componentMap;
}

void Entity::add(Component* component){
        std::string name  = component->getType();
	componentMap[name] = component;
}

bool Entity::has(std::vector<std::string> nodeNameVector)
{
    bool resoult = true;
    for (std::vector<std::string>::iterator i = nodeNameVector.begin();i!=nodeNameVector.end();i++)
    {
        if (componentMap.count((*i))==0)
        {
            resoult = false;
            break;
        }
    }
    return resoult;
}

Component* Entity::get(std::string name)
{
    return componentMap[name];
}
