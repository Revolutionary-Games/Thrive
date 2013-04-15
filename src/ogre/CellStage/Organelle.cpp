#include "Organelle.h"

NucleusOrganelle::NucleusOrganelle()
{
    
}
std::string NucleusOrganelle::getType()
{
    return "Nucleus";
}
bool NucleusOrganelle::canFit(std::vector<Organelle*>*)
{
    return true;
}

FlagelaOrganelle::FlagelaOrganelle()
{
    
}
std::string FlagelaOrganelle::getType()
{
    return "Flagela";
}
bool FlagelaOrganelle::canFit(std::vector<Organelle*>*)
{
    return true;
}

MitochondriaOrganelle::MitochondriaOrganelle()
{
    
}
std::string MitochondriaOrganelle::getType()
{
    return "Mitochondria";
}
bool MitochondriaOrganelle::canFit(std::vector<Organelle*>* organelleList)
{
    for (std::vector<Organelle*>::iterator i = organelleList->begin();i!=organelleList->end();i++){
        if ((*i)->getType()=="Nucleus")
            return true;
    }
    return false;
}

