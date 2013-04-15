/* 
 * File:   Engine.h
 * Author: Dani Ferri
 *
 * Created on 6 de febrero de 2013, 15:20
 */

#ifndef ENGINE_H
#define	ENGINE_H

#include "Entity.h"
#include "Node.h"
#include "System.h"

class Engine
{
public:
    Engine();
    void addEntity(Entity*);
    void removeEntity(Entity*);
    void addSystem(ISystem*);
    void removeSystem(ISystem*);
    std::vector<Node*>* getNodeList(std::string);
    void update(Ogre::Real);
    
private:
    std::vector<Entity*>         entityList;
    std::vector<ISystem*>        systemList;
    std::map<std::string,std::vector<Node*>*>  nodeMap;
    
};
#endif	/* ENGINE_H */

