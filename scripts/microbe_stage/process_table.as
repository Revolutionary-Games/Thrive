
//! Versions of processes that are used in OrganelleComponent
//! \todo It needs to be verified that this actually applies to the processor component
//! of a microbe in process_system.h
class TweakedProcess{

    //! \param processName name of the process as it is in PROCESS_TABLE
    TweakedProcess(const string &in processName, float tweakRate){

        const BioProcess@ process;
        if(!PROCESS_TABLE.get(processName, @process)){

            // This helps debugging
            printProcessTable();
            assert(false, "Tried to create TweakedProcess with invalid name: " + processName);
            return;
        }

        if(process is null){

            assert(false, "TweakedProcess BioProcess with name is null: " + processName);
        }

        _process.store(@process);

        if(this.process is null){

            assert(false, "It's broken");
        }
        
        this.tweakRate = tweakRate;
    }

    const BioProcess@ process {
        get{
            const BioProcess@ obj;
            _process.retrieve(@obj);
            return obj;
        }
    }

    // TODO: is this worse or better performance than calling bioProcessRegistry().getTypeData
    // any time the BioProcess is needed
    private any _process;

    float tweakRate = 1.0;

    // The setup needs the process capacity for some reason
    float capacity = 1.0f;
}

dictionary PROCESS_TABLE;

//! Sets up Processes for use
void setupProcesses(CellStageWorld@ world){

    uint64 count = SimulationParameters::bioProcessRegistry().getSize();
    for(uint64 processId = 0; processId < count; ++processId){

        const auto name = SimulationParameters::bioProcessRegistry().
            getInternalName(processId);

        // The handle needs to be used here to properly give the dictionary the handle value
        @PROCESS_TABLE[name] = SimulationParameters::bioProcessRegistry().
            getTypeData(processId);

        // This is just a sanity check
        // But keep this code for reference
        const BioProcess@ process;
        if(!PROCESS_TABLE.get(name, @process) || process is null){
            assert(false, "Logic for building PROCESS_TABLE broke");
        }
    }

    // Uncomment to print process table for debugging
    // printProcessTable();
}

void printProcessTable(){
    LOG_INFO("Registered processes:");
    auto keys = PROCESS_TABLE.getKeys();
    for(uint i = 0; i < keys.length(); ++i){

        LOG_WRITE(keys[i]);
    }
    LOG_WRITE("");
}


