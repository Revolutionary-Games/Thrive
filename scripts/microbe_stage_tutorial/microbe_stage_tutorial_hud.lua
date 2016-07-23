
-- Updates the hud with relevant information
class 'MicrobeStageTutorialHudSystem' (System)

function MicrobeStageTutorialHudSystem:__init()
    System.__init(self)
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

function MicrobeStageTutorialHudSystem:activate()
    global_activeMicrobeStageHudSystem = self -- Global reference for event handlers
    self.tutorialStep = 0
end

function MicrobeStageTutorialHudSystem:init(gameState)
    System.init(self, "MicrobeStageTutorialHudSystem", gameState)
    self.rootGUIWindow = gameState:rootGUIWindow()
    self.rootGUIWindow:getChild("MainMenuButton"):registerEventHandler("Clicked", menuMainMenuClicked) -- Defined in microbe_editor_hud.lua
    local quitButton = self.rootGUIWindow:getChild("QuitButton")
    quitButton:registerEventHandler("Clicked", quitButtonClicked)
    self.rootGUIWindow:getChild("HelpPanel"):registerEventHandler("Clicked", function() self.tutorialStep = self.tutorialStep + 1 end)
    self.compoundListBox = self.rootGUIWindow:getChild("CompoundsOpen"):getChild("CompoundsLabel")
    local editorButton = self.rootGUIWindow:getChild("EditorButton")
    editorButton:registerEventHandler("Clicked", function() self:editorButtonClicked() end)
end


function MicrobeStageTutorialHudSystem:update(renderTime)

    local tutorial = self.rootGUIWindow:getChild("HelpPanel")
    
    if Engine.mouse:wasButtonPressed(Mouse.MB_Left) and self.tutorialStep ~= 3 and self.tutorialStep ~= 8 and self.tutorialStep ~= 10 then
        self.tutorialStep = self.tutorialStep + 1
    elseif Engine.keyboard:wasKeyPressed(Keyboard.KC_ESCAPE) and self.tutorialStep <= 2 then
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
        if Entity(PLAYER_NAME):getComponent(MicrobeComponent.TYPE_ID) == nil then
            local microbe = microbeSpawnFunctionGeneric(nil, "Default", false, PLAYER_NAME)
            Engine:playerData():setActiveCreature(microbe.entity.id, GameState.MICROBE_TUTORIAL)
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
        local pos = Entity(PLAYER_NAME):getComponent(OgreSceneNodeComponent.TYPE_ID).transform.position
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
        local compoundID = CompoundRegistry.getCompoundId("atp")
        local compoundsString = string.format("%s - %d", CompoundRegistry.getCompoundDisplayName(compoundID), Microbe(Entity(PLAYER_NAME)):getCompoundAmount(compoundID))
        if self.compoundListItems[compoundID] == nil then
            self.rootGUIWindow:getChild("CompoundsOpen"):show()
            self.compoundListItems[compoundID] = StandardItemWrapper("[colour='FF004400']" .. compoundsString, compoundID)
            self.compoundListBox:listWidgetAddItem(self.compoundListItems[compoundID])
        end
        tutorial:setProperty("{{0.25, 0},{0.05, 0}}", "Position")
        tutorial:setProperty("{{0.5,0},{0.2,0}}", "Size")
        tutorial:setText(
[[You can keep track of your ATP by looking at the
compounds panel shown below.

You currently have only ]] .. math.floor(Microbe(Entity(PLAYER_NAME)):getCompoundAmount(compoundID)) .. [[ ATP. Let's make some more!

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
        local player = Entity(PLAYER_NAME)
        local playerPos = player:getComponent(OgreSceneNodeComponent.TYPE_ID).transform.position
        
        local offset = Entity(CAMERA_NAME):getComponent(OgreCameraComponent.TYPE_ID).properties.offset
        if offset.z < 70 then
            offset.z = offset.z + 1
        end
        
        compoundID = CompoundRegistry.getCompoundId("glucose")
        compoundsString = string.format("%s - %d", CompoundRegistry.getCompoundDisplayName(compoundID), Microbe(player):getCompoundAmount(compoundID))
        if self.compoundListItems[compoundID] == nil then
            self.compoundListItems[compoundID] = StandardItemWrapper("[colour='FF004400']" .. compoundsString, compoundID)
            self.compoundListBox:listWidgetAddItem(self.compoundListItems[compoundID])
        end
        if Microbe(player):getCompoundAmount(CompoundRegistry.getCompoundId("glucose")) < 10 then
            createCompoundCloud("glucose", playerPos.x + 10, playerPos.y, 1000)
        end
        
        tutorial:setProperty("{{0.3, 0},{0.05, 0}}", "Position")
        tutorial:setProperty("{{0.5,0},{0.1,0}}", "Size")
        tutorial:setText(
[[Gather the glucose cloud to continue.]])
        
        Engine:resumeGame()
        
        if Microbe(player):getCompoundAmount(CompoundRegistry.getCompoundId("glucose")) >= 10 then
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
        self.rootGUIWindow:getChild("EditorButton"):show()
        self.rootGUIWindow:getChild("EditorButton"):enable()
        
        tutorial:setProperty("{{0.275, 0},{0.05, 0}}", "Position")
        tutorial:setProperty("{{0.45,0},{0.15,0}}", "Size")
        tutorial:setText(
[[In fact, you are ready to reproduce right now!

Press the green button to the left to enter
the editor.]])
    else 
        Engine:playerData():setActiveCreature(Entity(PLAYER_NAME).id, GameState.MICROBE)
        Engine:setCurrentGameState(GameState.MICROBE)
    end
    
    if self.tutorialStep >= 6 then
        for compoundID in CompoundRegistry.getCompoundList() do
            local compoundsString = string.format("%s - %d", CompoundRegistry.getCompoundDisplayName(compoundID), Microbe(Entity(PLAYER_NAME)):getCompoundAmount(compoundID))
            if self.compoundListItems[compoundID] ~= nil then
               self.compoundListBox:listWidgetUpdateItem(self.compoundListItems[compoundID], "[colour='FF004400']" .. compoundsString)
            end
        end
    end
    
    if keyCombo(kmp.screenshot) then
        Engine:screenShot("screenshot.png")
    end
    
    -- Change zoom.
    local offset = Entity(CAMERA_NAME):getComponent(OgreCameraComponent.TYPE_ID).properties.offset
    
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

function MicrobeStageTutorialHudSystem:openCompoundPanel()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    self.rootGUIWindow:getChild("CompoundsOpen"):show()
    self.rootGUIWindow:getChild("CompoundsClosed"):hide()
end

function MicrobeStageTutorialHudSystem:closeCompoundPanel()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    self.rootGUIWindow:getChild("CompoundsOpen"):hide()
    self.rootGUIWindow:getChild("CompoundsClosed"):show()
end

function MicrobeStageTutorialHudSystem:editorButtonClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    Engine:setCurrentGameState(GameState.MICROBE_EDITOR)
end

function quitButtonClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    Engine:quit()
end
