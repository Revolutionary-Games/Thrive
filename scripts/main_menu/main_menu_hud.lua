-- Updates the hud with relevant information
class 'MainMenuHudSystem' (System)

function MainMenuHudSystem:__init()
    System.__init(self)
end

function MainMenuHudSystem:init(gameState)
    System.init(self, "MainMenuHudSystem", gameState)
    root = gameState:rootGUIWindow()
    local microbeButton = root:getChild("Background"):getChild("MainMenuInteractive"):getChild("NewGameButton")
    local microbeEditorButton = root:getChild("Background"):getChild("MainMenuInteractive"):getChild("EditorMenuButton")
    local quitButton = root:getChild("Background"):getChild("MainMenuInteractive"):getChild("ExitGameButton")
    local loadButton = root:getChild("Background"):getChild("MainMenuInteractive"):getChild("LoadGameButton")   
    microbeButton:registerEventHandler("Clicked", mainMenuMicrobeStageButtonClicked)
    microbeEditorButton:registerEventHandler("Clicked", mainMenuMicrobeEditorButtonClicked)
    loadButton:registerEventHandler("Clicked", mainMenuLoadButtonClicked)
    quitButton:registerEventHandler("Clicked", quitButtonClicked)
	updateLoadButton();
    self.videoPlayer = CEGUIVideoPlayer("IntroPlayer")
    root:addChild( self.videoPlayer)
    self.hasShownIntroVid = false
    self.vidFadeoutStarted = false
    self.skippedVideo = false
    
end

__soundTimer = 0
function MainMenuHudSystem:update(renderTime, logicTime)
    if keyCombo(kmp.screenshot) then
        Engine:screenShot("screenshot.png")
    elseif keyCombo(kmp.skipvideo) then
        if self.videoPlayer then
            self.videoPlayer:pause()
            self.videoPlayer:hide()
            Entity("gui_sounds"):getComponent(SoundSourceComponent.TYPE_ID):interruptPlaying()
            Entity("main_menu_ambience"):getComponent(SoundSourceComponent.TYPE_ID).autoLoop = true
            self.skippedVideo = true
        end
    elseif keyCombo(kmp.forward) then
    
    end
    if self.videoPlayer then
        self.videoPlayer:update()
        if self.videoPlayer:getCurrentTime() >= self.videoPlayer:getDuration() - 3.0 then
            if not self.vidFadeoutStarted then
                self.videoPlayer:playAnimation("fadeout")
                self.vidFadeoutStarted = true
            end
            if not self.skippedVideo and self.videoPlayer:getCurrentTime() >= self.videoPlayer:getDuration() then
                self.videoPlayer:hide()
                Entity("main_menu_ambience"):getComponent(SoundSourceComponent.TYPE_ID).autoLoop = true
            end
        else
            __soundTimer = __soundTimer + 1
            if __soundTimer == 2 then 
                -- cAudio gives an error if we play this first frame or in active or init
                -- The manual playing of sound here is a temporary fix until we get the video player to play audio
                Entity("gui_sounds"):getComponent(SoundSourceComponent.TYPE_ID):playSound("intro")
            end
        end
    end
end

function MainMenuHudSystem:shutdown()
    -- Necessary to avoid failed assert in ogre on exit
    CEGUIVideoPlayer.destroyVideoPlayer(self.videoPlayer)
end

function MainMenuHudSystem:activate()
    updateLoadButton();
    if  self.videoPlayer and not self.hasShownIntroVid then
        self.videoPlayer:setVideo("intro.wmv")
        self.hasShownIntroVid = true
        self.videoPlayer:play()
    end
end
function updateLoadButton()
    if Engine:fileExists("quick.sav") then
        root:getChild("Background"):getChild("MainMenuInteractive"):getChild("LoadGameButton"):enable();
    else
        root:getChild("Background"):getChild("MainMenuInteractive"):getChild("LoadGameButton"):disable();
    end
end


function mainMenuLoadButtonClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    Engine:setCurrentGameState(GameState.MICROBE)
    Engine:load("quick.sav")
    print("Game loaded");
end

function mainMenuMicrobeStageButtonClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    Engine:setCurrentGameState(GameState.MICROBE_TUTORIAL)
end

function mainMenuMicrobeEditorButtonClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    Engine:setCurrentGameState(GameState.MICROBE_EDITOR)
end

-- quitButtonClicked is already defined in microbe_stage_hud.lua
