
-- Updates the hud with relevant information
class 'HudSystem' (System)

function HudSystem:__init()
    System.__init(self)
	self.compoundListBox = nil
	self.hitpointsCountLabel = nil
	self.hitpointsBar = nil
	self.compoundListItems = {}
    self.rootGuiWindow = nil
end

function HudSystem:init(gameState)
    System.init(self, gameState)
    self.rootGuiWindow =  gameState:rootGUIWindow()
    self.compoundListBox = self.rootGuiWindow:getChild("BottomSection"):getChild("CompoundList")
    self.hitpointsBar = self.rootGuiWindow:getChild("BottomSection"):getChild("LifeBar")
    self.hitpointsCountLabel = self.hitpointsBar:getChild("NumberLabel")
    local menuButton = self.rootGuiWindow:getChild("BottomSection"):getChild("MenuButton")
    local editorButton = self.rootGuiWindow:getChild("MenuPanel"):getChild("EditorButton")
    local returnButton = self.rootGuiWindow:getChild("MenuPanel"):getChild("ReturnButton")
    local quitButton = self.rootGuiWindow:getChild("MenuPanel"):getChild("QuitButton")
    menuButton:registerEventHandler("Clicked", menuButtonClicked)
    editorButton:registerEventHandler("Clicked", editorButtonClicked)
    returnButton:registerEventHandler("Clicked", returnButtonClicked)
    quitButton:registerEventHandler("Clicked", quitButtonClicked)
    self.rootGuiWindow:getChild("MenuPanel"):getChild("MainMenuButton"):registerEventHandler("Clicked", menuMainMenuClicked)
end


function HudSystem:update(milliseconds)
    local player = Entity("player")
    local playerMicrobe = Microbe(player)

    self.hitpointsBar:progressbarSetProgress(playerMicrobe.microbe.hitpoints/playerMicrobe.microbe.maxHitpoints)
    self.hitpointsCountLabel:setText("".. math.floor(playerMicrobe.microbe.hitpoints))
    
    for compoundID in CompoundRegistry.getCompoundList() do
        local compoundsString = string.format("%s - %d", CompoundRegistry.getCompoundDisplayName(compoundID), playerMicrobe:getCompoundAmount(compoundID))
        if self.compoundListItems[compoundID] == nil then
            self.compoundListItems[compoundID] = ListboxItem(compoundsString)
            self.compoundListItems[compoundID]:setTextColours(0.0, 0.25, 0.0)
            self.compoundListBox:listboxAddItem(self.compoundListItems[compoundID])
        else
            self.compoundListItems[compoundID]:setText(compoundsString)
        end
    end
    self.compoundListBox:listboxHandleUpdatedItemData()
    
    if  Engine.keyboard:wasKeyPressed(Keyboard.KC_ESCAPE) then
        menuButtonClicked()
    elseif  Engine.keyboard:wasKeyPressed(Keyboard.KC_F2) then
        editorButtonClicked()
    elseif  Engine.keyboard:wasKeyPressed(Keyboard.KC_E) then
        playerMicrobe:emitAgent(CompoundRegistry.getCompoundId("oxytoxy"), 3)
    end
end

--Event handlers
function menuButtonClicked()
    Engine:currentGameState():rootGUIWindow():getChild("MenuPanel"):show()
end

function editorButtonClicked()
    Engine:currentGameState():rootGUIWindow():getChild("MenuPanel"):hide()
    Engine:setCurrentGameState(GameState.MICROBE_EDITOR)
end

function returnButtonClicked()
    Engine:currentGameState():rootGUIWindow():getChild("MenuPanel"):hide()
end

function quitButtonClicked()
    Engine:quit()
end