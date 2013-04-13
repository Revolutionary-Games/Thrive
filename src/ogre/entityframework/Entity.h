#ifndef ENTITY_H_
#define ENTITY_H_

#include <map>
#include "Component.h"

class Entity
{
public:
	Entity(void);
	virtual ~Entity(void);
	void add(Component*);
        bool has(std::vector<std::string>);
        Component* get(std::string);
private:

	std::map<std::string, Component*>	componentMap;
};


#endif /* ENTITY_H_ */
