// Spawn functions for microbes

// TODO: this is likely a huge cause of lag. Would be nice to be able
// to spawn these so that only one per tick is spawned.
ObjectID bacteriaColonySpawn(CellStageWorld@ world, const Float3 &in pos,
    const string &in name)
{
    Float3 curSpawn = Float3(GetEngine().GetRandom().GetNumber(1, 7), 0,
        GetEngine().GetRandom().GetNumber(1, 7));

    // Three kinds of colonies are supported, line colonies and clump coloniesand Networks
    if (GetEngine().GetRandom().GetNumber(0, 4) < 2)
    {
        // Clump
        for(int i = 0; i < GetEngine().GetRandom().GetNumber(MIN_BACTERIAL_COLONY_SIZE,
                MAX_BACTERIAL_COLONY_SIZE); i++){

            //dont spawn them on top of each other because it
            //causes them to bounce around and lag
            MicrobeOperations::spawnMicrobe(world, pos + curSpawn, name, true, true);
            curSpawn = curSpawn + Float3(GetEngine().GetRandom().GetNumber(-7, 7), 0,
                GetEngine().GetRandom().GetNumber(-7, 7));
        }
    }
    else if (GetEngine().GetRandom().GetNumber(0,30) > 2)
    {
        // Line
        // Allow for many types of line
        float lineX = GetEngine().GetRandom().GetNumber(-5, 5) + GetEngine().GetRandom().
            GetNumber(-5, 5);
        float linez = GetEngine().GetRandom().GetNumber(-5, 5) + GetEngine().GetRandom().
            GetNumber(-5, 5);

        for(int i = 0; i < GetEngine().GetRandom().GetNumber(MIN_BACTERIAL_LINE_SIZE,
                MAX_BACTERIAL_LINE_SIZE); i++){

            // Dont spawn them on top of each other because it
            // Causes them to bounce around and lag
            MicrobeOperations::spawnMicrobe(world, pos+curSpawn, name, true, true);
            curSpawn = curSpawn + Float3(lineX + GetEngine().GetRandom().GetNumber(-2, 2),
                0, linez + GetEngine().GetRandom().GetNumber(-2, 2));
        }
    }
    else{
        // Network
        // Allows for "jungles of cyanobacteria"
        // Network is extremely rare
        float x = curSpawn.X;
        float z = curSpawn.Z;
        // To prevent bacteria being spawned on top of each other
        bool horizontal = false;
        bool vertical = false;

        for(int i = 0; i < GetEngine().GetRandom().GetNumber(MIN_BACTERIAL_COLONY_SIZE,
                MAX_BACTERIAL_COLONY_SIZE); i++)
        {
            if (GetEngine().GetRandom().GetNumber(0, 4) < 2 && !horizontal)
            {
                horizontal = true;
                vertical = false;

                for(int c = 0; c < GetEngine().GetRandom().GetNumber(
                        MIN_BACTERIAL_LINE_SIZE, MAX_BACTERIAL_LINE_SIZE); ++c){

                    // Dont spawn them on top of each other because
                    // It causes them to bounce around and lag
                    curSpawn.X += GetEngine().GetRandom().GetNumber(5, 7);

                    // Add a litlle organicness to the look
                    curSpawn.Z += GetEngine().GetRandom().GetNumber(-2, 2);
                    MicrobeOperations::spawnMicrobe(world, pos + curSpawn, name,
                        true, true);
                }
            }
            else if (GetEngine().GetRandom().GetNumber(0,4) < 2 && !vertical) {
                horizontal=false;
                vertical=true;
                for(int c = 0; c < GetEngine().GetRandom().GetNumber(MIN_BACTERIAL_LINE_SIZE,MAX_BACTERIAL_LINE_SIZE); ++c){
                    // Dont spawn them on top of each other because it
                    // Causes them to bounce around and lag
                    curSpawn.Z += GetEngine().GetRandom().GetNumber(5,7);
                    // Add a litlle organicness to the look
                    curSpawn.X += GetEngine().GetRandom().GetNumber(-2,2);
                    MicrobeOperations::spawnMicrobe(world, pos+curSpawn, name, true,
                        true);
                }
            }
            else if (GetEngine().GetRandom().GetNumber(0, 4) < 2 && !horizontal)
            {
                horizontal = true;
                vertical = false;

                for(int c = 0; c < GetEngine().GetRandom().GetNumber(
                        MIN_BACTERIAL_LINE_SIZE, MAX_BACTERIAL_LINE_SIZE); ++c){

                    // Dont spawn them on top of each other because
                    // It causes them to bounce around and lag
                    curSpawn.X -= GetEngine().GetRandom().GetNumber(5, 7);
                    // Add a litlle organicness to the look
                    curSpawn.Z -= GetEngine().GetRandom().GetNumber(-2, 2);
                    MicrobeOperations::spawnMicrobe(world, pos + curSpawn, name,
                        true, true);
                }
            }
            else if (GetEngine().GetRandom().GetNumber(0, 4) < 2 && !vertical) {
                horizontal = false;
                vertical = true;

                for(int c = 0; c < GetEngine().GetRandom().GetNumber(
                        MIN_BACTERIAL_LINE_SIZE, MAX_BACTERIAL_LINE_SIZE); ++c){

                    // Dont spawn them on top of each other because it
                    //causes them to bounce around and lag
                    curSpawn.Z -= GetEngine().GetRandom().GetNumber(5, 7);
                    //add a litlle organicness to the look
                    curSpawn.X -= GetEngine().GetRandom().GetNumber(-2, 2);
                    MicrobeOperations::spawnMicrobe(world, pos+curSpawn, name, true,
                        true);
                }
            }
            else {
                // Diagonal
                horizontal = false;
                vertical = false;

                for(int c = 0; c < GetEngine().GetRandom().GetNumber(
                        MIN_BACTERIAL_LINE_SIZE, MAX_BACTERIAL_LINE_SIZE); ++c){

                    // Dont spawn them on top of each other because it
                    // Causes them to bounce around and lag
                    curSpawn.Z += GetEngine().GetRandom().GetNumber(5, 7);
                    curSpawn.X += GetEngine().GetRandom().GetNumber(5, 7);
                    MicrobeOperations::spawnMicrobe(world, pos + curSpawn, name,
                        true, true);
                }
            }
        }
    }

    return MicrobeOperations::spawnMicrobe(world, pos, name, true);
}

