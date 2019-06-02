#include "microbe_stage/patch.h"

using namespace thrive;

Patch::Patch(std::string name) 
{
    this->name = name;
}

Patch::~Patch() 
{
    delete patchBiome;
}

std::string
Patch::getName()
{
    return this->name;
}

void
Patch::setName(std::string name)
{
    this->name = name;
}

Biome*
Patch::getBiome()
{
    return this->patchBiome;
}
void
Patch::setBiome(Biome* biome)
{
    if(patchBiome)
        delete patchBiome;
    this->patchBiome = biome;
}

size_t
Patch::getId()
{
    return this->patchId;
}