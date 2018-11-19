// Common helpers for the GUI to work with


//! Returns a value between min and max, range: [min, max]
export function randomBetween(min, max){
    return Math.floor(Math.random() * (max - min + 1) + min);
}

//! Returns true if ran in Thrive (Leviathan is available) false if inside a desktop browser
export function isInEngine(){
    return typeof Leviathan === "object" && Leviathan !== null;
}

//! Shows an alert if isInEngine is false
export function requireEngine(msg){
    if(!isInEngine()){

        alert("This method only works inside Thrive, msg: " + msg);
    }
}

//! Plays the button press sound effect
export function playButtonPressSound(){

    if(isInEngine()){
        Leviathan.Play2DSoundEffect("Data/Sound/soundeffects/gui/button-hover-click.ogg");
    }
}

//! Hides the loading logo
export function hideLoadingLogo(){
    document.getElementById("loadingLogo").style.display = "none";
}

//! Shows the loading logo
export function showLoadingLogo(){
    document.getElementById("loadingLogo").style.display = "flex";
}

//! Helper for filling bar backgrounds
export function barHelper(value, max){
    return (value / max) * 100 + "%";
}

//! Helper for clearing html node children
export function clearChildren(node){

    while(node.hasChildNodes()) {
        node.removeChild(node.lastChild);
    }
}

//! Helper for using native and leviathan js types keys
export function getKeys(obj){

    if(obj.keys){

        return obj.keys();

    } else {

        return Object.keys(obj);
    }
}
