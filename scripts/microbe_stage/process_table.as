
//! Represents a process. This loads the data when first setup 
class Process{

    //! This is the only method you should use from outside this file
    void apply() const{
        assert(false, "TODO: Process::apply");
    }

    // ------------------------------------ //
    Process(const string &in name){
        _name = name;

        
    }

    string name {
        get const{
            return _name;
        }
    }
    
    private int input;
    private string _name;
}

//! Versions of processes that are used in OrganelleComponent
class TweakedProcess{

    //! \param processName name of the process as it is in PROCESS_TABLE
    TweakedProcess(const string &in processName, float tweakRate){

        Process@ retrievedProcess;
        if(!PROCESS_TABLE.get(processName, retrievedProcess)){

            assert(false, "Tried to create TweakedProcess with invalid name: " + processName);
            return;
        }

        _process = retrievedProcess;
        this.tweakRate = tweakRate;
    }

    const Process@ process {
        get const{
            return _process;
        }
    }

    private Process@ _process;

    float tweakRate = 1.0;
}

dictionary PROCESS_TABLE;

//! Sets up Processes for use
void setupProcesses(CellStageWorld@ world){

    assert(false, "TODO: read the process registry and create Process objects");

    PROCESS_TABLE[name] = Process(name, data);
}


