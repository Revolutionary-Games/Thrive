// Thrive JS to C++ wrapper functions and queries
var Thrive = {};

(function(){
    
    Thrive.getVersion = function(onSuccess, onFailure){
        // Get it asynchronously //
        window.cefQuery({request: "thriveVersion", persistent: false, 
                         onSuccess: onSuccess,
                         onFailure: onFailure
                        });
    }

    native function startNewGame();
    Thrive.start = startNewGame;

    native function editorButtonClicked();
    Thrive.editorButtonClicked = editorButtonClicked;

    native function finishEditingClicked();
    Thrive.finishEditingClicked = finishEditingClicked;
    
    native function killPlayerCellClicked();
    Thrive.killPlayerCellClicked = killPlayerCellClicked;

    native function exitToMenuClicked();
    Thrive.exitToMenuClicked = exitToMenuClicked;

}());
