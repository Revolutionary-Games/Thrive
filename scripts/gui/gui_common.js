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

//! Plays the button press sound effect
function playButtonPressSound(){

    if(isInEngine()){
        Leviathan.Play2DSoundEffect("Data/Sound/soundeffects/gui/button-hover-click.ogg");
    }
}

//! Hides the loading logo
function hideLoadingLogo(){
    document.getElementById("loadingLogo").style.display = "none";
}

//! Shows the loading logo
function showLoadingLogo(){
    document.getElementById("loadingLogo").style.display = "flex";
}


//! Plays a video with the video player
function playVideo(file, ondone){

    document.getElementById("videoPlayer").style.display = "flex";
    let videoElement = document.getElementById("videoPlayersVideo");

    // Start playing as autoplay is on
    videoElement.src = file;

    // TODO: volume control
    videoElement.volume = 1.0;

    // Set end event
    $(videoElement).one("ended", function(event){
        event.stopPropagation();
        
        // TODO: cool animation
        document.getElementById("videoPlayer").style.display = "none";
        
        ondone();
    });
}

//! Stops a video (and triggers the end event)
function stopVideo(){

    let videoElement = document.getElementById("videoPlayersVideo");
    
    let event = new Event("ended");
    videoElement.dispatchEvent(event);
    videoElement.src = "";
}
