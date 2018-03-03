
//! Versions of processes that are used in OrganelleComponent
//! \todo It needs to be verified that this actually applies to the processor component
//! of a microbe in process_system.h
class TweakedProcess{

    //! \param processName name of the process as it is in PROCESS_TABLE
    TweakedProcess(const string &in processName, float tweakRate){

        BioProcess@ retrievedProcess;
        if(!PROCESS_TABLE.get(processName, @retrievedProcess)){

            assert(false, "Tried to create TweakedProcess with invalid name: " + processName);
            return;
        }

        @_process = retrievedProcess;
        this.tweakRate = tweakRate;
    }

    const BioProcess@ process {
        get const{
            return _process;
        }
    }

    private BioProcess@ _process;

    float tweakRate = 1.0;

    // The setup needs the process capacity for some reason
    float capacity = 1.0f;
}

dictionary PROCESS_TABLE;

//! Sets up Processes for use
void setupProcesses(CellStageWorld@ world){

    uint64 count = SimulationParameters::bioProcessRegistry().getSize();
    for(uint64 processId = 0; processId < count; ++processId){

        @PROCESS_TABLE[SimulationParameters::bioProcessRegistry().getInternalName(processId)] =
            SimulationParameters::bioProcessRegistry().getTypeData(processId);
    }

    // Uncomment to print process table
    LOG_INFO("Registered processes:");
    auto keys = PROCESS_TABLE.getKeys();
    for(uint i = 0; i < keys.length(); ++i){

        LOG_WRITE(keys[i]);
    }
    LOG_WRITE("");
}


