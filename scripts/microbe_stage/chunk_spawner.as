// Factory for chunks and helpers for spawning the right compound clouds for the current patch


namespace ChunkSpawner{

class Chunkfactory{

    Chunkfactory(uint c){

        chunkId = c;
    }

    ObjectID spawn(CellStageWorld@ world, Float3 pos){
        return createChunk(world, chunkId, pos);
    }

            private uint chunkId;
}

dictionary chunkSpawnTypes;

}
