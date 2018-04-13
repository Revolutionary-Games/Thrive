// Thrive JS to C++ wrapper functions and queries
var Thrive = {};

Thrive.getVersion = function(onSuccess, onFailure){
    // Get it asynchronously //
    window.cefQuery({request: "thriveVersion", persistent: false, 
        onSuccess: onSuccess,
        onFailure: onFailure
        });
}

Thrive.start = function(){

    native function startNewGame();
    startNewGame();
}
