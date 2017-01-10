-- Updates the hud with relevant information
class 'MainMenuHudSystem' (System)

function MainMenuHudSystem:__init()
    System.__init(self)
end

function MainMenuHudSystem:init(gameState)
    System.init(self, "MainMenuHudSystem", gameState)
    root = gameState:rootGUIWindow()
    local microbeButton = root:getChild("Background"):getChild("MainMenuInteractive"):getChild("NewGameButton")
    local quitButton = root:getChild("Background"):getChild("MainMenuInteractive"):getChild("ExitGameButton")
    local loadButton = root:getChild("Background"):getChild("MainMenuInteractive"):getChild("LoadGameButton")   
    microbeButton:registerEventHandler("Clicked", mainMenuMicrobeStageButtonClicked)
    loadButton:registerEventHandler("Clicked", mainMenuLoadButtonClicked)
    quitButton:registerEventHandler("Clicked", quitButtonClicked)
	updateLoadButton();
    self.videoPlayer = CEGUIVideoPlayer("IntroPlayer")
    root:addChild( self.videoPlayer)
    self.hasShownIntroVid = false
    self.vidFadeoutStarted = false
    self.skippedVideo = false
    
end

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
    resetAutoEvo()
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    Engine:setCurrentGameState(GameState.MICROBE)
    Engine:load("quick.sav")
    print("Game loaded");
end

function mainMenuMicrobeStageButtonClicked()
    local guiSoundEntity = Entity("gui_sounds")
    resetAutoEvo()
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    Engine:setCurrentGameState(GameState.MICROBE_TUTORIAL)
end

-- quitButtonClicked is already defined in microbe_stage_hud.lua
