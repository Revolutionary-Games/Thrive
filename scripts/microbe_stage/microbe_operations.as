// Operations on microbe entities

namespace MicrobeOperations{

// Queries the currently stored amount of an compound
//
// @param compoundId
// The id of the compound to query
//
// @returns amount
// The amount stored in the microbe's storage oraganelles
double getCompoundAmount(CellStageWorld@ world, ObjectID microbeEntity, CompoundId compoundId){
    return world.GetComponent_CompoundBagComponent(microbeEntity).
        getCompoundAmount(compoundId);
}

// Getter for microbe species
// 
// returns the species component or null if it doesn't have a valid species
SpeciesComponent@ getSpeciesComponent(CellStageWorld@ world, ObjectID microbeEntity){
    
    auto microbeComponent = getComponent(microbeEntity, MicrobeComponent);
    // This needs to loop all the components and get matching one
    return getComponent(microbeComponent.speciesName, g_luaEngine.currentGameState,
        SpeciesComponent);
}

// Retrieves the organelle occupying a hex cell
//
// @param q, r
// Axial coordinates, relative to the microbe's center
//
// @returns organelle
// The organelle at (q,r) or null if the hex is unoccupied
PlacedOrganelle@ getOrganelleAt(ObjectID microbeEntity, Int2 hex){
    auto microbeComponent = getComponent(microbeEntity, MicrobeComponent);

    for(_, organelle in pairs(microbeComponent.organelles)){
        auto localQ = q - organelle.position.q;
        auto localR = r - organelle.position.r;
        if(organelle.getHex(localQ, localR) !is null){
            return organelle;
        }
    }
    return null;
}
    
// Removes the organelle at a hex cell
// Note that this renders the organelle unusable as we destroy its underlying entity
//
// @param q, r
// Axial coordinates of the organelle's center
//
// @returns success
// True if an organelle has been removed, false if there was no organelle
// at (q,r)
// @note use a more specific version (for example damaged) if available
bool removeOrganelle(CellStageWorld@ world, ObjectID microbeEntity, Int2 hex){
    auto microbeComponent = world.GetComponent_MicrobeComponent(microbeEntity);
    auto rigidBodyComponent = world.GetComponent_RigidBodyComponent(microbeEntity);

    auto organelle = getOrganelleAt(microbeEntity, q, r);
    if(not organelle){
        return false
            }
    
    auto s = encodeAxial(organelle.position.q, organelle.position.r);
    microbeComponent.organelles[s] = null;
    
    rigidBodyComponent.properties.mass = rigidBodyComponent.properties.mass - organelle.mass;
    rigidBodyComponent.properties.touch();
    // TODO: cache for performance
    auto compoundShape = CompoundShape.castFrom(rigidBodyComponent.properties.shape);
    compoundShape.removeChildShape(
        organelle.collisionShape
    );
    
    organelle.onRemovedFromMicrobe();
    
    MicrobeSystem.calculateHealthFromOrganelles(microbeEntity);
    microbeComponent.maxBandwidth = microbeComponent.maxBandwidth -
        BANDWIDTH_PER_ORGANELLE ; // Temporary solution for decreasing max bandwidth
        
    microbeComponent.remainingBandwidth = microbeComponent.maxBandwidth;
    
    return true;
}

bool organelleDestroyedByDamage(CellStageWorld@ world, ObjectID microbeEntity, Int2 hex){

    // TODO: effects for destruction?
    return removeOrganelle(world, microbeEntity, hex);
}


}

