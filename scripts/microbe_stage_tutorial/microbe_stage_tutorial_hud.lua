
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
    elseif Engine.keyboard:wasKeyPressed(Keyboard.KC_ESCAPE) then
        self.tutorialStep = -1
    end
    
    if self.tutorialStep == 0 then
        Engine:pauseGame()
        tutorial:setProperty("{{0, 200},{0, 200}}", "Position")
        tutorial:setText(
[[On a distant alien planet, eons of volcanic
activity and meteor impacts have led to the
development of a new phenomenon in the universe.

Life.

Simple microbes reside in the deep regions of the
ocean, and have diversified into many species.
You are only one of the many that have evolved.


Click to continue or press escape to skip the tutorial.]])
        tutorial:setProperty("{{0,600},{0,260}}", "Size")
    elseif self.tutorialStep == 1 then
        tutorial:setProperty("{{0, 200},{0, 200}}", "Position")
        tutorial:setText(
[[To survive in this hostile world, you will need to
collect any compounds that you can find and
evolve each generation to compete against
the other species of cells.

Remember, that they will be adapting to the environment
just like you. (Probably not in this release though.)


Click to continue or press escape to skip the tutorial.]])
        tutorial:setProperty("{{0,600},{0,220}}", "Size")
    elseif self.tutorialStep == 2 then
        tutorial:setProperty("{{0, 300},{0, 50}}", "Position")
        tutorial:setText(
[[Your cell is shown below.

Click anywhere to continue...]])
        tutorial:setProperty("{{0,400},{0,80}}", "Size")
        if Entity(PLAYER_NAME):getComponent(MicrobeComponent.TYPE_ID) == nil then
            local microbe = microbeSpawnFunctionGeneric(nil, "Default", false, PLAYER_NAME)
            Engine:playerData():setActiveCreature(microbe.entity.id, GameState.MICROBE_TUTORIAL)
        end
    elseif self.tutorialStep == 3 then
        Engine:resumeGame()
        tutorial:setProperty("{{0, 250},{0, 50}}", "Position")
        tutorial:setText(
[[You can change its orientation with your
mouse, and use WASD to move around.

Give it a try!

Leave the green ring to continue...]])
        tutorial:setProperty("{{0,500},{0,140}}", "Size")
        
        local pos = Entity(PLAYER_NAME):getComponent(OgreSceneNodeComponent.TYPE_ID).transform.position
        if math.sqrt(pos.x*pos.x + pos.y*pos.y) > 30 then
            self.tutorialStep = self.tutorialStep + 1;
        end
    elseif self.tutorialStep == 4 then
        Engine:pauseGame()
        tutorial:setProperty("{{0, 250},{0, 50}}", "Position")
        tutorial:setText(
[[Congratulation! You can swim! To do this,
your cell uses chemical energy.

Click anywhere to continue...]])
        tutorial:setProperty("{{0,500},{0,100}}", "Size")
    elseif self.tutorialStep == 5 then
        tutorial:setProperty("{{0, 200},{0, 50}}", "Position")
        tutorial:setText(
[[This energy comes in the form of ATP. ATP is used by
your cell to move and to stay alive. However, you 
cannot find ATP in the environment. ATP must be 
harvested from the compounds you find.

Click anywhere to continue...]])
        tutorial:setProperty("{{0,600},{0,140}}", "Size")
    elseif self.tutorialStep == 6 then
        local compoundID = CompoundRegistry.getCompoundId("atp")
        local compoundsString = string.format("%s - %d", CompoundRegistry.getCompoundDisplayName(compoundID), Microbe(Entity(PLAYER_NAME)):getCompoundAmount(compoundID))
        if self.compoundListItems[compoundID] == nil then
            self.rootGUIWindow:getChild("CompoundsOpen"):show()
            self.compoundListItems[compoundID] = StandardItemWrapper("[colour='FF004400']" .. compoundsString, compoundID)
            self.compoundListBox:listWidgetAddItem(self.compoundListItems[compoundID])
        end
        
        tutorial:setProperty("{{0, 200},{0, 50}}", "Position")
        tutorial:setText(
[[You can keep track of your ATP by looking at the
compounds panel shown below.

You currently have only ]] .. math.floor(Microbe(Entity(PLAYER_NAME)):getCompoundAmount(compoundID)) .. [[ ATP. Let's make some more!

Click anywhere to continue...]])
        tutorial:setProperty("{{0,600},{0,140}}", "Size")
           
    elseif self.tutorialStep == 7 then
tutorial:setProperty("{{0, 200},{0, 50}}", "Position")
tutorial:setText(
[[ATP is automatically made in your mitochondria 
(purple organelle) out of oxygen and glucose.

Glucose is spawned in the environment in the form
of white clouds, while oxygen is cyan. 

Click anywhere to continue...]])
        tutorial:setProperty("{{0,600},{0,160}}", "Size")
        
    elseif self.tutorialStep == 8 then
        local player = Entity(PLAYER_NAME)
        local playerPos = player:getComponent(OgreSceneNodeComponent.TYPE_ID).transform.position
        
        local offset = Entity(CAMERA_NAME):getComponent(OgreCameraComponent.TYPE_ID).properties.offset
        if offset.z < 70 then
            offset.z = offset.z + 1
        end
        
        local compoundID = CompoundRegistry.getCompoundId("oxygen")
        local compoundsString = string.format("%s - %d", CompoundRegistry.getCompoundDisplayName(compoundID), Microbe(player):getCompoundAmount(compoundID))
        if self.compoundListItems[compoundID] == nil then
            self.compoundListItems[compoundID] = StandardItemWrapper("[colour='FF004400']" .. compoundsString, compoundID)
            self.compoundListBox:listWidgetAddItem(self.compoundListItems[compoundID])
        end    
        if Microbe(player):getCompoundAmount(CompoundRegistry.getCompoundId("oxygen")) < 10 then
            createCompoundCloud("oxygen", playerPos.x - 10, playerPos.y, 1000)
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
        
        tutorial:setProperty("{{0, 200},{0, 50}}", "Position")
        tutorial:setText(
[[Gather the clouds to continue.]])
        tutorial:setProperty("{{0,600},{0,80}}", "Size")
        
        Engine:resumeGame()
        
        if Microbe(player):getCompoundAmount(CompoundRegistry.getCompoundId("oxygen")) >= 10 and Microbe(player):getCompoundAmount(CompoundRegistry.getCompoundId("glucose")) >= 10 then
            self.tutorialStep = self.tutorialStep + 1
        end
        
    elseif self.tutorialStep == 9 then
        tutorial:setProperty("{{0, 200},{0, 50}}", "Position")
        tutorial:setText(
[[Great job! The compounds you collected are stored
in your only vacuole (light green organelle) and
are being processed into ATP by your mitochondrion.

Your cell is actually capable of carrying out a large
variety of processes, such as shooting toxins to hurt
other cells, engulfing cells and bacteria smaller than
you, and, most importantly, reproducing.

Click anywhere to continue...]])
        tutorial:setProperty("{{0,600},{0,220}}", "Size")
        
    elseif self.tutorialStep == 10 then
        self.rootGUIWindow:getChild("EditorButton"):show()
        self.rootGUIWindow:getChild("EditorButton"):enable()
        
        tutorial:setProperty("{{0, 200},{0, 50}}", "Position")
        tutorial:setText(
[[In fact, you are ready to reproduce right now!

Press the green button to the left to enter
the editor.]])
        tutorial:setProperty("{{0,600},{0,100}}", "Size")
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
