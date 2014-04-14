
-- Updates the hud with relevant information
class 'HudSystem' (System)

function HudSystem:__init()
    System.__init(self)
	self.compoundListBox = nil
	self.compoundListItems = {}
end

function HudSystem:activate()
	-- This needs to be done only once but has to be done after the gamestate has been activated so init is too early.
	if self.compoundListBox == nil then
		self.compoundListBox = CEGUIWindow.getRootWindow():getChild("MicrobeStageRoot"):getChild("BottomSection"):getChild("CompoundList")
	end
end

function HudSystem:update(milliseconds)
    local player = Entity("player")
    local playerMicrobe = Microbe(player)
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