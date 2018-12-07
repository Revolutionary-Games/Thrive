// Helpers for organelle positioning
#include "organelle.as"

namespace OrganellePlacement{

//! Searches organelle list for an organelle at the specified hex
//Rotated hex
PlacedOrganelle@ getOrganelleAt(const array<PlacedOrganelle@>@ organelles, const Int2 &in hex)
{
    for(uint i = 0; i < organelles.length(); ++i){
        auto organelle = organelles[i];
        auto hexes = organelle.organelle.getRotatedHexes(organelle.rotation);
        for(uint c = 0; c < hexes.length(); ++c){
            auto localQ = hex.X - organelle.q;
            auto localR = hex.Y - organelle.r;
            if(hexes[c].q == localQ && hexes[c].r == localR){
                return organelle;
            }
        }
    }
    return null;
}

//! Removes organelle that contains hex
bool removeOrganelleAt(array<PlacedOrganelle@>@ organelles, const Int2 &in hex)
{
    for(uint i = 0; i < organelles.length(); ++i){
        auto organelle = organelles[i];

        auto localQ = hex.X - organelle.q;
        auto localR = hex.Y - organelle.r;
        if(organelle.organelle.getHex(localQ, localR) !is null){

            organelles.removeAt(i);
            return true;
        }
    }

    return false;
}

}
