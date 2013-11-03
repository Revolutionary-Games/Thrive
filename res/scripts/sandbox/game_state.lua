class 'TestSystem' (System)

function TestSystem:__init(string)
    System.__init(self)
    self.string = string
end

function TestSystem:update(milliseconds)
    print(self.string .. tostring(milliseconds))
end


Engine:addGameState(
    "test",
    {
        TestSystem("A: "),
        TestSystem("B: "),
        TestSystem("C: ")
    }
)

Engine:setCurrentGameState("test")
