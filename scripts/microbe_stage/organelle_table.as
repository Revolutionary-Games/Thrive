// TODO: update this
/*
Organelle atributes:
    mass:   How heavy an organelle is. Affects speed, mostly.

    components: A table with the components an organelle has, plus
                the arguments the component needs to initialize.
                Refer to the particular component's lua file for
                more information.

    mpCost: The cost (in mutation points) an organelle costs in the
            microbe editor.

    mesh:   The name of the mesh file of the organelle.
            It has to be in the models folder.

    hexes:  A table of the hexes that the organelle occupies.
            Each hex it's represented by a table that looks like this:
                {["q"]=q,   ["r"]=r}
            where q and r are the hex position in axial coordinates.

    gene:   The letter that will be used by the auto-evo system to
            identify this organelle.

    chanceToCreate: The (relative) chance this organelle will appear in a randomly
                    generated or mutated microbe (to do roulette selection).

    processes:  A table with all the processes this organelle does,
                and the capacity of the process (the amount of
                process that can be made in one second).
                TODO: put it in the procesOrganelle component?

    composition:    A table with the compounds that compost the organelle.
                    They are needed in order to split the organelle, and a
                    percentage of them is released upon death of the microbe.
*/
#include "organelle.as"
#include "organelle_component.as"

#include "process_table.as"

// For AxialCoordinates
#include "configs.as"

// Need to include all the used organelle types for the factory functions
#include "nucleus_organelle.as"

//! Factory typedef for OrganelleComponent
funcdef OrganelleComponent@ OrganelleComponentFactoryFunc();

class OrganelleComponentFactory{

    OrganelleComponentFactory(OrganelleComponentFactoryFunc@ f, const string &in name){

        create = f;
        this.name = name;
    }

    OrganelleComponentFactoryFunc@ create;
    string name;
}

//! This replaced the old tables that specified things for cells.
//! This is clearer as to what are valid properties
class OrganelleParameters{

    OrganelleParameters(const string &in name){

        this.name = name;
    }
    float mass = 0;
    string name;
    string gene = "INVALID";
    
    //! Chance of randomly generating this (probably used by the initial random species)
    float chanceToCreate = 0.0;

    //! The factories for the components that define what this organelle does
    array<OrganelleComponentFactory@> components;

    //! The initial amount of compounds this organelle consists of
    dictionary initialComposition;

    //! Cost in mutation points
    int mpCost = 0;
}

//! Cache the result if called multiple times for the same world
dictionary@ getOrganelleTable(GameWorld@ world){

    return cast<dictionary@>(_mainOrganelleTable[formatInt(world.GetID())]);
}

// ------------------------------------ //
// Private part starts here don't directly call or read these things from any other file
// Only thing you'll need to modify is the "Main organelle table" below

// Don't touch this from anywhere except setupOrganellesForWorld
dictionary _mainOrganelleTable;

// Factory functions for all the organelle components

OrganelleComponent@ makeNucleusOrganelle(){
    dictionary arguments;
    dictionary data;
    return NucleusOrganelle(arguments, data);
}

OrganelleComponentFactory@ nucleusComponentFactory = OrganelleComponentFactory(
    @makeNucleusOrganelle, "NucleusOrganelle"
);

// Setups the organelle table for a specific world
void setupOrganellesForWorld(CellStageWorld@ world){

    assert(SimulationParameters::compoundRegistry().getSize() > 0,
        "Compound registry is empty");
    
    dictionary@ newWorldTable = dictionary();

    //
    //
    // ------------------------------------ //
    // Main organelle table is now here
    //

    // ------------------------------------ //
    // Nucleus
    auto nucleusParameters = OrganelleParameters("nucleus");
    
    nucleusParameters.mass = 0.7;
    nucleusParameters.gene = "N";
    nucleusParameters.mesh = "nucleus.mesh";
    nucleusParameters.chanceToCreate = 0; // Not randomly generated.
    nucleusParameters.mpCost = 0; //it's not supossed to be purchased.
    nucleusParameters.initialComposition = {
        {"aminoacids", 4}
    };
    nucleusParameters.components = {
        nucleusComponentFactory
    };
    nucleusParameters.processes = {
        TweakedProcess("FattyAcidSynthesis", 0.2),
        TweakedProcess("AminoAcidSynthesis", 0.2),
    };
    nucleusParameters.hexes = {
        Int2(0, 0),
        Int2(1, 0),
        Int2(0, 1),
        Int2(0, -1),
        Int2(1, -1),
        Int2(-1, 1),
        Int2(-1, 0),
        Int2(-1, -1),
        Int2(0, -2),
        Int2(1, -2)
    };

    newWorldTable["nucleus"] = Organelle(nucleusParameters, world);

    // ------------------------------------ //
    // Cytoplasm
    
    // ------------------------------------ //
    // Vacuole
    
    
    _mainOrganelleTable[formatInt(world.GetID())] = newWorldTable;
}



// ["cytoplasm"] = {
//     components = {
//     ["StorageOrganelle"] = {
//     capacity = 10.0
//     }
//     },

//     mass = 0.1,
//     gene = "Y",
//     chanceToCreate = 1,
//     mpCost = 5,
//     mesh = nil, //it's an empty hex
//         hexes = {
//             {["q"]=0,   ["r"]=0}
//         },

//         composition = {
//             aminoacids = 3,
//             glucose = 2,
//             // fattyacids = 0 :/
//         }
//     },

//     ["chloroplast"] = {
//         components = {
//             ["ProcessOrganelle"] = {
//                 colourChangeFactor = 1.0,


//             }
//         },

//         processes = {
//             ["Photosynthesis"] = 0.2
//         },

//         mass = 0.4,
//         gene = "H",
//         chanceToCreate = 2,
//         mpCost = 20,
//         mesh = "chloroplast.mesh",
//         hexes = {
//             {["q"]=0,   ["r"]=0},
//             {["q"]=1,   ["r"]=0},
//             {["q"]=0,   ["r"]=1}
//         },

//         composition = {
//             aminoacids = 4,
//             glucose = 2,
//             // fattyacids = 0 :/
//         }
//     },

//     ["oxytoxy"] = {
//         components = {
//             ["AgentVacuole"] = {
//                 compound = "oxytoxy",
//                 process = "OxyToxySynthesis"
//             }
//         },

//         processes = {
//             ["OxyToxySynthesis"] = 0.05
//         },

//         mass = 0.3,
//         gene = "T",
//         chanceToCreate = 1,
//         mpCost = 40,
//         mesh = "oxytoxy.mesh",
//         hexes = {
//             {["q"]=0,   ["r"]=0}
//         },

//         composition = {
//             aminoacids = 4,
//             glucose = 2,
//             // fattyacids = 0 :/
//         }
//     },

//     ["mitochondrion"] = {
//         components = {
//             ["ProcessOrganelle"] = {
//                 colourChangeFactor = 1.0,
//             }
//         },

//         processes = {
//             ["Respiration"] = 0.07
//         },

//         mass = 0.3,
//         gene = "M",
//         chanceToCreate = 3,
//         mpCost = 20,
//         mesh = "mitochondrion.mesh",
//         hexes = {
//             {["q"]=0, ["r"]=0},
//             {["q"]=0, ["r"]=1}
//         },

//         composition = {
//             aminoacids = 4,
//             glucose = 2,
//             // fattyacids = 0 :/
//         }
//     },

//     ["vacuole"] = {
//         components = {
//             ["StorageOrganelle"] = {
//                 capacity = 100.0
//             }
//         },

//         mass = 0.4,
//         gene = "V",
//         chanceToCreate = 3,
//         mpCost = 15,
//         mesh = "vacuole.mesh",
//         hexes = {
//             {["q"]=0,   ["r"]=0},
//         },

//         composition = {
//             aminoacids = 4,
//             glucose = 2,
//             // fattyacids = 0 :/
//         }
//     },

//     ["flagellum"] = {
//         components = {
//             ["MovementOrganelle"] = {
//                 momentum = 20,
//                 torque = 300
//             }
//         },

//         mass = 0.3,
//         gene = "F",
//         chanceToCreate = 3,
//         mpCost = 25,
//         mesh = "flagellum.mesh",
//         hexes = {
//             {["q"]=0,   ["r"]=0},
//         },

//         composition = {
//             aminoacids = 4,
//             glucose = 2,
//             // fattyacids = 0 :/
//         }
//     }
// }
