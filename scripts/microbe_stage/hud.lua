
-- Updates the hud with relevant information
class 'HudSystem' (System)

function HudSystem:__init()
    System.__init(self)
    self.compoundListBox = nil
    self.hitpointsCountLabel = nil
    self.hitpointsBar = nil
    self.compoundListItems = {}
end

function HudSystem:activate()
    -- This needs to be done only once but has to be done after the gamestate has been activated so init is too early.
    if self.compoundListBox == nil then
        local root = CEGUIWindow.getRootWindow():getChild("MicrobeStageRoot")
        self.compoundListBox = root:getChild("BottomSection"):getChild("CompoundList")
        self.hitpointsBar = root:getChild("BottomSection"):getChild("LifeBar")
        self.hitpointsCountLabel = self.hitpointsBar:getChild("NumberLabel")
    end
end

function HudSystem:update(milliseconds)
    local player = Entity("player")
    local playerMicrobe = Microbe(player)

    self.hitpointsBar:progressbarSetProgress(playerMicrobe.microbe.hitpoints/playerMicrobe.microbe.maxHitpoints)
    self.hitpointsCountLabel:setText(""..playerMicrobe.microbe.hitpoints)
  
    for compoundID in CompoundRegistry.getCompoundList() do
        local compoundsString = string.format("%s - %d", CompoundRegistry.getCompoundDisplayName(compoundID), playerMicrobe:getCompoundAmount(compoundID)+0.5)
        if self.compoundListItems[compoundID] == nil then
            self.compoundListItems[compoundID] = ListboxItem(compoundsString)
            self.compoundListItems[compoundID]:setTextColours(0.0, 0.25, 0.0)
            self.compoundListBox:listboxAddItem(self.compoundListItems[compoundID])
        else
            self.compoundListItems[compoundID]:setText(compoundsString)
        end
    end
    self.compoundListBox:listboxHandleUpdatedItemData()
end
