
-- Updates the hud with relevant information
MicrobeStageTutorialHudSystem = class(
    LuaSystem,
    function(self)

        LuaSystem.create(self)
        self.compoundListBox = nil
        self.hitpointsCountLabel = nil
        self.hitpointsBar = nil
        self.compoundListItems = {}
        self.rootGuiWindow = nil
        self.populationNumberLabel = nil
        self.rootGUIWindow = nil
        self.tutorialStep = 0
        self.scrollChange = 0
    end
)

function MicrobeStageTutorialHudSystem:activate()
    global_activeMicrobeStageHudSystem = self -- Global reference for event handlers
    self.tutorialStep = 0
end

function MicrobeStageTutorialHudSystem:init(gameState)
    LuaSystem.init(self, "MicrobeStageTutorialHudSystem", gameState)
    self.rootGUIWindow = gameState:rootGUIWindow()
    self.rootGUIWindow:getChild("PauseMenu"):getChild("MainMenuButton"):registerEventHandler("Clicked", function() self:menuMainMenuClicked() end)
    local quitButton = self.rootGUIWindow:getChild("PauseMenu"):getChild("QuitButton")
    quitButton:registerEventHandler("Clicked", quitButtonClicked)
    self.rootGUIWindow:getChild("TutorialPanel"):registerEventHandler("Clicked", function() self.tutorialStep = self.tutorialStep + 1 end)
    self.editorButton = self.rootGUIWindow:getChild("EditorButton")
    self.editorButton:registerEventHandler("Clicked", function() self:editorButtonClicked() end)
	
	self.hitpointsBar = self.rootGUIWindow:getChild("HealthPanel"):getChild("LifeBar")
    self.hitpointsCountLabel = self.hitpointsBar:getChild("NumberLabel")
    self.hitpointsMaxLabel = self.rootGUIWindow:getChild("HealthPanel"):getChild("HealthTotal")
    self.hitpointsBar:setProperty("ThriveGeneric/HitpointsBar", "FillImage") 
	
	self.atpBar = self.rootGUIWindow:getChild("CompoundPanel"):getChild("CompoundScroll"):getChild("ATPBar"):getChild("ATPBar")
    self.atpCountLabel = self.atpBar:getChild("NumberLabel")
    self.atpMaxLabel = self.rootGUIWindow:getChild("CompoundPanel"):getChild("CompoundScroll"):getChild("ATPBar"):getChild("ATPTotal")
    self.atpBar:setProperty("ThriveGeneric/ATPBar", "FillImage")
	
	self.atpCountLabel2 = self.rootGUIWindow:getChild("HealthPanel"):getChild("ATPValue")
	
	self.oxygenBar = self.rootGUIWindow:getChild("CompoundPanel"):getChild("CompoundScroll"):getChild("OxygenBar"):getChild("OxygenBar")
    self.oxygenCountLabel = self.oxygenBar:getChild("NumberLabel")
    self.oxygenMaxLabel = self.rootGUIWindow:getChild("CompoundPanel"):getChild("CompoundScroll"):getChild("OxygenBar"):getChild("OxygenTotal")
    self.oxygenBar:setProperty("ThriveGeneric/OxygenBar", "FillImage")
	
	self.aminoacidsBar = self.rootGUIWindow:getChild("CompoundPanel"):getChild("CompoundScroll"):getChild("AminoAcidsBar"):getChild("AminoAcidsBar")
    self.aminoacidsCountLabel = self.aminoacidsBar:getChild("NumberLabel")
    self.aminoacidsMaxLabel = self.rootGUIWindow:getChild("CompoundPanel"):getChild("CompoundScroll"):getChild("AminoAcidsBar"):getChild("AminoAcidsTotal")
    self.aminoacidsBar:setProperty("ThriveGeneric/AminoAcidsBar", "FillImage")
	
	self.ammoniaBar = self.rootGUIWindow:getChild("CompoundPanel"):getChild("CompoundScroll"):getChild("AmmoniaBar"):getChild("AmmoniaBar")
    self.ammoniaCountLabel = self.ammoniaBar:getChild("NumberLabel")
    self.ammoniaMaxLabel = self.rootGUIWindow:getChild("CompoundPanel"):getChild("CompoundScroll"):getChild("AmmoniaBar"):getChild("AmmoniaTotal")
    self.ammoniaBar:setProperty("ThriveGeneric/AmmoniaBar", "FillImage")
	
	self.glucoseBar = self.rootGUIWindow:getChild("CompoundPanel"):getChild("CompoundScroll"):getChild("GlucoseBar"):getChild("GlucoseBar")
    self.glucoseCountLabel = self.glucoseBar:getChild("NumberLabel")
    self.glucoseMaxLabel = self.rootGUIWindow:getChild("CompoundPanel"):getChild("CompoundScroll"):getChild("GlucoseBar"):getChild("GlucoseTotal")
    self.glucoseBar:setProperty("ThriveGeneric/GlucoseBar", "FillImage")
	
	self.co2Bar = self.rootGUIWindow:getChild("CompoundPanel"):getChild("CompoundScroll"):getChild("CO2Bar"):getChild("CO2Bar")
    self.co2CountLabel = self.co2Bar:getChild("NumberLabel")
    self.co2MaxLabel = self.rootGUIWindow:getChild("CompoundPanel"):getChild("CompoundScroll"):getChild("CO2Bar"):getChild("CO2Total")
    self.co2Bar:setProperty("ThriveGeneric/CO2Bar", "FillImage")
	
	self.fattyacidsBar = self.rootGUIWindow:getChild("CompoundPanel"):getChild("CompoundScroll"):getChild("FattyAcidsBar"):getChild("FattyAcidsBar")
	self.fattyacidsCountLabel = self.fattyacidsBar:getChild("NumberLabel")
    self.fattyacidsMaxLabel = self.rootGUIWindow:getChild("CompoundPanel"):getChild("CompoundScroll"):getChild("FattyAcidsBar"):getChild("FattyAcidsTotal")
    self.fattyacidsBar:setProperty("ThriveGeneric/FattyAcidsBar", "FillImage")
	
	self.oxytoxyBar = self.rootGUIWindow:getChild("CompoundPanel"):getChild("CompoundScroll"):getChild("OxyToxyNTBar"):getChild("OxyToxyNTBar")
	self.oxytoxyCountLabel = self.oxytoxyBar:getChild("NumberLabel")
    self.oxytoxyMaxLabel = self.rootGUIWindow:getChild("CompoundPanel"):getChild("CompoundScroll"):getChild("OxyToxyNTBar"):getChild("OxyToxyNTTotal")
    self.oxytoxyBar:setProperty("ThriveGeneric/OxyToxyBar", "FillImage")
end


function MicrobeStageTutorialHudSystem:update(renderTime)

    local tutorial = self.rootGUIWindow:getChild("TutorialPanel")

    -- Updating the GUI:
    if self.tutorialStep > 2 then--where the player microbe is created.
        -- Updating the ATP label.
        local atpID = CompoundRegistry.getCompoundId("atp")
        local atpString = string.format(
            "%d", math.floor(Microbe(
                                 Entity.new(PLAYER_NAME, self.gameState.wrapper)
                                 , nil, self.gameState):getCompoundAmount(atpID)))
        self.atpCountLabel2:setText(atpString)

        -- Updating the compound panel.
        local glucoseID = CompoundRegistry.getCompoundId("glucose")
        local glucoseString = string.format(
            "%d", math.floor(Microbe(
                                 Entity.new(PLAYER_NAME, self.gameState.wrapper)
                                 , nil, self.gameState):getCompoundAmount(glucoseID)))
        self.atpCountLabel:setText(atpString)
        self.glucoseCountLabel:setText(glucoseString)

        -- The default cell has a vacuole, which means it has 100 storage points.
        -- I'm too lazy to check for the microbe's storage space. :/
        self.atpMaxLabel:setText("/ 100")
        self.glucoseMaxLabel:setText("/ 100")

        self.atpBar:progressbarSetProgress(atpString / 100)
        self.glucoseBar:progressbarSetProgress(glucoseString / 100)
    else
        self.atpCountLabel2:hide()
        self.rootGUIWindow:getChild("HealthPanel"):getChild("ATPIcon"):hide()
	self.rootGUIWindow:getChild("HealthPanel"):getChild("ATPLabel"):hide()
        self.rootGUIWindow:getChild("EditorButton"):hide()
        self.rootGUIWindow:getChild("CompoundPanel"):hide()
    end

    if Engine.mouse:wasButtonPressed(Mouse.MB_Left) and self.tutorialStep ~= 3 and self.tutorialStep ~= 8 and self.tutorialStep ~= 11 then
        self.tutorialStep = self.tutorialStep + 1
    elseif Engine.keyboard:wasKeyPressed(KEYCODE.KC_ESCAPE) and self.tutorialStep <= 2 then
        self.tutorialStep = -1
    end
    
    if self.tutorialStep == 0 then
        Engine:pauseGame()
        tutorial:setProperty("{{0.25, 0},{0.3, 0}}", "Position")
        tutorial:setProperty("{{0.5,0},{0.4,0}}", "Size")
        tutorial:setText(
[[On a distant alien planet, eons of volcanic
activity and meteor impacts have led to the
development of a new phenomenon in the universe.

Life.

Simple microbes reside in the deep regions of the
ocean, and have diversified into many species.
You are only one of the many that have evolved.


Click to continue or press escape to skip the tutorial.]])

    elseif self.tutorialStep == 1 then
        tutorial:setProperty("{{0.25, 0},{0.35, 0}}", "Position")
        tutorial:setProperty("{{0.6,0},{0.3,0}}", "Size")
        tutorial:setText(
[[To survive in this hostile world, you will need to
collect any compounds that you can find and
evolve each generation to compete against
the other species of cells.

Remember, that they will be adapting to the environment
just like you. (Probably not in this release though.)


Click to continue or press escape to skip the tutorial.]])

    elseif self.tutorialStep == 2 then
        tutorial:setProperty("{{0.31, 0},{0.05, 0}}", "Position")
        tutorial:setProperty("{{0.38,0},{0.1,0}}", "Size")
        tutorial:setText(
[[Your cell is shown below.

Click anywhere to continue...]])
        if getComponent(PLAYER_NAME, self.gameState, MicrobeComponent) == nil then
            print("trying to spawn player")
            local microbe = microbeSpawnFunctionGeneric(nil, "Default", false,
                                                        PLAYER_NAME, self.gameState)
            Engine:playerData():setActiveCreature(microbe.entity.id,
                                                  GameState.MICROBE_TUTORIAL.wrapper)

            -- Make sure player doesn't run out of ATP immediately
            microbe:storeCompound(CompoundRegistry.getCompoundId("atp"), 50, false)
        end
		
		
    elseif self.tutorialStep == 3 then
        Engine:resumeGame()
        tutorial:setProperty("{{0.3, 0},{0.05, 0}}", "Position")
        tutorial:setProperty("{{0.5,0},{0.2,0}}", "Size")
        tutorial:setText(
[[You can change its orientation with your
mouse, and use WASD to move around.

Give it a try!

Swim for a while in any direction to continue...]])
        local pos = getComponent(PLAYER_NAME, self.gameState, OgreSceneNodeComponent).transform.position
        if math.sqrt(pos.x*pos.x + pos.y*pos.y) > 30 then
            self.tutorialStep = self.tutorialStep + 1;
        end
    elseif self.tutorialStep == 4 then
        Engine:pauseGame()
        tutorial:setProperty("{{0.3, 0},{0.05, 0}}", "Position")
        tutorial:setProperty("{{0.4,0},{0.15,0}}", "Size")
        tutorial:setText(
[[Congratulation! You can swim! To do this,
your cell uses chemical energy.

Click anywhere to continue...]])
        
    elseif self.tutorialStep == 5 then
        tutorial:setProperty("{{0.25, 0},{0.05, 0}}", "Position")
        tutorial:setProperty("{{0.5,0},{0.2,0}}", "Size")
        tutorial:setText(
[[This energy comes in the form of ATP. ATP is used by
your cell to move and to stay alive. However, you 
cannot find ATP in the environment. ATP must be 
harvested from the compounds you find.

Click anywhere to continue...]])
        
    elseif self.tutorialStep == 6 then
        self.rootGUIWindow:getChild("HealthPanel"):show()
        self.atpCountLabel2:show()
        self.rootGUIWindow:getChild("HealthPanel"):getChild("ATPIcon"):show()
	self.rootGUIWindow:getChild("HealthPanel"):getChild("ATPLabel"):show()
        local atpID = CompoundRegistry.getCompoundId("atp")

        tutorial:setProperty("{{0.25, 0},{0.05, 0}}", "Position")
        tutorial:setProperty("{{0.5,0},{0.2,0}}", "Size")
        tutorial:setText(
[[You can keep track of your ATP by looking at the
compounds panel shown below.

You currently have only ]] .. math.floor(Microbe(
                                             Entity.new(PLAYER_NAME,
                                             self.gameState.wrapper) ,
                                             nil,
                                             self.gameState):getCompoundAmount(atpID))
                                             .. [[ ATP. Let's make
                                             some more!

Click anywhere to continue...]])
           
    elseif self.tutorialStep == 7 then
        tutorial:setProperty("{{0.25, 0},{0.05, 0}}", "Position")
        tutorial:setProperty("{{0.5,0},{0.25,0}}", "Size")
        tutorial:setText(
[[ATP is automatically made in your mitochondria 
(purple organelle) out of oxygen and glucose.

Glucose is spawned in the environment in the form
of white clouds, while oxygen is cyan. 

Click anywhere to continue...]])
        
        
    elseif self.tutorialStep == 8 then
        local player = Entity.new(PLAYER_NAME, self.gameState.wrapper)
        local playerPos = getComponent(player, OgreSceneNodeComponent).transform.position
        
        local offset = getComponent(CAMERA_NAME, self.gameState, OgreCameraComponent).properties.offset
        if offset.z < 70 then
            offset.z = offset.z + 1
        end

        self.rootGUIWindow:getChild("CompoundPanel"):show()

        if Microbe(player, nil, self.gameState
                  ):getCompoundAmount(CompoundRegistry.getCompoundId("glucose")) < 10 then
            createCompoundCloud("glucose", playerPos.x + 10, playerPos.y, 1000)
        end
        
        tutorial:setProperty("{{0.3, 0},{0.05, 0}}", "Position")
        tutorial:setProperty("{{0.5,0},{0.1,0}}", "Size")
        tutorial:setText(
[[Gather the glucose cloud to continue.]])
        
        Engine:resumeGame()
        
        if Microbe(player, nil, self.gameState
                  ):getCompoundAmount(CompoundRegistry.getCompoundId("glucose")) >= 10 then
            self.tutorialStep = self.tutorialStep + 1
        end
        
    elseif self.tutorialStep == 9 then
        tutorial:setProperty("{{0.25, 0},{0.05, 0}}", "Position")
        tutorial:setProperty("{{0.5,0},{0.35,0}}", "Size")
        tutorial:setText(
[[Great job! The compounds you collected are stored
in your only vacuole (light green organelle) and
are being processed into ATP by your mitochondrion.

Your cell is actually capable of carrying out a large
variety of processes, such as shooting toxins to hurt
other cells, engulfing cells and bacteria smaller than
you, and, most importantly, reproducing.

Click anywhere to continue...]])

    elseif self.tutorialStep == 10 then
        tutorial:setProperty("{{0.25, 0},{0.05, 0}}", "Position")
        tutorial:setProperty("{{0.5,0},{0.4,0}}", "Size")
        tutorial:setText(
[[To reproduce you need to divide each of your 
organelles into two and then duplicate the DNA
in your nucleus. Each organelle needs 2 glucose
and 1 amino acids (made from 1 glucose and 1 ammonia)
to split in half. 

Make sure your glucose (white clouds) store is always
above 16 and your ammonia (yellow clouds) store is 
above 12 and you'll be fine.

Click anywhere to continue...]])
        
    elseif self.tutorialStep == 11 then
        self.rootGUIWindow:getChild("EditorButton"):show()
        self.rootGUIWindow:getChild("EditorButton"):enable()
        
        tutorial:setProperty("{{0.275, 0},{0.05, 0}}", "Position")
        tutorial:setProperty("{{0.45,0},{0.15,0}}", "Size")
        tutorial:setText(
[[In fact, you are ready to reproduce right now!

Press the button on the top right corner to enter
the editor.]])
    else 
        Engine:playerData():setActiveCreature(Entity.new(PLAYER_NAME, self.gameState.wrapper).
                                                  id, GameState.MICROBE.wrapper)
        g_luaEngine:setCurrentGameState(GameState.MICROBE)
    end
    
    if self.tutorialStep >= 6 then
        for _, compoundID in pairs(CompoundRegistry.getCompoundList()) do
            local compoundsString = string.format(
                "%s - %d",
                CompoundRegistry.getCompoundDisplayName(compoundID),
                Microbe.new(Entity.new(PLAYER_NAME, g_luaEngine.currentGameState.wrapper),
                            nil, g_luaEngine.currentGameState
                ):getCompoundAmount(compoundID))
            if self.compoundListItems[compoundID] ~= nil then
               self.compoundListBox:listWidgetUpdateItem(self.compoundListItems[compoundID], "[colour='FF004400']" .. compoundsString)
            end
        end
    end
    
    if keyCombo(kmp.screenshot) then
        Engine:screenShot("screenshot.png")
    end
    
    -- Change zoom.
    local offset = getComponent(CAMERA_NAME, self.gameState, OgreCameraComponent
    ).properties.offset
    
    if Engine.mouse:scrollChange()/10 ~= 0 then
        self.scrollChange = self.scrollChange + Engine.mouse:scrollChange()/10
    end
    
    local newZVal = offset.z
    if self.scrollChange >= 1 then
        newZVal = newZVal + 2.5
        self.scrollChange = self.scrollChange - 1
    elseif self.scrollChange <= -1 then
        newZVal = newZVal - 2.5
        self.scrollChange = self.scrollChange + 1
    end
    
    if newZVal < 10 then
        newZVal = 10
        self.scrollChange = 0
    elseif newZVal > 60 then
        newZVal = 60
        self.scrollChange = 0
    end
    
    offset.z = newZVal
end

function MicrobeStageTutorialHudSystem:toggleCompoundPanel()
    getComponent("gui_sounds", g_luaEngine.currentGameState, SoundSourceComponent
    ):playSound("button-hover-click")
    if self.compoundsOpen then
    self.rootGUIWindow:getChild("CompoundPanel"):hide()
    self.rootGUIWindow:getChild("CompoundExpandButton"):getChild("CompoundExpandIcon"):hide()
    self.rootGUIWindow:getChild("CompoundExpandButton"):getChild("CompoundContractIcon"):show()
    self.compoundsOpen = false
    else
    self.rootGUIWindow:getChild("CompoundPanel"):show()
    self.rootGUIWindow:getChild("CompoundExpandButton"):getChild("CompoundExpandIcon"):show()
    self.rootGUIWindow:getChild("CompoundExpandButton"):getChild("CompoundContractIcon"):hide()
    self.compoundsOpen = true
    end
end

function MicrobeStageTutorialHudSystem:showReproductionDialog()
    -- print("Reproduction Dialog called but currently disabled. Is it needed? Note that the editor button has been enabled")
    --global_activeMicrobeStageHudSystem.rootGUIWindow:getChild("ReproductionPanel"):show()
    self.editorButton:enable()
end

function MicrobeStageTutorialHudSystem:closeCompoundPanel()
    getComponent("gui_sounds", g_luaEngine.currentGameState, SoundSourceComponent
    ):playSound("button-hover-click")
    self.rootGUIWindow:getChild("CompoundsOpen"):hide()
    self.rootGUIWindow:getChild("CompoundsClosed"):show()
end

function MicrobeStageTutorialHudSystem:editorButtonClicked()
    getComponent("gui_sounds", g_luaEngine.currentGameState, SoundSourceComponent
    ):playSound("button-hover-click")
    g_luaEngine:setCurrentGameState(GameState.MICROBE_EDITOR)
end

function quitButtonClicked()

    getComponent("gui_sounds", g_luaEngine.currentGameState, SoundSourceComponent
    ):playSound("button-hover-click")
    
    Engine:quit()
end
