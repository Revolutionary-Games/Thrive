-- Updates the hud with relevant information
class MainMenuHudSystem : ScriptSystem{
        assert(false, "TODO: Main menu hud");
		
		void Init(){
		}
		void activate(){
        //updateLoadButton();
        // if  self.videoPlayer and not self.hasShownIntroVid then
			//   self.videoPlayer:setVideo("intro.wmv")
		//   self.hasShownIntroVid = true
		//    self.videoPlayer:play()
		}
		void updateLoadButton(){
			//if Engine:fileExists("quick.sav") then
			// root:getChild("Background"):getChild("MainMenuInteractive"):getChild("LoadGameButton"):enable();
			//else
			//root:getChild("Background"):getChild("MainMenuInteractive"):getChild("LoadGameButton"):disable();
			}

		void shutdown(){
			// Necessary to avoid failed assert in ogre on exit
			// CEGUIVideoPlayer.destroyVideoPlayer(self.videoPlayer)
			}
			
		void mainMenuLoadButtonClicked(){
		    //getComponent("gui_sounds", g_luaEngine.currentGameState, SoundSourceComponent
			//):playSound("button-hover-click")
    
			//g_luaEngine:setCurrentGameState(GameState.MICROBE)
			//Engine:load("quick.sav")
			// print("Game loaded");
		}
		
		void mainMenuMicrobeStageButtonClicked(){
		   // getComponent("gui_sounds", g_luaEngine.currentGameState, SoundSourceComponent
			//):playSound("button-hover-click")
			// g_luaEngine:setCurrentGameState(GameState.MICROBE_TUTORIAL)
		}
		
	update(){
	
	}

}



//function MainMenuHudSystem:init(gameState)
  // LuaSystem.init(self, "MainMenuHudSystem", gameState)
 //  root = gameState.guiWindow
   
 //  local microbeButton = root:getChild("Background"):
 //     getChild("MainMenuInteractive"):getChild("NewGameButton")
 //  local quitButton = root:getChild("Background"):
 //     getChild("MainMenuInteractive"):getChild("ExitGameButton")
  // local loadButton = root:getChild("Background"):
 //     getChild("MainMenuInteractive"):getChild("LoadGameButton")
   
 //  microbeButton:registerEventHandler("Clicked", mainMenuMicrobeStageButtonClicked)
  // loadButton:registerEventHandler("Clicked", mainMenuLoadButtonClicked)
  // quitButton:registerEventHandler("Clicked", quitButtonClicked)
   
 // updateLoadButton();
   
   //self.videoPlayer = CEGUIVideoPlayer.new("IntroPlayer")
  // root:addChild( self.videoPlayer)
   
  // self.hasShownIntroVid = false
 //  self.vidFadeoutStarted = false
   //self.skippedVideo = false

   -- Set version in GUI
   //local versionLabel = root:getChild("Background"):
    //   getChild("MainMenuInteractive"):getChild("VersionLabel")
   
  // versionLabel:setText("v" .. Engine.thriveVersion)

//end



//function MainMenuHudSystem:update(renderTime, logicTime)
//   if keyCombo(kmp.screenshot) then
       Engine:screenShot("screenshot.png")
//   elseif keyCombo(kmp.skipvideo) then
//      if self.videoPlayer then
//         self.videoPlayer:close()
//         self.videoPlayer:hide()       
//         getComponent("gui_sounds", self.gameState, SoundSourceComponent
//         ):interruptPlaying()
//         getComponent("main_menu_ambience", self.gameState, SoundSourceComponent
//         ).autoLoop = true
//         self.skippedVideo = true
//      end
//   elseif keyCombo(kmp.forward) then 
//   end
//   if self.videoPlayer then
//      self.videoPlayer:update()
//      if self.videoPlayer:getCurrentTime() >= self.videoPlayer:getDuration() - 3.0 then
//         if not self.vidFadeoutStarted then
//            self.videoPlayer:playAnimation("fadeout")
//            self.vidFadeoutStarted = true
//         end
//         if not self.skippedVideo and self.videoPlayer:getCurrentTime() >= self.videoPlayer:getDuration() then
//            self.videoPlayer:hide()
//            getComponent("main_menu_ambience", self.gameState, SoundSourceComponent
//            ).autoLoop = true           
//         end
//      end
//   end
//end

-- quitButtonClicked is already defined in microbe_stage_hud.lua
