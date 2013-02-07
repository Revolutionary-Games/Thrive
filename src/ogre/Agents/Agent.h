#ifndef AGENT_H
#define	AGENT_H

class Agent
{
public:
    //virtual ~Agent(){};
    //virtual Ogre::Vector3 update();
    virtual std::string getType() = 0;
};

#endif	/* AGENT_H */

