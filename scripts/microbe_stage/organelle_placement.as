// Helpers for organelle positioning
#include "organelle.as"

namespace OrganellePlacement{

//! Searches organelle list for an organelle at the specified hex
PlacedOrganelle@ getOrganelleAt(const array<PlacedOrganelle@>@ organelles, const Int2 &in hex)
{
    for(uint i = 0; i < organelles.length(); ++i){
        auto organelle = organelles[i];
        auto hexes = organelle.organelle.getRotatedHexes(organelle.rotation);
        for(uint c = 0; c < hexes.length(); ++c){
            auto localQ = hex.X - organelle.q;
            auto localR = hex.Y - organelle.r;
            if(hexes[c].X == localQ && hexes[c].Y == localR){
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
        auto hexes = organelle.organelle.getRotatedHexes(organelle.rotation);
        for(uint c = 0; c < hexes.length(); ++c){
            auto localQ = hex.X - organelle.q;
            auto localR = hex.Y - organelle.r;
            if(hexes[c].X == localQ && hexes[c].Y == localR){
                organelles.removeAt(i);
                return true;
            }
        }
    }

    return false;
}

}
