
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

global_if_already_displayed = false

function HudSystem:activate()
    lockedMap = Engine:playerData():lockedMap()
    if lockedMap ~= nil and not lockedMap:isLocked("Toxin") and not ss and not global_if_already_displayed then
        showMessage("'E' Releases Toxin")
        global_if_already_displayed = true
    end
end

function HudSystem:init(gameState)
    System.init(self, gameState)
    self.rootGuiWindow =  gameState:rootGUIWindow()
    self.compoundListBox = self.rootGuiWindow:getChild("BottomSection"):getChild("CompoundList")
    self.hitpointsBar = self.rootGuiWindow:getChild("BottomSection"):getChild("LifeBar")
    self.hitpointsCountLabel = self.hitpointsBar:getChild("NumberLabel")
    local menuButton = self.rootGuiWindow:getChild("BottomSection"):getChild("MenuButton")
    local helpButton = self.rootGuiWindow:getChild("BottomSection"):getChild("HelpButton")
    local editorButton = self.rootGuiWindow:getChild("MenuPanel"):getChild("EditorButton")
    local returnButton = self.rootGuiWindow:getChild("MenuPanel"):getChild("ReturnButton")
    local returnButton2 = self.rootGuiWindow:getChild("HelpPanel"):getChild("ReturnButton")
    local returnButton3 = self.rootGuiWindow:getChild("MessagePanel"):getChild("ReturnButton")
    local quitButton = self.rootGuiWindow:getChild("MenuPanel"):getChild("QuitButton")
    menuButton:registerEventHandler("Clicked", menuButtonClicked)
    helpButton:registerEventHandler("Clicked", helpButtonClicked)
    editorButton:registerEventHandler("Clicked", editorButtonClicked)
    returnButton:registerEventHandler("Clicked", returnButtonClicked)
    returnButton2:registerEventHandler("Clicked", returnButtonClicked)
    returnButton3:registerEventHandler("Clicked", returnButtonClicked)
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
    elseif  Engine.keyboard:wasKeyPressed(Keyboard.KC_P) then
        playerMicrobe:reproduce()
    end
end

function showMessage(msg)
    local messagePanel = Engine:currentGameState():rootGUIWindow():getChild("MessagePanel")
    messagePanel:getChild("MessageLabel"):setText(msg)
    messagePanel:show()
end

--Event handlers
function menuButtonClicked()
    Engine:currentGameState():rootGUIWindow():getChild("MenuPanel"):show()
        
    if Engine:currentGameState():name() == "microbe" then
        Engine:currentGameState():rootGUIWindow():getChild("HelpPanel"):hide()
    end
end

function helpButtonClicked()
    Engine:currentGameState():rootGUIWindow():getChild("MenuPanel"):hide()
    if Engine:currentGameState():name() == "microbe" then
        Engine:currentGameState():rootGUIWindow():getChild("HelpPanel"):show()
    end
end

function editorButtonClicked()
    Engine:currentGameState():rootGUIWindow():getChild("MenuPanel"):hide()
    Engine:setCurrentGameState(GameState.MICROBE_EDITOR)
end

function returnButtonClicked()
    Engine:currentGameState():rootGUIWindow():getChild("MenuPanel"):hide()
    if Engine:currentGameState():name() == "microbe" then
        Engine:currentGameState():rootGUIWindow():getChild("HelpPanel"):hide()
        Engine:currentGameState():rootGUIWindow():getChild("MessagePanel"):hide()
    elseif Engine:currentGameState():name() == "microbe_editor" then
        Engine:currentGameState():rootGUIWindow():getChild("SaveLoadPanel"):hide()
    end
end

function quitButtonClicked()
    Engine:quit()
end