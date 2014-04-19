-- Needs to be the first system
class 'MicrobeReplacementSystem' (System)

function MicrobeReplacementSystem:__init()
    System.__init(self)
    self.createNewPlayer = nil
end


function MicrobeReplacementSystem:update(milliseconds)
      if self.createNewPlayer ~= nil then -- This triggers as the first system update after a player removal
        player = self.createNewPlayer:recreateMicrobe(PLAYER_NAME)
        self.createNewPlayer = nil
    end
    if newPlayerAvaliable ~= nil then
       Entity(PLAYER_NAME):destroy() -- The removal is processed after all systems are done updating
       self.createNewPlayer = newPlayerAvaliable
       newPlayerAvaliable = nil
    end
  
end


