#ifndef ORGANELLE_H
#define	ORGANELLE_H

#include <OgreRoot.h>

class Organelle
{
public:
    virtual std::string getType();
    virtual bool canFit(std::vector<Organelle*>*);
};

class NucleusOrganelle : public Organelle
{
public:
    NucleusOrganelle();
    Ogre::Real size;
    std::string getType();
    bool canFit(std::vector<Organelle*>*);
};

class FlagelaOrganelle : public Organelle
{
public:
    FlagelaOrganelle();
    Ogre::Real size;
    std::string getType();
    bool canFit(std::vector<Organelle*>*);
};

class MitochondriaOrganelle : public Organelle
{
public:
    MitochondriaOrganelle();
    Ogre::Real size;
    std::string getType();
    bool canFit(std::vector<Organelle*>*);
};

#endif	/* ORGANELLE_H */

