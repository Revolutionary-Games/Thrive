// Common helpers for the GUI to work with
"use strict";

//! Returns true if ran in Thrive (Leviathan is available) false if inside a desktop browser
function isInEngine(){
    return typeof Leviathan === 'object' && Leviathan !== null;
}

//! Shows an alert if isInEngine is false
function requireEngine(msg){
    if(!isInEngine()){

        alert("This method only works inside Thrive, msg: " + msg);
    }
}
