// Thrive JS to C++ wrapper functions and queries
var Thrive = {};

(function(){
    
    Thrive.getVersion = function(onSuccess, onFailure){
        // Get it asynchronously //
        window.cefQuery({request: "thriveVersion", persistent: false, 
                         onSuccess: onSuccess,
                         onFailure: onFailure
                        });
    };
    
    native function startNewGame();
    Thrive.start = startNewGame;
    
    native function editorButtonClicked();
    Thrive.editorButtonClicked = editorButtonClicked;
    
    native function freebuildEditorButtonClicked();
    Thrive.freebuildEditorButtonClicked = freebuildEditorButtonClicked;
    
    native function finishEditingClicked();
    Thrive.finishEditingClicked = finishEditingClicked;
    
    native function killPlayerCellClicked();
    Thrive.killPlayerCellClicked = killPlayerCellClicked;
    
    native function exitToMenuClicked();
    Thrive.exitToMenuClicked = exitToMenuClicked;
    
    native function connectToServer(url);
    Thrive.connectToServer = connectToServer;
    
    native function disconnectFromServer();
    Thrive.disconnectFromServer = disconnectFromServer;
    
    native function enterPlanetEditor();
    Thrive.enterPlanetEditor = enterPlanetEditor;

    native function editPlanet(editType, value);
    Thrive.editPlanet = editPlanet;

    native function pause(value);
    Thrive.pause = pause;

}());
